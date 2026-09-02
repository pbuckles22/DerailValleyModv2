using System;
using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Inventory-gated job-car AR pins (6.21). One pin per spur; world pos is the
    /// nearest live task car in the camera FOV (adjacent cars count even
    /// when their origin is off-axis). Else nearest. Hide when taken GO.
    /// </summary>
    internal static class JobCarArProbe
    {
        private const int CarsPerSlot = 16;

        private static readonly List<Job> HeldJobs = new List<Job>(8);
        private static readonly HashSet<Job> HeldSeen = new HashSet<Job>();
        private static readonly List<Car> ExpectedLogic = new List<Car>(16);
        private static readonly List<int> ExpectedIds = new List<int>(16);
        private static readonly List<TrainCar> TaskCars = new List<TrainCar>(16);
        private static readonly List<int> AttachedIds = new List<int>(16);
        private static readonly JobCarPickupAccum[] Groups =
            new JobCarPickupAccum[JobCarPickupGroups.AccumCapacity];
        private static readonly JobCarPickupMarker[] Ranked =
            new JobCarPickupMarker[JobCarPickupGroups.DefaultMaxMarkers];
        private static readonly JobCarPickupSample[] Samples =
            new JobCarPickupSample[JobCarPickupGroups.SampleCapacity];
        private static readonly TrainCar?[] SampleCars =
            new TrainCar?[JobCarPickupGroups.SampleCapacity];
        private static readonly TrainCar?[] SlotCars =
            new TrainCar?[JobCarMarkerDisplay.DefaultMaxMarkers * CarsPerSlot];
        private static readonly int[] SlotCarCounts = new int[JobCarMarkerDisplay.DefaultMaxMarkers];
        private static readonly string?[] TrackLabels = new string?[JobCarMarkerDisplay.DefaultMaxMarkers];
        private static readonly int[] CarCounts = new int[JobCarMarkerDisplay.DefaultMaxMarkers];
        private static readonly string?[] Captions = new string?[JobCarMarkerDisplay.DefaultMaxMarkers];
        private static readonly int[] CaptionMeterKeys = new int[JobCarMarkerDisplay.DefaultMaxMarkers];

        private static int _count;
        private static JobCarPinLogCache _pinLog;
        private static string? _scannedHeldJobId;
        private static string? _jobId;
        private static int _expectedCars;
        private static JobConsistStatus _status;
        private static float _nextEnsureAt;

        internal static int Count => _count;

        internal static void Clear()
        {
            ClearPins();
            _pinLog = default;
            _scannedHeldJobId = null;
            _status = JobConsistStatus.Missing;
            _nextEnsureAt = 0f;
            HeldJobs.Clear();
            HeldSeen.Clear();
            ExpectedLogic.Clear();
            ExpectedIds.Clear();
            TaskCars.Clear();
            AttachedIds.Clear();
        }

        internal static void Ensure(Action<string>? log)
        {
            var now = Time.unscaledTime;
            if (now < _nextEnsureAt)
            {
                return;
            }

            _nextEnsureAt = now + 0.25f;

            string? heldJobId = null;
            Job? heldJob = null;
            if (JobPrepReader.TryFillHeldJobs(HeldJobs, HeldSeen, includingDropped: false)
                && HeldJobs.Count > 0
                && HeldJobs[0] != null)
            {
                heldJob = HeldJobs[0];
                heldJobId = heldJob.ID;
            }

            var reason = JobCarArScanPolicy.Decide(_scannedHeldJobId, heldJobId);
            var jobTaken = IsTakenJob(heldJobId);
            if (reason == JobCarArScanReason.Keep)
            {
                // Keep = same paperwork. Rebuild still runs so GO-hide and
                // live car IDs update without waiting for a ticket swap.
                if (heldJob != null && !string.IsNullOrEmpty(heldJobId))
                {
                    var prevCount = _count;
                    Rebuild(heldJob, heldJobId!, jobTaken);
                    if (prevCount > 0 && _count == 0)
                    {
                        log?.Invoke(JobCarTelemetry.FormatHide(heldJobId));
                    }

                    EmitPins(log);
                }

                return;
            }

            if (reason == JobCarArScanReason.Clear || heldJob == null || string.IsNullOrEmpty(heldJobId))
            {
                var hadPins = _count > 0 || _scannedHeldJobId != null;
                ClearPins();
                _scannedHeldJobId = null;
                if (hadPins)
                {
                    log?.Invoke(JobCarTelemetry.FormatClear());
                    EmitPins(log);
                }

                return;
            }

            _scannedHeldJobId = heldJobId;
            Rebuild(heldJob, heldJobId!, jobTaken);
            log?.Invoke(JobCarTelemetry.FormatScan(heldJobId, jobTaken, _count));
            EmitPins(log);
        }

        private static void EmitPins(Action<string>? log)
        {
            var line = JobCarTelemetry.NextPins(_count, TrackLabels, ref _pinLog);
            if (line != null)
            {
                log?.Invoke(line);
            }
        }

        internal static bool TryGet(int index, out Vector3 world, out string caption)
        {
            world = default;
            caption = "";
            if (index < 0 || index >= _count)
            {
                return false;
            }

            var player = PlayerManager.PlayerTransform;
            var px = 0f;
            var py = 0f;
            var pz = 0f;
            if (player != null)
            {
                var pos = player.position;
                px = pos.x;
                py = pos.y;
                pz = pos.z;
            }

            var haveLook = TryGetLook(
                out var lx, out var ly, out var lz,
                out var fx, out var fy, out var fz,
                out var minCosFov);
            if (haveLook)
            {
                px = lx;
                py = ly;
                pz = lz;
            }

            if (!TryNearestCarWorld(
                    index, px, py, pz, haveLook, fx, fy, fz, minCosFov, out world))
            {
                return false;
            }

            var dx = world.x - px;
            var dy = world.y - py;
            var dz = world.z - pz;
            var dist = Mathf.Sqrt((dx * dx) + (dy * dy) + (dz * dz));

            var meters = JobCarMarkerDisplay.CaptionMeters(dist);
            var cached = Captions[index];
            if (cached == null || CaptionMeterKeys[index] != meters)
            {
                cached = JobCarMarkerDisplay.FormatCaption(
                    _jobId,
                    TrackLabels[index],
                    CarCounts[index],
                    dist);
                CaptionMeterKeys[index] = meters;
                Captions[index] = cached;
            }

            caption = cached;
            return true;
        }

        private static void Rebuild(Job job, string jobId, bool jobTaken)
        {
            ClearPins();
            ExpectedLogic.Clear();
            ExpectedIds.Clear();
            _status = JobConsistProbe.Evaluate(job, SeedCar(), ExpectedLogic, ExpectedIds);
            _expectedCars = ExpectedLogic.Count;
            if (!JobCarMarkerDisplay.ShouldShowAr(jobTaken, _status, _expectedCars))
            {
                _jobId = jobId;
                return;
            }

            JobConsistProbe.FillTaskTrainCars(ExpectedLogic, TaskCars);
            AttachedIds.Clear();
            if (jobTaken)
            {
                JobConsistProbe.FillAttachedIds(SeedCar(), ExpectedIds, AttachedIds, out _);
            }

            var groupCount = 0;
            var sampleCount = 0;
            for (var i = 0; i < SampleCars.Length; i++)
            {
                SampleCars[i] = null;
            }

            for (var i = 0; i < TaskCars.Count; i++)
            {
                var car = TaskCars[i];
                if (car == null)
                {
                    continue;
                }

                int id;
                try
                {
                    id = car.GetInstanceID();
                }
                catch
                {
                    continue;
                }

                if (jobTaken && ContainsId(AttachedIds, id))
                {
                    continue;
                }

                Vector3 p;
                try
                {
                    p = car.transform.position;
                }
                catch
                {
                    continue;
                }

                var spur = JobCarMarkerDisplay.ShortSpurLabel(TryGetTrackDisplay(car));
                var before = sampleCount;
                if (!JobCarPickupGroups.TryAddSample(
                        Groups, ref groupCount, Samples, ref sampleCount, spur, p.x, p.y, p.z))
                {
                    continue;
                }

                if (sampleCount > before)
                {
                    SampleCars[sampleCount - 1] = car;
                }
            }

            float px = 0f, py = 0f, pz = 0f;
            var havePlayer = false;
            var player = PlayerManager.PlayerTransform;
            if (player != null)
            {
                var pos = player.position;
                px = pos.x;
                py = pos.y;
                pz = pos.z;
                havePlayer = true;
            }

            var n = JobCarPickupGroups.RankNearest(
                Groups, groupCount, havePlayer, px, py, pz, Ranked);
            _jobId = jobId;
            FillSlots(n, sampleCount);
        }

        private static void FillSlots(int rankedCount, int sampleCount)
        {
            _count = 0;
            for (var i = 0; i < SlotCarCounts.Length; i++)
            {
                SlotCarCounts[i] = 0;
                TrackLabels[i] = null;
                CarCounts[i] = 0;
                Captions[i] = null;
                CaptionMeterKeys[i] = -1;
            }

            for (var i = 0; i < SlotCars.Length; i++)
            {
                SlotCars[i] = null;
            }

            for (var slot = 0; slot < rankedCount && slot < Ranked.Length; slot++)
            {
                var marker = Ranked[slot];
                var filled = 0;
                var baseIndex = slot * CarsPerSlot;
                for (var s = 0; s < sampleCount && filled < CarsPerSlot; s++)
                {
                    if (Samples[s].GroupIndex != marker.GroupIndex)
                    {
                        continue;
                    }

                    SlotCars[baseIndex + filled] = SampleCars[s];
                    filled++;
                }

                if (filled == 0)
                {
                    continue;
                }

                TrackLabels[_count] = marker.TrackLabel;
                CarCounts[_count] = marker.Count;
                SlotCarCounts[_count] = filled;
                if (_count != slot)
                {
                    var destBase = _count * CarsPerSlot;
                    for (var c = 0; c < filled; c++)
                    {
                        SlotCars[destBase + c] = SlotCars[baseIndex + c];
                        SlotCars[baseIndex + c] = null;
                    }

                    SlotCarCounts[slot] = 0;
                }

                _count++;
            }
        }

        private static bool TryGetLook(
            out float px,
            out float py,
            out float pz,
            out float fx,
            out float fy,
            out float fz,
            out float minCosFov)
        {
            px = 0f;
            py = 0f;
            pz = 0f;
            fx = 0f;
            fy = 0f;
            fz = 0f;
            minCosFov = JobCarPickupGroups.DefaultMinCosFov;
            try
            {
                var cam = PlayerManager.ActiveCamera;
                if (cam == null)
                {
                    cam = Camera.main;
                }

                if (cam == null)
                {
                    return false;
                }

                var t = cam.transform;
                var p = t.position;
                px = p.x;
                py = p.y;
                pz = p.z;
                var f = t.forward;
                fx = f.x;
                fy = f.y;
                fz = f.z;
                var vHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                var aspect = cam.aspect;
                if (aspect < 0.1f)
                {
                    aspect = 16f / 9f;
                }

                var hHalf = Mathf.Atan(Mathf.Tan(vHalf) * aspect);
                minCosFov = Mathf.Cos(hHalf);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryNearestCarWorld(
            int slot,
            float playerX,
            float playerY,
            float playerZ,
            bool haveLook,
            float forwardX,
            float forwardY,
            float forwardZ,
            float minCosFov,
            out Vector3 world)
        {
            world = default;
            var n = SlotCarCounts[slot];
            if (n <= 0)
            {
                return false;
            }

            var baseIndex = slot * CarsPerSlot;
            if (haveLook
                && TryNearestCarWorld(
                    n,
                    baseIndex,
                    playerX,
                    playerY,
                    playerZ,
                    requireInView: true,
                    forwardX,
                    forwardY,
                    forwardZ,
                    minCosFov,
                    out world))
            {
                return true;
            }

            // Looking away (warehouse wall): nearest live car, overlay edge-clamps.

            return TryNearestCarWorld(
                n,
                baseIndex,
                playerX,
                playerY,
                playerZ,
                requireInView: false,
                forwardX,
                forwardY,
                forwardZ,
                minCosFov,
                out world);
        }

        private static bool TryNearestCarWorld(
            int n,
            int baseIndex,
            float playerX,
            float playerY,
            float playerZ,
            bool requireInView,
            float forwardX,
            float forwardY,
            float forwardZ,
            float minCosFov,
            out Vector3 world)
        {
            world = default;
            var found = false;
            var bestSq = 0f;
            for (var i = 0; i < n; i++)
            {
                var car = SlotCars[baseIndex + i];
                if (car == null)
                {
                    continue;
                }

                Vector3 p;
                try
                {
                    p = car.transform.position;
                }
                catch
                {
                    continue;
                }

                var dx = p.x - playerX;
                var dy = p.y - playerY;
                var dz = p.z - playerZ;
                if (requireInView
                    && !JobCarPickupGroups.IsInView(
                        dx,
                        dy,
                        dz,
                        forwardX,
                        forwardY,
                        forwardZ,
                        JobCarPickupGroups.InViewMinForward,
                        JobCarPickupGroups.DefaultAdjacentMeters,
                        minCosFov))
                {
                    continue;
                }

                var sq = (dx * dx) + (dy * dy) + (dz * dz);
                if (!found || sq < bestSq)
                {
                    found = true;
                    bestSq = sq;
                    world = p;
                }
            }

            return found;
        }

        private static void ClearPins()
        {
            _count = 0;
            _jobId = null;
            _expectedCars = 0;
            for (var i = 0; i < TrackLabels.Length; i++)
            {
                TrackLabels[i] = null;
                CarCounts[i] = 0;
                Captions[i] = null;
                CaptionMeterKeys[i] = -1;
                SlotCarCounts[i] = 0;
            }

            for (var i = 0; i < SlotCars.Length; i++)
            {
                SlotCars[i] = null;
            }

            for (var i = 0; i < SampleCars.Length; i++)
            {
                SampleCars[i] = null;
            }
        }

        private static bool IsTakenJob(string? heldJobId)
        {
            if (string.IsNullOrEmpty(heldJobId))
            {
                return false;
            }

            try
            {
                var jobs = JobsManager.Instance?.currentJobs;
                if (jobs == null)
                {
                    return false;
                }

                for (var i = 0; i < jobs.Count; i++)
                {
                    var candidate = jobs[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    string? state = null;
                    try
                    {
                        state = candidate.State.ToString();
                    }
                    catch
                    {
                        state = null;
                    }

                    if (ActiveJobHudLine.IsCancelledState(state))
                    {
                        continue;
                    }

                    if (string.Equals(candidate.ID, heldJobId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static TrainCar? SeedCar()
        {
            try
            {
                var boarded = PlayerManager.Car;
                if (boarded != null)
                {
                    return boarded;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                var last = PlayerManager.LastLoco;
                if (last != null)
                {
                    return last;
                }
            }
            catch
            {
                // fall through
            }

            return UsableTrainProbe.TryGetUsableLoco() ?? UsableTrainProbe.TryGetTargetCar();
        }

        private static string? TryGetTrackDisplay(TrainCar car)
        {
            try
            {
                var id = car.logicCar?.CurrentTrack?.ID;
                var display = id?.FullDisplayID?.Trim();
                if (!string.IsNullOrEmpty(display))
                {
                    return display;
                }

                return id?.FullID?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static bool ContainsId(List<int> ids, int id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
