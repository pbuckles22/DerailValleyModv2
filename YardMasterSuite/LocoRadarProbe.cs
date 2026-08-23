using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Event-gated other-loco cache for AR radar (6.16). FoT only on city enter,
    /// leave-loco, world enter (including a second save), or dead cache — never a timer.
    /// </summary>
    internal static class LocoRadarProbe
    {
        private const int CandidateCap = 64;

        private static readonly TrainCar?[] RankedCars = new TrainCar?[LocoRadarSelection.DefaultMaxResults];
        private static readonly string?[] TypeIds = new string?[LocoRadarSelection.DefaultMaxResults];
        private static readonly string?[] Captions = new string?[LocoRadarSelection.DefaultMaxResults];
        private static readonly int[] CaptionMeterKeys = new int[LocoRadarSelection.DefaultMaxResults];
        private static readonly LocoRadarCandidate[] Candidates = new LocoRadarCandidate[CandidateCap];
        private static readonly TrainCar?[] CandidateCars = new TrainCar?[CandidateCap];
        private static readonly int[] RankedIds = new int[LocoRadarSelection.DefaultMaxResults];
        private static readonly HashSet<int> Exclude = new HashSet<int>();

        private static int _lastLicenseKey;
        private static int _count;
        private static string? _scannedCityId;
        private static int? _occupiedLocoId;
        private static bool _forceScan = true;
        private static bool _unknownRetryUsed;
        private static bool _pendingUnknownRetry;
        private static float _unknownRetryAt = -1f;
        private static bool _playerLocoKnownScanUsed;

        internal static int Count => _count;

        internal static void Clear()
        {
            _count = 0;
            _scannedCityId = null;
            _occupiedLocoId = null;
            _forceScan = true;
            _lastLicenseKey = 0;
            ResetUnknownRetry();
            ClearSlots();
        }

        private static void ClearSlots()
        {
            for (var i = 0; i < RankedCars.Length; i++)
            {
                RankedCars[i] = null;
                TypeIds[i] = null;
                Captions[i] = null;
                CaptionMeterKeys[i] = -1;
            }
        }

        internal static void MarkWorldEnter()
        {
            _forceScan = true;
            ResetUnknownRetry();
        }

        internal static void Ensure(System.Action<string>? log)
        {
            if (LocoRadarScanPolicy.ShouldForceScanWhenCacheDead(_count, CountLive()))
            {
                _forceScan = true;
            }

            var licenseKey = LocoRadarLicenseGate.FilterEnabled ? CurrentLicenseKey() : 0;
            if (LocoRadarLicenseGate.FilterEnabled
                && LocoRadarScanPolicy.ShouldForceScanOnLicenseChange(_lastLicenseKey, licenseKey))
            {
                _forceScan = true;
                ResetUnknownRetry();
            }

            if (_pendingUnknownRetry
                && LocoRadarScanPolicy.ShouldForceScanOnLicenseUnknownRetry(
                    hadUnknown: true,
                    alreadyRetried: _unknownRetryUsed)
                && Time.unscaledTime >= _unknownRetryAt)
            {
                _forceScan = true;
                _unknownRetryUsed = true;
                _pendingUnknownRetry = false;
            }

            if (LocoRadarScanPolicy.ShouldForceScanOnPlayerLocoKnown(
                    _count,
                    PlayerHasKnownLoco(),
                    _playerLocoKnownScanUsed))
            {
                _forceScan = true;
                _playerLocoKnownScanUsed = true;
            }

            _lastLicenseKey = licenseKey;

            var occupied = TryGetOccupiedLocoInstanceId();
            string? city = null;
            if (StationOfficeAnchor.TryGet(out _, out _, out _, out var yardId)
                && !string.IsNullOrWhiteSpace(yardId))
            {
                city = yardId;
            }

            var reason = LocoRadarScanPolicy.Decide(
                featureEnabled: true,
                forceScan: _forceScan,
                lastScannedCityId: _scannedCityId,
                currentCityId: city,
                lastOccupiedLocoId: _occupiedLocoId,
                currentOccupiedLocoId: occupied,
                out var leftLocoId);

            _occupiedLocoId = occupied;
            if (reason == LocoRadarScanReason.None)
            {
                return;
            }

            _forceScan = false;
            _scannedCityId = city;
            RunScan(reason, city, leftLocoId, log);
        }

        internal static bool TryGet(int index, out Vector3 world, out string caption)
        {
            world = default;
            caption = "";
            if (index < 0 || index >= _count)
            {
                return false;
            }

            var car = RankedCars[index];
            if (car == null)
            {
                return false;
            }

            try
            {
                world = car.transform.position;
            }
            catch
            {
                return false;
            }

            var dist = 0f;
            var player = PlayerManager.PlayerTransform;
            if (player != null)
            {
                var pos = player.position;
                var dx = world.x - pos.x;
                var dy = world.y - pos.y;
                var dz = world.z - pos.z;
                dist = Mathf.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
            }

            // AR projects every LateUpdate; only rebuild the string when the metre value moves.
            var meters = LocoRadarDisplay.CaptionMeters(dist);
            var cached = Captions[index];
            if (cached == null || CaptionMeterKeys[index] != meters)
            {
                cached = LocoRadarDisplay.FormatCaption(TypeIds[index], dist);
                CaptionMeterKeys[index] = meters;
                Captions[index] = cached;
            }

            caption = cached;
            return true;
        }

        private static int CountLive()
        {
            var live = 0;
            for (var i = 0; i < _count; i++)
            {
                if (RankedCars[i] != null)
                {
                    live++;
                }
            }

            return live;
        }

        private static void RunScan(
            LocoRadarScanReason reason,
            string? city,
            int? leftLocoId,
            System.Action<string>? log)
        {
            _count = 0;
            ClearSlots();

            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                log?.Invoke(
                    LocoRadarTelemetry.FormatScan(
                        reason, city, leftLocoId, 0, 0, 0, 0, fotMs: 0));
                return;
            }

            var origin = player.position;
            var fotSw = Stopwatch.StartNew();
            TrainCar[] allCars;
            try
            {
                allCars = Object.FindObjectsOfType<TrainCar>() ?? System.Array.Empty<TrainCar>();
            }
            catch
            {
                fotSw.Stop();
                log?.Invoke(
                    LocoRadarTelemetry.FormatScan(
                        reason, city, leftLocoId, 0, 0, 0, 0, fotSw.ElapsedMilliseconds));
                return;
            }

            fotSw.Stop();
            var fotMs = fotSw.ElapsedMilliseconds;

            Exclude.Clear();
            CollectExclusions(Exclude);
            if (leftLocoId.HasValue)
            {
                Exclude.Add(leftLocoId.Value);
            }

            var candCount = 0;
            var exclCount = 0;
            var unlicCount = 0;
            var hadUnknown = false;
            var sawLocoCars = false;
            for (var i = 0; i < allCars.Length && candCount < CandidateCap; i++)
            {
                var car = allCars[i];
                if (car == null || !car.IsLoco)
                {
                    continue;
                }

                sawLocoCars = true;

                int id;
                try
                {
                    id = car.GetInstanceID();
                }
                catch
                {
                    continue;
                }

                if (Exclude.Contains(id))
                {
                    exclCount++;
                    continue;
                }

                var verdict = EvaluateLocoLicense(car);
                if (LocoRadarLicenseGate.FilterEnabled
                    && verdict == LocoRadarLicenseVerdict.Unknown)
                {
                    hadUnknown = true;
                }

                if (!LocoRadarLicenseGate.ShouldShow(verdict))
                {
                    unlicCount++;
                    continue;
                }

                Vector3 pos;
                try
                {
                    pos = car.transform.position;
                }
                catch
                {
                    continue;
                }

                var dx = pos.x - origin.x;
                var dy = pos.y - origin.y;
                var dz = pos.z - origin.z;
                Candidates[candCount] = new LocoRadarCandidate(id, (dx * dx) + (dy * dy) + (dz * dz));
                CandidateCars[candCount] = car;
                candCount++;
            }

            var n = candCount == 0
                ? 0
                : LocoRadarSelection.RankNearest(
                    Candidates,
                    excludeIds: null,
                    LocoRadarSelection.DefaultMaxResults,
                    RankedIds,
                    candCount);

            for (var r = 0; r < n; r++)
            {
                var id = RankedIds[r];
                TrainCar? found = null;
                for (var c = 0; c < candCount; c++)
                {
                    if (Candidates[c].Id == id)
                    {
                        found = CandidateCars[c];
                        break;
                    }
                }

                if (found == null)
                {
                    continue;
                }

                RankedCars[_count] = found;
                TypeIds[_count] = TryGetLocoTypeId(found);
                _count++;
            }

            if (LocoRadarScanPolicy.ShouldForceScanOnKnowledgeRetry(
                    hadUnknown,
                    sawZeroLocoCars: !sawLocoCars,
                    alreadyRetried: _unknownRetryUsed))
            {
                _pendingUnknownRetry = true;
                _unknownRetryAt = Time.unscaledTime + LocoRadarScanPolicy.LicenseUnknownRetrySeconds;
            }

            log?.Invoke(LocoRadarTelemetry.FormatScan(
                reason, city, leftLocoId, exclCount, unlicCount, candCount, _count, fotMs));
        }

        private static void ResetUnknownRetry()
        {
            _unknownRetryUsed = false;
            _pendingUnknownRetry = false;
            _unknownRetryAt = -1f;
            _playerLocoKnownScanUsed = false;
        }

        private static bool PlayerHasKnownLoco()
        {
            try
            {
                if (PlayerManager.LastLoco != null)
                {
                    return true;
                }

                var car = PlayerManager.Car;
                return car != null && car.IsLoco;
            }
            catch
            {
                return false;
            }
        }

        private static void CollectExclusions(HashSet<int> exclude)
        {
            AddTrainsetLocos(exclude, PlayerManager.Car);
            AddCar(exclude, PlayerManager.LastLoco);
            AddTrainsetLocos(exclude, UsableTrainProbe.TryGetUsableLoco());
        }

        private static void AddTrainsetLocos(HashSet<int> exclude, TrainCar? seed)
        {
            if (seed == null)
            {
                return;
            }

            AddCar(exclude, seed);
            try
            {
                var cars = seed.trainset != null ? seed.trainset.cars : null;
                if (cars == null)
                {
                    return;
                }

                for (var i = 0; i < cars.Count; i++)
                {
                    var c = cars[i];
                    if (c != null && c.IsLoco)
                    {
                        AddCar(exclude, c);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private static void AddCar(HashSet<int> exclude, TrainCar? car)
        {
            if (car == null)
            {
                return;
            }

            try
            {
                exclude.Add(car.GetInstanceID());
            }
            catch
            {
                // ignored
            }
        }

        private static int? TryGetOccupiedLocoInstanceId()
        {
            try
            {
                var car = PlayerManager.Car;
                if (car == null || !car.IsLoco)
                {
                    return null;
                }

                return car.GetInstanceID();
            }
            catch
            {
                return null;
            }
        }

        private static LocoRadarLicenseVerdict EvaluateLocoLicense(TrainCar car)
        {
            try
            {
                var livery = car.carLivery;
                var required = livery != null ? livery.requiredLicense : null;
                var lm = LicenseManager.Instance;
                var holds = required != null && lm != null && lm.IsGeneralLicenseAcquired(required);
                return LocoRadarLicenseGate.Evaluate(
                    hasLivery: livery != null,
                    requiresLicense: required != null,
                    licenseQueryReady: lm != null,
                    playerHoldsRequiredLicense: holds);
            }
            catch
            {
                return LocoRadarLicenseVerdict.Unknown;
            }
        }

        private static int CurrentLicenseKey()
        {
            try
            {
                var lm = LicenseManager.Instance;
                return lm == null ? 0 : lm.GetNumberOfAcquiredGeneralLicenses();
            }
            catch
            {
                return 0;
            }
        }

        private static string? TryGetLocoTypeId(TrainCar car)
        {
            try
            {
                if (!car.IsLoco)
                {
                    return null;
                }

                return car.carLivery?.parentType?.id ?? car.carLivery?.id;
            }
            catch
            {
                return null;
            }
        }

    }
}
