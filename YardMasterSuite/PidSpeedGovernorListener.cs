using System;
using DV.Simulation.Controllers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// <b>9.1</b> PID speed hold on an active Maps / Switch List leg. Three-Gate
    /// reverser to the current step, then bleed indy + train at crawl, then
    /// throttle + independent raise on overspeed (never dump on derail).
    /// Yields to <b>7.5</b>. Thermal ceiling from <b>7.2</b>.
    /// Runs after vanilla incremental input so the first notch is not snapped to 0.
    /// Writes via <see cref="OverridableBaseControl.MUOverride"/> so a cab
    /// <c>ControlBlocker</c> cannot no-op <c>Set</c> after thr-on (2.9.1.10:
    /// <c>thr-off</c> with levers stuck at <c>thr=9 indy=0</c>).
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class PidSpeedGovernorListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private PidSpeedLogCache _log;
        private PidSpeedThrCache _thrLog;
        private PidSpeedApplyCache _applyLog;
        private PidSpeedState _pid;
        private Func<bool>? _softWrite;
        private OverridableBaseControl? _pendingThrottle;
        private OverridableBaseControl? _pendingInd;
        private OverridableBaseControl? _pendingTrain;
        private OverridableBaseControl? _pendingReverser;
        private float _desiredThrottle;
        private float _desiredInd;
        private float _desiredTrain;
        private float _desiredReverser;
        private bool _writeThrottle;
        private bool _writeInd;
        private bool _writeTrain;
        private bool _writeReverser;
        private float? _postedKmh;
        private PathGraphMapper? _graph;

        private void OnEnable()
        {
            _log = default;
            _thrLog = default;
            _applyLog = default;
            _pid = default;
            _softWrite = ApplyPending;
            _postedKmh = null;
            _graph = GetComponent<PathGraphMapper>();
            ClearPending();
            YmsEventBus.OnPostedLimitChanged += OnPosted;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPostedLimitChanged -= OnPosted;
            _log = default;
            _thrLog = default;
            _applyLog = default;
            _pid = default;
            _softWrite = null;
            _postedKmh = null;
            _graph = null;
            ClearPending();
        }

        private void OnPosted(PostedLimitSnapshot snapshot) => _postedKmh = snapshot.Kmh;

        private void FixedUpdate()
        {
            try
            {
                Tick();
            }
            catch
            {
                Emit(PidSpeedMode.Idle, wantThr: false);
            }
        }

        private void Tick()
        {
            var worldReady = ScreenOverlayGate.WorldReady();
            var worldActive = HudWorldSession.IsActive(
                PlayerManager.PlayerTransform != null,
                worldReady);
            var overlayClear = worldReady && !ScreenOverlayGate.IsBlocking();
            var loco = worldActive ? UsableTrainProbe.TryGetUsableLoco() : null;
            var hasLoco = loco != null && loco.IsLoco;

            float? derailPercent = null;
            MotorStatus? motors = null;
            MotorCabTempBand? band = null;
            if (hasLoco)
            {
                derailPercent = DerailRiskReader.ReadConsist(loco).MaxPercent;
                LocoSimReader.ReadPower(loco!, out _, out _, out _, out motors);
                band = LocoSimReader.TryReadCabTempBand(loco!);
            }

            var derail = LimitThrottleCap.ShouldIntervene(derailPercent);
            var switchList = SwitchListSession.HasActive && !SwitchListSession.IsComplete;
            var plan = RoutePlanSession.Plan;
            var facingReady = PidSpeedFacing.FacingReady(
                switchList,
                RoutePinLatch.HasLatch,
                RoutePlanSession.HasPlan);
            var goActive = SwitchListRunner.PidGoActive(
                SwitchListRunnerSession.Mode,
                SwitchListSession.CurrentStep);
            var armed = PidSpeedArm.IsArmed(
                goActive,
                RouteDestSession.HasDestination,
                switchList,
                facingReady,
                PidCruiseSession.Enabled);
            var pinStep = PidSpeedFacing.PinStepActive(
                RoutePinLatch.ShowPin,
                SwitchListRouteLeg.ShouldArmPin(plan),
                RoutePinLatch.DisplayDismissed);
            var destBehind = RouteFacingResolver.IsDestBehind(plan, _graph);
            var legReverse = PidSpeedFacing.LegNeedsReverse(
                pinStep,
                RoutePinLatch.TravelUsesReverse,
                destBehind);

            var controls = hasLoco ? loco!.SimController?.controlsOverrider : null;
            var throttle = controls?.Throttle;
            var ind = controls?.IndependentBrake;
            var train = controls?.Brake;
            var reverser = controls?.Reverser;
            var controlsPresent = throttle != null
                || ind != null
                || train != null
                || reverser != null;
            var throttleVal = throttle != null ? throttle.Value : 0f;
            var indVal = ind != null ? ind.Value : 0f;
            var trainVal = train != null ? train.Value : 0f;
            var reverserVal = reverser != null
                ? reverser.Value
                : ProximityTravelDirectionGate.NeutralValue;
            var speedKmh = hasLoco
                ? SpeedDisplay.ToKilometersPerHour(loco!.GetAbsSpeed())
                : 0f;
            var motorsDead = motors == MotorStatus.Dead;
            var ceiling = ThermalThrottleCap.CeilingForMotors(motors, band);

            var cmd = PidSpeedHold.Tick(
                new PidSpeedInput(
                    Time.fixedDeltaTime,
                    speedKmh,
                    PidSpeedTarget.DefaultRequestKmh,
                    _postedKmh,
                    throttleVal,
                    indVal,
                    armed,
                    derail,
                    ceiling,
                    reverserVal,
                    legReverse,
                    trainVal),
                ref _pid);

            var mode = PidSpeedTelemetry.Mode(
                armed,
                derail,
                cmd.GearPending,
                cmd.BrakePending,
                motorsDead,
                _pid.WaitCrawl);
            var wantThr = PidSpeedTelemetry.WantsThrottle(
                armed,
                cmd.GearPending,
                cmd.BrakePending,
                cmd.DesiredThrottle);
            if (!cmd.Active || !hasLoco)
            {
                Emit(mode, wantThr);
                return;
            }

            var leverThr = throttleVal;
            var leverInd = indVal;
            PidSpeedCab.Apply(cmd, wantThr, ref leverThr, ref leverInd);
            _desiredThrottle = leverThr;
            _desiredInd = leverInd;
            _desiredTrain = cmd.DesiredTrain;
            _desiredReverser = cmd.DesiredReverser;
            _writeReverser = armed
                && reverser != null
                && !PidSpeedGear.Matches(reverserVal, legReverse);
            _writeThrottle = throttle != null
                && Math.Abs(leverThr - throttleVal) > PidSpeedNotch.ExactEpsilon;
            _writeInd = ind != null
                && Math.Abs(leverInd - indVal) > PidSpeedNotch.ExactEpsilon;
            _writeTrain = train != null
                && cmd.BrakePending
                && LimitThrottleCap.ShouldLower(trainVal, _desiredTrain);
            if (!_writeThrottle && !_writeInd && !_writeTrain && !_writeReverser)
            {
                Emit(mode, wantThr);
                return;
            }

            // MUOverride bypasses ControlBlocker; do not abort the whole tick when
            // one lever reports blocked (2.9.1.10 thr-off with no controls line).
            if (!overlayClear || !controlsPresent)
            {
                var skip = PidSpeedTelemetry.NextSkip(
                    overlay: !overlayClear,
                    ref _applyLog);
                if (skip != null)
                {
                    EmitLog?.Invoke(skip);
                }

                Emit(mode, wantThr);
                return;
            }

            _pendingThrottle = throttle;
            _pendingInd = ind;
            _pendingTrain = train;
            _pendingReverser = reverser;
            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, hasLoco),
                ThreeGateWrite.StateRegistry(controlsPresent),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: true),
                _softWrite!);
            if (!result.Applied)
            {
                var skip = PidSpeedTelemetry.NextSkip(overlay: false, ref _applyLog);
                if (skip != null)
                {
                    EmitLog?.Invoke(skip);
                }

                Emit(mode, wantThr);
                return;
            }

            var apply = PidSpeedTelemetry.NextApply(
                _writeThrottle ? _desiredThrottle : throttleVal,
                _writeInd ? _desiredInd : indVal,
                ref _applyLog);
            if (apply != null)
            {
                EmitLog?.Invoke(apply);
            }

            Emit(mode, wantThr);
        }

        private bool ApplyPending()
        {
            if (_writeReverser)
            {
                _pendingReverser!.MUOverride(_desiredReverser);
            }

            if (_writeThrottle)
            {
                _pendingThrottle!.MUOverride(PidSpeedNotch.Snap(_desiredThrottle));
            }

            if (_writeInd)
            {
                // Do not Hud-round: ApproachBrake bleed is continuous (0.996→…).
                // Hud(0.996)=1.0 stuck indy at 100 (2.9.1.11 set-dest no move).
                _pendingInd!.MUOverride(_desiredInd);
            }

            if (_writeTrain)
            {
                _pendingTrain!.MUOverride(_desiredTrain);
            }

            return true;
        }

        private void Emit(PidSpeedMode mode, bool wantThr)
        {
            var line = PidSpeedTelemetry.NextLog(mode, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }

            var thr = PidSpeedTelemetry.NextThr(wantThr, ref _thrLog);
            if (thr != null)
            {
                EmitLog?.Invoke(thr);
            }
        }

        private void ClearPending()
        {
            _pendingThrottle = null;
            _pendingInd = null;
            _pendingTrain = null;
            _pendingReverser = null;
            _writeThrottle = false;
            _writeInd = false;
            _writeTrain = false;
            _writeReverser = false;
        }
    }
}
