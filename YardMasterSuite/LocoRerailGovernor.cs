using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Loco-only turn-in-place + re-rail place via native teleporter / Rerail (**8.6**).
    /// </summary>
    internal static class LocoRerailGovernor
    {
        private static readonly List<TrainCar> ScratchLocos = new(32);
        private static readonly List<string> ScratchTypes = new(16);

        internal static IReadOnlyList<string> ListTypesOnMap()
        {
            ScratchTypes.Clear();
            try
            {
                var spawner = CarSpawner.Instance;
                var locos = spawner != null ? spawner.AllLocos : null;
                if (locos == null)
                {
                    return Array.Empty<string>();
                }

                for (var i = 0; i < locos.Count; i++)
                {
                    var car = locos[i];
                    if (car == null || !car.IsLoco)
                    {
                        continue;
                    }

                    var label = LocoTypeId.DisplayLabel(TryTypeId(car));
                    if (label == "—" || ScratchTypes.Contains(label))
                    {
                        continue;
                    }

                    ScratchTypes.Add(label);
                }

                ScratchTypes.Sort(StringComparer.Ordinal);
                var copy = new string[ScratchTypes.Count];
                for (var i = 0; i < ScratchTypes.Count; i++)
                {
                    copy[i] = ScratchTypes[i];
                }

                return copy;
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private static int _latchedTurnLocoId;
        private static string _latchedTurnType = string.Empty;

        internal static string TurnLookAtInPlace(MonoBehaviour host)
        {
            if (host == null)
            {
                return "T2 loco-rerail: no host";
            }

            // Prefer live look-at; fall back to last LOOK latch (desk click aims at UI).
            var look = UsableTrainProbe.TryGetLookAtCar();
            var loco = look != null && look.IsLoco ? look : FindLatchedTurnLoco();
            if (loco != null && loco.IsLoco)
            {
                LatchTurnLoco(loco);
            }

            var consistCount = CountConsistCars(loco);
            var speed = AbsSpeedKmh(loco);
            var derailed = IsDerailed(loco);
            var abort = LocoRerailPolicy.EvaluateTurn(
                hasLoco: loco != null,
                consistCarCount: consistCount,
                maxAbsSpeedKmh: speed,
                isTeleporting: IsTeleportBusy(loco),
                isDerailed: derailed);

            if (!LocoRerailPolicy.CanApply(abort))
            {
                var reason = loco == null
                    ? "look at a loco"
                    : LocoRerailPolicy.FormatAbort(abort);
                var blocked = "T2 loco-rerail: turn abort · " + reason;
                MapsDeskPanel.EmitLog?.Invoke(blocked);
                return blocked;
            }

            var car = loco!;
            var track = LocoTrackProbe.ResolveTrack(car);
            if (track == null)
            {
                var noTrack = "T2 loco-rerail: turn abort · no track";
                MapsDeskPanel.EmitLog?.Invoke(noTrack);
                return noTrack;
            }

            // In-place 180° via MoveToTrack (Rerail is derail-recovery only — no-op on rails).
            // Do NOT use TeleportTrainset — it seeks clear space and slides past neighbors.
            var worldPos = car.transform.position;
            var forward = -car.transform.forward;
            var result = ThreeGate.TryApply(
                integrityOk: true,
                stateRegistryOk: CarSpawner.Instance != null,
                safetyOk: true,
                softWrite: () =>
                {
                    car.MoveToTrack(track, worldPos, forward);
                    return true;
                });

            if (!result.Applied)
            {
                var fail = "T2 loco-rerail: turn ThreeGate " + result.AbortReason;
                MapsDeskPanel.EmitLog?.Invoke(fail);
                return fail;
            }

            var ok = "T2 loco-rerail: turn · " + LocoTypeId.DisplayLabel(TryTypeId(car))
                     + " · id=" + car.GetInstanceID()
                     + " · MoveToTrack";
            MapsDeskPanel.EmitLog?.Invoke(ok);
            return ok;
        }

        internal static string FormatLookAtLocoChip()
        {
            var look = UsableTrainProbe.TryGetLookAtCar();
            if (look != null && look.IsLoco)
            {
                LatchTurnLoco(look);
                return "LOOK · " + LocoTypeId.DisplayLabel(TryTypeId(look));
            }

            if (_latchedTurnLocoId != 0 && FindLatchedTurnLoco() != null)
            {
                return "LOOK · " + _latchedTurnType + " (latched)";
            }

            if (look != null && !look.IsLoco)
            {
                return "LOOK · not a loco";
            }

            return "LOOK · — (point at a loco)";
        }

        private static void LatchTurnLoco(TrainCar loco)
        {
            _latchedTurnLocoId = loco.GetInstanceID();
            _latchedTurnType = LocoTypeId.DisplayLabel(TryTypeId(loco));
        }

        private static TrainCar? FindLatchedTurnLoco()
        {
            if (_latchedTurnLocoId == 0)
            {
                return null;
            }

            try
            {
                var locos = CarSpawner.Instance?.AllLocos;
                if (locos == null)
                {
                    return null;
                }

                for (var i = 0; i < locos.Count; i++)
                {
                    var car = locos[i];
                    if (car != null && car.GetInstanceID() == _latchedTurnLocoId)
                    {
                        return car;
                    }
                }
            }
            catch
            {
                // fail closed
            }

            return null;
        }

        internal static string BeginPlace(string typeLabel)
        {
            var normalized = LocoTypeId.Normalize(typeLabel);
            if (string.IsNullOrEmpty(normalized))
            {
                return "T2 loco-rerail: pick loco type";
            }

            var keepAim = LocoRerailSession.HasLatchedTarget;
            var ax = 0f;
            var ay = 0f;
            var az = 0f;
            string? track = null;
            if (keepAim)
            {
                track = LocoRerailSession.TargetTrackId;
                LocoRerailSession.TryGetAimPoint(out ax, out ay, out az);
            }

            LocoRerailSession.Begin(normalized);
            if (keepAim && !string.IsNullOrEmpty(track))
            {
                LocoRerailSession.UnlockTarget();
                LocoRerailSession.SetTarget(track, ax, ay, az);
            }

            PollPlaceTarget();
            var line = "T2 loco-rerail: place armed · " + normalized
                       + (LocoRerailSession.HasLatchedTarget
                           ? " · " + LocoRerailSession.TargetTrackId
                           : " · look at a track");
            MapsDeskPanel.EmitLog?.Invoke(line);
            return line;
        }

        internal static string CancelPlace()
        {
            LocoRerailSession.Clear();
            MapsDeskPanel.EmitLog?.Invoke("T2 loco-rerail: place cancelled");
            return "place cancelled";
        }

        internal static void PollPlaceTarget()
        {
            if (!LocoRerailSession.IsActive)
            {
                return;
            }

            const float maxLookMeters = 250f;
            const float maxTrackSnapMeters = 12f;

            try
            {
                var cam = PlayerManager.ActiveCamera ?? Camera.main;
                if (cam == null)
                {
                    LocoRerailSession.ClearTargetIfUnlocked();
                    return;
                }

                var tracks = RailTrackRegistry.Instance != null
                    ? RailTrackRegistry.Instance.AllTracks
                    : RailTrackRegistry.RailTracks;
                if (tracks == null || tracks.Length == 0)
                {
                    LocoRerailSession.ClearTargetIfUnlocked();
                    return;
                }

                var ray = new Ray(cam.transform.position, cam.transform.forward);
                Vector3 aim;
                if (Physics.Raycast(ray, out var hit, maxLookMeters, ~0, QueryTriggerInteraction.Ignore))
                {
                    aim = hit.point;
                }
                else
                {
                    aim = ray.GetPoint(40f);
                }

                RailTrack? best = null;
                var bestDist = float.MaxValue;
                for (var i = 0; i < tracks.Length; i++)
                {
                    var rail = tracks[i];
                    if (rail == null)
                    {
                        continue;
                    }

                    var pointDist = RailTrack.GetClosestPoint(rail, aim, 0f);
                    var dist = pointDist.Item2;
                    if (dist > maxTrackSnapMeters || dist >= bestDist)
                    {
                        continue;
                    }

                    bestDist = dist;
                    best = rail;
                }

                if (best == null)
                {
                    LocoRerailSession.ClearTargetIfUnlocked();
                    return;
                }

                LocoRerailSession.SetTarget(LogicTrackKey.FromRail(best), aim.x, aim.y, aim.z);
            }
            catch
            {
                LocoRerailSession.ClearTargetIfUnlocked();
            }
        }

        internal static string FormatActiveChip()
        {
            if (!LocoRerailSession.IsActive)
            {
                return string.Empty;
            }

            var typeId = LocoRerailSession.TypeId;
            var match = CountMatches(typeId, out _, out _);
            var source = SelectSource(typeId);
            var abort = LocoRerailPolicy.EvaluatePlace(
                hasTypeSelected: !string.IsNullOrEmpty(typeId),
                matchCount: match,
                selectedAbsSpeedKmh: AbsSpeedKmh(source),
                isTeleporting: IsTeleportBusy(source),
                hasTargetTrack: !string.IsNullOrEmpty(LocoRerailSession.TargetTrackId),
                selectedCoupled: IsCoupled(source),
                selectedDerailed: IsDerailed(source));

            return LocoRerailPolicy.FormatPlaceChip(
                true,
                typeId,
                LocoRerailSession.TargetTrackId,
                abort)
                   + (LocoRerailSession.IsTargetLocked ? " · LOCKED" : string.Empty);
        }

        internal static string LockAim()
        {
            if (!LocoRerailSession.IsActive)
            {
                return "T2 loco-rerail: place inactive";
            }

            PollPlaceTarget();
            if (!LocoRerailSession.HasLatchedTarget)
            {
                return "T2 loco-rerail: look at a track first";
            }

            LocoRerailSession.LockTarget();
            var line = "T2 loco-rerail: aim locked · " + LocoRerailSession.TargetTrackId;
            MapsDeskPanel.EmitLog?.Invoke(line);
            return line;
        }

        internal static string ConfirmPlace(MonoBehaviour host)
        {
            if (host == null)
            {
                return "T2 loco-rerail: no host";
            }

            if (!LocoRerailSession.IsActive)
            {
                return "T2 loco-rerail: place inactive";
            }

            var typeId = LocoRerailSession.TypeId;
            var match = CountMatches(typeId, out _, out _);
            var source = SelectSource(typeId);
            var abort = LocoRerailPolicy.EvaluatePlace(
                hasTypeSelected: !string.IsNullOrEmpty(typeId),
                matchCount: match,
                selectedAbsSpeedKmh: AbsSpeedKmh(source),
                isTeleporting: IsTeleportBusy(source),
                hasTargetTrack: !string.IsNullOrEmpty(LocoRerailSession.TargetTrackId),
                selectedCoupled: IsCoupled(source),
                selectedDerailed: IsDerailed(source));

            if (!LocoRerailPolicy.CanApply(abort) || source == null)
            {
                var detail = source == null && match > 0
                    ? LocoRerailPolicy.FormatAbort(LocoRerailAbort.Derailed)
                    : LocoRerailPolicy.FormatAbort(abort);
                var blocked = "T2 loco-rerail: place abort · " + detail;
                MapsDeskPanel.EmitLog?.Invoke(blocked);
                return blocked;
            }

            var trackKey = LocoRerailSession.TargetTrackId!;
            if (!TryGetRailTrack(trackKey, out var rail) || rail == null)
            {
                var noRail = "T2 loco-rerail: place abort · " + LocoRerailPolicy.FormatAbort(LocoRerailAbort.NoTarget);
                MapsDeskPanel.EmitLog?.Invoke(noRail);
                return noRail;
            }

            if (!LocoRerailSession.TryGetAimPoint(out var ax, out var ay, out var az))
            {
                var noAim = "T2 loco-rerail: place abort · look at a track";
                MapsDeskPanel.EmitLog?.Invoke(noAim);
                return noAim;
            }

            var aimPos = new Vector3(ax, ay, az);
            var target = new Vector3(aimPos.x, rail.transform.position.y, aimPos.z);
            var forceDir = LocoRerailSession.ForceRegularDirection;
            var cars = new List<TrainCar>(1) { source };
            var hostRef = host;
            var srcId = source.GetInstanceID();
            var srcType = LocoTypeId.DisplayLabel(TryTypeId(source));

            MapsDeskPanel.EmitLog?.Invoke(
                "T2 loco-rerail: place source · " + srcType + " · id=" + srcId
                + " · derailed=" + IsDerailed(source));

            var result = ThreeGate.TryApply(
                integrityOk: cars.Count == 1 && !IsDerailed(source),
                stateRegistryOk: CarSpawner.Instance != null,
                safetyOk: true,
                softWrite: () =>
                {
                    hostRef.StartCoroutine(TeleportThenClear(cars, target, forceDir));
                    return true;
                });

            if (!result.Applied)
            {
                var fail = "T2 loco-rerail: place ThreeGate " + result.AbortReason;
                MapsDeskPanel.EmitLog?.Invoke(fail);
                return fail;
            }

            var ok = "T2 loco-rerail: place started · " + typeId + " → " + trackKey;
            MapsDeskPanel.EmitLog?.Invoke(ok);
            return ok;
        }

        private static IEnumerator TeleportThenClear(
            List<TrainCar> cars,
            Vector3 target,
            bool forceRegularDirection)
        {
            yield return TrainCarTeleporter.TeleportTrainset(cars, target, forceRegularDirection);
            LocoRerailSession.Clear();
            MapsDeskPanel.EmitLog?.Invoke("T2 loco-rerail: place complete");
        }

        private static TrainCar? SelectSource(string typeId)
        {
            FillScratchLocos();
            var playerPos = PlayerManager.PlayerTransform != null
                ? PlayerManager.PlayerTransform.position
                : Vector3.zero;
            var playerYard = DestinationCatalog.YardIdFromTrackKey(
                LogicTrackKey.FromCar(PlayerManager.LastLoco)
                ?? LogicTrackKey.FromCar(PlayerManager.Car));

            var idx = LocoRerailPolicy.SelectSourceIndex(
                ScratchLocos.Count,
                i => LocoTypeId.Matches(TryTypeId(ScratchLocos[i]), typeId),
                i => IsCoupled(ScratchLocos[i]),
                i => IsDerailed(ScratchLocos[i]),
                i =>
                {
                    var yard = DestinationCatalog.YardIdFromTrackKey(LogicTrackKey.FromCar(ScratchLocos[i]));
                    return !string.IsNullOrEmpty(playerYard)
                           && string.Equals(yard, playerYard, StringComparison.OrdinalIgnoreCase);
                },
                i =>
                {
                    var p = ScratchLocos[i].transform.position;
                    var dx = p.x - playerPos.x;
                    var dz = p.z - playerPos.z;
                    return (dx * dx) + (dz * dz);
                });

            return idx >= 0 ? ScratchLocos[idx] : null;
        }

        private static bool IsDerailed(TrainCar? car)
        {
            if (car == null)
            {
                return false;
            }

            try
            {
                return car.derailed;
            }
            catch
            {
                return false;
            }
        }

        private static int CountMatches(string typeId, out TrainCar? sample, out bool coupled)
        {
            sample = null;
            coupled = false;
            FillScratchLocos();
            var n = 0;
            for (var i = 0; i < ScratchLocos.Count; i++)
            {
                var car = ScratchLocos[i];
                if (!LocoTypeId.Matches(TryTypeId(car), typeId))
                {
                    continue;
                }

                n++;
                if (sample == null)
                {
                    sample = car;
                    coupled = IsCoupled(car);
                }
            }

            return n;
        }

        private static void FillScratchLocos()
        {
            ScratchLocos.Clear();
            try
            {
                var locos = CarSpawner.Instance?.AllLocos;
                if (locos == null)
                {
                    return;
                }

                for (var i = 0; i < locos.Count; i++)
                {
                    var car = locos[i];
                    if (car != null && car.IsLoco)
                    {
                        ScratchLocos.Add(car);
                    }
                }
            }
            catch
            {
                ScratchLocos.Clear();
            }
        }

        private static string? TryTypeId(TrainCar car)
        {
            try
            {
                return car.carLivery?.parentType?.id ?? car.carLivery?.id;
            }
            catch
            {
                return null;
            }
        }

        private static int CountConsistCars(TrainCar? loco)
        {
            if (loco == null)
            {
                return 0;
            }

            try
            {
                var cars = loco.trainset?.cars;
                return cars != null ? cars.Count : 1;
            }
            catch
            {
                return 1;
            }
        }

        private static bool IsCoupled(TrainCar? car)
        {
            if (car == null)
            {
                return false;
            }

            try
            {
                var cars = car.trainset?.cars;
                return cars != null && cars.Count > 1;
            }
            catch
            {
                return false;
            }
        }

        private static float? AbsSpeedKmh(TrainCar? car)
        {
            if (car == null)
            {
                return null;
            }

            try
            {
                return Mathf.Abs(car.GetForwardSpeed() * 3.6f);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsTeleportBusy(TrainCar? car)
        {
            try
            {
                if (car != null && car.IsTeleporting)
                {
                    return true;
                }

                var field = typeof(TrainCarTeleporter).GetField(
                    "isTeleportingTrain",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.GetValue(null) is bool busy)
                {
                    return busy;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool TryGetRailTrack(string trackKey, out RailTrack? rail)
        {
            rail = null;
            try
            {
                var tracks = RailTrackRegistry.Instance != null
                    ? RailTrackRegistry.Instance.AllTracks
                    : RailTrackRegistry.RailTracks;
                if (tracks == null)
                {
                    return false;
                }

                for (var i = 0; i < tracks.Length; i++)
                {
                    var t = tracks[i];
                    if (t == null)
                    {
                        continue;
                    }

                    var key = LogicTrackKey.FromRail(t);
                    if (string.Equals(key, trackKey, StringComparison.OrdinalIgnoreCase))
                    {
                        rail = t;
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
    }
}
