using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Rear/Front clearance on the loco bar (**6.18** / v1 4.11–4.12).
    /// 10 Hz sample; HUD string only when the caption key changes.
    /// Overlap/ray use NonAlloc buffers — no RaycastAll.
    /// </summary>
    public sealed class BackupProximityListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private const float SampleSeconds = 0.1f;
        private const float CouplerInsetMeters = 0.25f;
        private const float RayLiftMeters = 0.35f;

        private static readonly Collider[] OverlapHits = new Collider[128];
        private static readonly RaycastHit[] RayHits = new RaycastHit[64];

        private BackupProximityCache _cache;
        private float _nextAt;
        private float _lastChangeLogAt = -BackupProximityTelemetry.MinChangeLogSeconds;

        private void OnEnable()
        {
            _cache = default;
            _nextAt = 0f;
            _lastChangeLogAt = -BackupProximityTelemetry.MinChangeLogSeconds;
            PublishIfChanged();
        }

        private void OnDisable()
        {
            _cache = default;
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextAt)
            {
                return;
            }

            _nextAt = Time.unscaledTime + SampleSeconds;

            var boarded = PlayerManager.Car;
            var moving = boarded != null
                && RouteReverseHitchGate.ConsistIsMoving(
                    SpeedDisplay.ToKilometersPerHour(boarded.GetAbsSpeed()));
            if (RouteReverseHitchGate.QuietCabDuringPinReverse(
                    boarded != null && boarded.IsLoco,
                    RoutePinLatch.TravelUsesReverse,
                    RouteClearanceSession.Phase,
                    moving))
            {
                return;
            }

            PublishIfChanged();
        }

        private void PublishIfChanged()
        {
            TryRead(out var direction, out var meters, out var inRange, out var tipActive);
            var show = ProximityTravelDirectionGate.ShouldShowChip(direction);
            var key = BackupProximityTelemetry.CaptionKey(
                show,
                direction,
                meters,
                inRange,
                tipActive);
            if (!BackupProximityTelemetry.Observe(key, ref _cache))
            {
                return;
            }

            var chip = key == BackupProximityTelemetry.KeyOmit
                ? string.Empty
                : BackupProximityDisplay.FormatHud(
                    meters,
                    inRange,
                    tipActive,
                    ProximityTravelDirectionGate.ChipLabel(direction));
            YmsEventBus.RaiseBackupProximityChanged(new HudBarSnapshot(chip, visible: chip.Length > 0));

            var msg = BackupProximityTelemetry.NextLog(
                key,
                direction,
                meters,
                inRange,
                tipActive,
                Time.unscaledTime,
                ref _lastChangeLogAt);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        private static void TryRead(
            out ProximityTravelDirection direction,
            out float? clearanceMeters,
            out bool inCoupleRange,
            out bool tipActive)
        {
            direction = ProximityTravelDirection.Unknown;
            clearanceMeters = null;
            inCoupleRange = false;
            tipActive = false;

            try
            {
                var loco = UsableTrainProbe.TryGetUsableLoco();
                if (loco == null)
                {
                    return;
                }

                direction = ProximityTravelDirectionGate.FromReverser(TryGetReverserValue(loco));
                if (!ProximityTravelDirectionGate.ShouldShowChip(direction))
                {
                    return;
                }

                var useFront = ProximityTravelDirectionGate.UseFrontTip(direction);
                var coupler = TryGetApproachTipCoupler(loco, useFront);
                if (coupler == null || coupler.IsCoupled())
                {
                    return;
                }

                tipActive = true;
                var near = coupler.GetFirstCouplerInRange(Coupler.COUPLING_SCAN_RANGE);
                if (near != null)
                {
                    clearanceMeters = Vector3.Distance(
                        CouplerClearancePoint(coupler),
                        CouplerClearancePoint(near));
                    inCoupleRange = true;
                    return;
                }

                if (TryScanNearestForeignCoupler(coupler, out var hitMeters))
                {
                    clearanceMeters = hitMeters;
                    inCoupleRange = BackupProximityDisplay.IsInCoupleRange(hitMeters);
                }
            }
            catch
            {
                direction = ProximityTravelDirection.Unknown;
                clearanceMeters = null;
                inCoupleRange = false;
                tipActive = false;
            }
        }

        private static float? TryGetReverserValue(TrainCar? loco)
        {
            try
            {
                var reverser = loco?.SimController?.controlsOverrider?.Reverser;
                return reverser == null ? (float?)null : reverser.Value;
            }
            catch
            {
                return null;
            }
        }

        private static Vector3 CouplerClearancePoint(Coupler coupler)
        {
            var t = coupler.transform;
            var fwd = t.forward;
            if (fwd.sqrMagnitude < 1e-6f)
            {
                return t.position;
            }

            return t.position + (fwd.normalized * -CouplerInsetMeters);
        }

        private static Coupler? TryGetApproachTipCoupler(TrainCar loco, bool useFront)
        {
            var set = loco.trainset?.cars;
            if (set == null || set.Count == 0)
            {
                return null;
            }

            var locoFwd = loco.transform.forward;
            float ix, iy, iz;
            if (useFront)
            {
                BackupProximityAim.FrontIntent(
                    locoFwd.x, locoFwd.y, locoFwd.z, out ix, out iy, out iz);
            }
            else
            {
                BackupProximityAim.RearIntent(
                    locoFwd.x, locoFwd.y, locoFwd.z, out ix, out iy, out iz);
            }

            Coupler? best = null;
            var bestAlign = float.NegativeInfinity;

            for (var i = 0; i < set.Count; i++)
            {
                var c = set[i];
                if (c == null)
                {
                    continue;
                }

                Consider(c.frontCoupler);
                Consider(c.rearCoupler);
            }

            return best;

            void Consider(Coupler? coupler)
            {
                if (coupler == null || coupler.IsCoupled())
                {
                    return;
                }

                var opposite = coupler.GetOppositeCoupler();
                var isAlone = set.Count == 1;
                var oppositeCoupled = opposite != null && opposite.IsCoupled();
                if (!isAlone && !oppositeCoupled)
                {
                    return;
                }

                var o = coupler.transform.forward;
                var align = BackupProximityAim.TipAlignment(o.x, o.y, o.z, ix, iy, iz);
                if (align > bestAlign)
                {
                    bestAlign = align;
                    best = coupler;
                }
            }
        }

        private static bool TryScanNearestForeignCoupler(Coupler tip, out float meters)
        {
            meters = 0f;
            var origin = CouplerClearancePoint(tip);
            var tipOut = tip.transform.forward;
            if (tipOut.sqrMagnitude < 1e-6f)
            {
                return false;
            }

            tipOut.Normalize();
            const float maxDistance = BackupProximityDisplay.MaxDisplayMeters;
            var maxSq = maxDistance * maxDistance;

            int numHits;
            try
            {
                numHits = Physics.OverlapSphereNonAlloc(
                    origin,
                    maxDistance,
                    OverlapHits,
                    ~0,
                    QueryTriggerInteraction.Ignore);
            }
            catch
            {
                return false;
            }

            var ownSet = tip.train?.trainset;
            var nearestSq = maxSq;
            var found = false;

            for (var i = 0; i < numHits; i++)
            {
                var col = OverlapHits[i];
                if (col == null)
                {
                    continue;
                }

                var hitCar = TrainCar.Resolve(col.transform);
                if (hitCar == null || (ownSet != null && hitCar.trainset == ownSet))
                {
                    continue;
                }

                ConsiderCoupler(hitCar.frontCoupler);
                ConsiderCoupler(hitCar.rearCoupler);
            }

            try
            {
                var rayOrigin = tip.transform.position + (Vector3.up * RayLiftMeters);
                var rayCount = Physics.RaycastNonAlloc(
                    rayOrigin,
                    tipOut,
                    RayHits,
                    maxDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore);
                for (var i = 0; i < rayCount; i++)
                {
                    var hit = RayHits[i];
                    var hitCar = hit.collider != null ? TrainCar.Resolve(hit.collider.transform) : null;
                    if (hitCar == null || (ownSet != null && hitCar.trainset == ownSet))
                    {
                        continue;
                    }

                    var dSq = hit.distance * hit.distance;
                    if (dSq < nearestSq)
                    {
                        nearestSq = dSq;
                        found = true;
                    }
                }
            }
            catch
            {
                // keep overlap result
            }

            if (!found)
            {
                return false;
            }

            meters = Mathf.Sqrt(nearestSq);
            return true;

            void ConsiderCoupler(Coupler? other)
            {
                if (other == null || other.IsCoupled())
                {
                    return;
                }

                var otherPt = CouplerClearancePoint(other);
                var delta = otherPt - origin;
                var distSq = delta.sqrMagnitude;
                if (distSq > maxSq || distSq < 1e-6f)
                {
                    return;
                }

                if (!BackupProximityAim.IsInApproachCone(
                        delta.x, delta.y, delta.z, tipOut.x, tipOut.y, tipOut.z))
                {
                    return;
                }

                if (distSq < nearestSq)
                {
                    nearestSq = distSq;
                    found = true;
                }
            }
        }
    }
}
