using System;
using DV.MultipleUnit;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// <b>7.4</b> Auto-coupler — fail-closed couple assist while on the consist.
    /// Travel aim + green ≤0.5 m crawl (not 1.5 m scan) → Three-Gate TryCouple; incomplete link → finish
    /// hose / cocks / chain. MU is best-effort. No off-train remote.
    /// Cached write delegate; 10 Hz sample (no per-tick lambda).
    /// </summary>
    public sealed class AutoCouplerListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private const float SampleSeconds = 0.1f;

        private AutoCoupleLogCache _log;
        private Func<bool>? _softWrite;
        private Coupler? _pendingTip;
        private AutoCoupleAction _pendingAction;
        private float _nextAt;

        private void OnEnable()
        {
            _log = default;
            _softWrite = ApplyPending;
            _pendingTip = null;
            _pendingAction = AutoCoupleAction.None;
            _nextAt = 0f;
        }

        private void OnDisable()
        {
            _log = default;
            _softWrite = null;
            _pendingTip = null;
            _pendingAction = AutoCoupleAction.None;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime < _nextAt)
            {
                return;
            }

            _nextAt = Time.unscaledTime + SampleSeconds;
            try
            {
                Tick();
            }
            catch
            {
                Emit(false, linkComplete: false, AutoCoupleAction.None, ThreeGateAbortReason.SoftWrite);
            }
        }

        private void Tick()
        {
            var worldReady = ScreenOverlayGate.WorldReady();
            var worldActive = HudWorldSession.IsActive(
                PlayerManager.PlayerTransform != null,
                worldReady);
            var overlayClear = worldReady && !ScreenOverlayGate.IsBlocking();
            TrainCar? standing = null;
            if (worldActive)
            {
                try
                {
                    standing = PlayerManager.Car;
                }
                catch
                {
                    standing = null;
                }
            }

            var playerOnCar = standing != null;
            if (!worldActive || !playerOnCar)
            {
                Emit(false, linkComplete: false, AutoCoupleAction.None, ThreeGateAbortReason.Integrity);
                return;
            }

            var aimLoco = TryGetAimLoco(standing!);
            var direction = ProximityTravelDirectionGate.FromReverser(TryGetReverserValue(aimLoco));
            var hasAim = AutoCoupleAssist.HasTravelAim(direction);
            var useFront = ProximityTravelDirectionGate.UseFrontTip(direction);
            var tip = hasAim && aimLoco != null
                ? TryGetApproachTipCoupler(aimLoco, useFront)
                : null;
            var sameSet = AutoCoupleAssist.ActorOnConsist(
                playerOnCar,
                standingInSameTrainset: tip != null && SameTrainset(standing, tip.train));
            var hasTip = tip != null && sameSet;
            var mech = hasTip && tip!.IsCoupled();
            var partner = hasTip && !mech
                ? tip!.GetFirstCouplerInRange(Coupler.COUPLING_SCAN_RANGE)
                : null;
            var partnerInRange = partner != null;
            float? clearance = null;
            if (partner != null)
            {
                clearance = Vector3.Distance(
                    CouplerClearancePoint(tip!),
                    CouplerClearancePoint(partner));
            }

            var speedKmh = 0f;
            try
            {
                if (aimLoco != null)
                {
                    speedKmh = SpeedDisplay.ToKilometersPerHour(aimLoco.GetAbsSpeed());
                }
            }
            catch
            {
                speedKmh = AutoCoupleAssist.MaxCoupleSpeedKmh + 1f;
            }

            var complete = hasTip && IsLinkComplete(tip!);
            var prevent = hasTip && IsPreventCouple(tip!, partner ?? tip!.GetCoupled() ?? tip.coupledTo);
            var action = AutoCoupleAssist.Decide(
                hasAim,
                hasTip,
                partnerInRange,
                mech,
                complete,
                AutoCoupleAssist.ClearanceAllowsCouple(clearance),
                AutoCoupleAssist.SpeedAllowsCouple(speedKmh));
            var safe = AutoCoupleAssist.IsSafeToWrite(
                worldActive,
                actorOnConsist: sameSet,
                tipPresent: hasTip,
                preventCouple: prevent,
                overlayClear,
                action);

            if (!safe)
            {
                var abort = complete
                    ? ThreeGateAbortReason.None
                    : EndAbort(worldActive, sameSet, hasTip, overlayClear, prevent);
                Emit(false, complete, AutoCoupleAction.None, abort);
                return;
            }

            _pendingTip = tip;
            _pendingAction = action;
            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, sameSet),
                ThreeGateWrite.StateRegistry(hasTip),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: !prevent),
                _softWrite!);

            if (!result.Applied)
            {
                Emit(false, complete, action, result.AbortReason);
                return;
            }

            var nowComplete = tip != null && IsLinkComplete(tip);
            Emit(true, linkComplete: false, action, ThreeGateAbortReason.None);
            if (nowComplete)
            {
                Emit(true, linkComplete: true, action, ThreeGateAbortReason.None);
            }
        }

        private bool ApplyPending()
        {
            var tip = _pendingTip;
            if (tip == null)
            {
                return false;
            }

            if (_pendingAction == AutoCoupleAction.Couple)
            {
                if (!tip.IsCoupled())
                {
                    tip.TryCouple(playAudio: true);
                }

                if (!tip.IsCoupled())
                {
                    return false;
                }

                TryConnectMu(tip);
                return true;
            }

            if (_pendingAction == AutoCoupleAction.Finish)
            {
                FinishLink(tip);
                TryConnectMu(tip);
                return true;
            }

            return false;
        }

        private static void FinishLink(Coupler tip)
        {
            var other = tip.GetCoupled() ?? tip.coupledTo;
            if (other == null)
            {
                return;
            }

            if (!tip.IsTightened())
            {
                tip.SetChainTight(tight: true);
            }

            if (tip.GetAirHoseConnectedTo() == null)
            {
                tip.ConnectAirHose(other, playAudio: true);
            }

            try
            {
                tip.IsCockOpen = true;
                other.IsCockOpen = true;
            }
            catch
            {
                // fail closed on cock write
            }
        }

        private static void TryConnectMu(Coupler tip)
        {
            var other = tip.GetCoupled() ?? tip.coupledTo;
            if (other == null)
            {
                return;
            }

            try
            {
                MultipleUnitModule.ConnectCablesOfConnectedCouplersIfMultipleUnitSupported(tip, other);
            }
            catch
            {
                // best-effort
            }
        }

        private static bool IsLinkComplete(Coupler tip)
        {
            var status = CouplerProbe.TryGetLinkStatus(tip);
            return status.HasValue && CouplingLink.IsUsable(status.Value);
        }

        private static bool IsPreventCouple(Coupler tip, Coupler? other)
        {
            try
            {
                if (tip.train != null && tip.train.preventCouple)
                {
                    return true;
                }

                return other?.train != null && other.train.preventCouple;
            }
            catch
            {
                return true;
            }
        }

        private static bool SameTrainset(TrainCar? a, TrainCar? b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            try
            {
                return a.trainset != null && a.trainset == b.trainset;
            }
            catch
            {
                return false;
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

            return t.position + (fwd.normalized * -0.25f);
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

        private static TrainCar? TryGetAimLoco(TrainCar standing)
        {
            try
            {
                if (standing.IsLoco)
                {
                    return standing;
                }

                var cars = standing.trainset?.cars;
                if (cars == null || cars.Count == 0)
                {
                    return null;
                }

                TrainCar? best = null;
                var bestIndex = int.MaxValue;
                for (var i = 0; i < cars.Count; i++)
                {
                    var c = cars[i];
                    if (c == null || !c.IsLoco)
                    {
                        continue;
                    }

                    if (c.indexInTrainset < bestIndex)
                    {
                        bestIndex = c.indexInTrainset;
                        best = c;
                    }
                }

                return best;
            }
            catch
            {
                return null;
            }
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

                Consider(c.frontCoupler, allowCoupled: false);
                Consider(c.rearCoupler, allowCoupled: false);
            }

            if (best != null)
            {
                return best;
            }

            for (var i = 0; i < set.Count; i++)
            {
                var c = set[i];
                if (c == null)
                {
                    continue;
                }

                Consider(c.frontCoupler, allowCoupled: true);
                Consider(c.rearCoupler, allowCoupled: true);
            }

            return best;

            void Consider(Coupler? coupler, bool allowCoupled)
            {
                if (coupler == null)
                {
                    return;
                }

                if (coupler.IsCoupled() != allowCoupled)
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

        private static ThreeGateAbortReason EndAbort(
            bool worldActive,
            bool actorOnConsist,
            bool hasTip,
            bool overlayClear,
            bool prevent)
        {
            if (!worldActive || !actorOnConsist)
            {
                return ThreeGateAbortReason.Integrity;
            }

            if (!hasTip)
            {
                return ThreeGateAbortReason.StateRegistry;
            }

            if (!overlayClear || prevent)
            {
                return ThreeGateAbortReason.Safety;
            }

            return ThreeGateAbortReason.Safety;
        }

        private void Emit(
            bool applied,
            bool linkComplete,
            AutoCoupleAction action,
            ThreeGateAbortReason abort)
        {
            var line = AutoCoupleTelemetry.NextLog(applied, linkComplete, action, abort, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
                if (line == AutoCoupleTelemetry.Done)
                {
                    MapsDeskPanel.TryAdvanceAfterCoupleSuccess();
                }
            }
        }
    }
}
