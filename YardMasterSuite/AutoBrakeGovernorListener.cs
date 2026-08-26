using System;
using DV.Simulation.Controllers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// <b>7.3</b> Auto-brake — engine on→off soft-rolls train + independent to full
    /// and throttle to idle. Never auto-releases on start. Handbrakes untouched.
    /// Fail closed via Three-Gate. Cached write delegate so the apply tick does not alloc.
    /// </summary>
    public sealed class AutoBrakeGovernorListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private AutoBrakeLogCache _log;
        private AutoBrakePhase _phase;
        private bool _wasEngineOn;
        private Func<bool>? _softWrite;
        private OverridableBaseControl? _pendingTrain;
        private OverridableBaseControl? _pendingInd;
        private OverridableBaseControl? _pendingThrottle;
        private float _desiredTrain;
        private float _desiredInd;
        private float _desiredThrottle;
        private bool _writeTrain;
        private bool _writeInd;
        private bool _writeThrottle;

        private void OnEnable()
        {
            _log = default;
            _phase = AutoBrakePhase.Idle;
            _wasEngineOn = false;
            _softWrite = ApplyPending;
            ClearPending();
        }

        private void OnDisable()
        {
            _log = default;
            _phase = AutoBrakePhase.Idle;
            _wasEngineOn = false;
            _softWrite = null;
            ClearPending();
        }

        private void FixedUpdate()
        {
            try
            {
                Tick();
            }
            catch
            {
                _phase = AutoBrakePhase.Idle;
                Emit(false, sessionNeedsWork: true, ThreeGateAbortReason.SoftWrite);
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

            if (!worldActive || !hasLoco)
            {
                EndIfApplying(sessionNeedsWork: true, ThreeGateAbortReason.Integrity);
                _wasEngineOn = false;
                return;
            }

            if (!overlayClear && _phase == AutoBrakePhase.Idle)
            {
                return;
            }

            var controls = loco!.SimController?.controlsOverrider;
            var brake = controls?.Brake;
            var ind = controls?.IndependentBrake;
            var throttle = controls?.Throttle;
            var engineOn = controls?.EngineOnReader != null && controls.EngineOnReader.IsOn;
            var engineOff = !engineOn;
            var falling = AutoBrakePark.DetectEngineOffFallingEdge(_wasEngineOn, engineOn);
            _wasEngineOn = engineOn;

            var trainVal = brake != null ? brake.Value : AutoBrakePark.FullApply;
            var indVal = ind != null ? ind.Value : AutoBrakePark.FullApply;
            var throttleVal = throttle != null ? throttle.Value : 0f;
            var needsWork = AutoBrakePark.SessionNeedsWork(trainVal, indVal, throttleVal);
            var controlsPresent = brake != null || ind != null || throttle != null;
            var blocked =
                (brake != null && brake.IsControlBlocked)
                || (ind != null && ind.IsControlBlocked)
                || (throttle != null && throttle.IsControlBlocked);

            var startOk = AutoBrakePark.IsSafeToApply(
                hasUsableLoco: true,
                controlsPresent: controlsPresent,
                controlNotBlocked: !blocked,
                engineOff: engineOff,
                sessionNeedsWork: needsWork);
            var continueOk = controlsPresent && !blocked && engineOff;
            var phaseSafe = _phase == AutoBrakePhase.Applying ? continueOk : startOk;
            _phase = AutoBrakePark.NextPhase(_phase, falling, engineOff, phaseSafe, needsWork);

            if (_phase != AutoBrakePhase.Applying)
            {
                var abort = EndAbort(controlsPresent, needsWork);
                Emit(false, needsWork, abort);
                return;
            }

            if (!overlayClear)
            {
                Emit(true, needsWork, ThreeGateAbortReason.None);
                return;
            }

            var dt = Time.fixedDeltaTime;
            _pendingTrain = brake;
            _pendingInd = ind;
            _pendingThrottle = throttle;
            _desiredTrain = AutoBrakePark.ComputeDesiredBrake(trainVal, applying: true, dt);
            _desiredInd = AutoBrakePark.ComputeDesiredBrake(indVal, applying: true, dt);
            _desiredThrottle = AutoBrakePark.ComputeDesiredThrottle(throttleVal, applying: true, dt);
            _writeTrain = brake != null && AutoBrakePark.ShouldRaise(trainVal, _desiredTrain);
            _writeInd = ind != null && AutoBrakePark.ShouldRaise(indVal, _desiredInd);
            _writeThrottle = throttle != null && AutoBrakePark.ShouldLower(throttleVal, _desiredThrottle);

            if (!_writeTrain && !_writeInd && !_writeThrottle)
            {
                Emit(true, needsWork, ThreeGateAbortReason.None);
                return;
            }

            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, actorPresent: true),
                ThreeGateWrite.StateRegistry(controlsPresent),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: !blocked),
                _softWrite!);

            if (result.Applied)
            {
                Emit(true, needsWork, ThreeGateAbortReason.None);
                return;
            }

            _phase = AutoBrakePhase.Idle;
            Emit(false, needsWork, result.AbortReason);
        }

        private bool ApplyPending()
        {
            if (_writeTrain)
            {
                _pendingTrain!.Set(_desiredTrain);
            }

            if (_writeInd)
            {
                _pendingInd!.Set(_desiredInd);
            }

            if (_writeThrottle)
            {
                _pendingThrottle!.Set(_desiredThrottle);
            }

            return true;
        }

        private void EndIfApplying(bool sessionNeedsWork, ThreeGateAbortReason abort)
        {
            if (_phase != AutoBrakePhase.Applying)
            {
                return;
            }

            _phase = AutoBrakePhase.Idle;
            Emit(false, sessionNeedsWork, abort);
        }

        private static ThreeGateAbortReason EndAbort(bool controlsPresent, bool needsWork)
        {
            if (!needsWork)
            {
                return ThreeGateAbortReason.None;
            }

            return controlsPresent ? ThreeGateAbortReason.Safety : ThreeGateAbortReason.StateRegistry;
        }

        private void Emit(bool applying, bool sessionNeedsWork, ThreeGateAbortReason abort)
        {
            var line = AutoBrakeTelemetry.NextLog(applying, sessionNeedsWork, abort, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void ClearPending()
        {
            _pendingTrain = null;
            _pendingInd = null;
            _pendingThrottle = null;
            _writeTrain = false;
            _writeInd = false;
            _writeThrottle = false;
        }
    }
}
