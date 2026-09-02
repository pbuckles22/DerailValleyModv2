using System;
using DV.Simulation.Controllers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// <b>7.5</b> consist safety — when Derail Risk ≥65 %, idle throttle and
    /// raise train + independent. Speed and posted Limit are HUD-only. Never
    /// dumps air. Hold until Derail is below 65 %. Fail closed via Three-Gate.
    /// </summary>
    public sealed class LimitGovernorListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private LimitGovLogCache _log;
        private LimitGovCueCache _cue;
        private Func<bool>? _softWrite;
        private OverridableBaseControl? _pendingThrottle;
        private OverridableBaseControl? _pendingInd;
        private OverridableBaseControl? _pendingTrain;
        private float _desiredThrottle;
        private float _desiredInd;
        private float _desiredTrain;
        private bool _writeThrottle;
        private bool _writeInd;
        private bool _writeTrain;

        private void OnEnable()
        {
            _log = default;
            _cue = default;
            _softWrite = ApplyPending;
            ClearPending();
        }

        private void OnDisable()
        {
            PublishCue(LimitGovCue.None);
            _log = default;
            _cue = default;
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
                Emit(false, ThreeGateAbortReason.SoftWrite);
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
            if (hasLoco)
            {
                derailPercent = DerailRiskReader.ReadConsist(loco).MaxPercent;
            }

            var intervening = LimitThrottleCap.ShouldIntervene(derailPercent);
            var rate = LimitThrottleCap.ApplyPerSecond(derailPercent);
            var indyTarget = intervening
                ? LimitThrottleCap.IndependentTarget(derailPercent)
                : 0f;
            var trainTarget = intervening
                ? LimitThrottleCap.TrainTarget(derailPercent)
                : 0f;

            var controls = hasLoco ? loco!.SimController?.controlsOverrider : null;
            var throttle = controls?.Throttle;
            var ind = controls?.IndependentBrake;
            var train = controls?.Brake;
            var controlsPresent = throttle != null || ind != null || train != null;
            var blocked =
                (throttle != null && throttle.IsControlBlocked)
                || (ind != null && ind.IsControlBlocked)
                || (train != null && train.IsControlBlocked);

            var throttleVal = throttle != null ? throttle.Value : 0f;
            var indVal = ind != null ? ind.Value : 0f;
            var trainVal = train != null ? train.Value : 0f;
            var dt = Time.fixedDeltaTime;
            _desiredThrottle = LimitThrottleCap.ComputeDesiredThrottle(throttleVal, intervening, dt, rate);
            _desiredInd = LimitThrottleCap.ComputeDesiredBrake(indVal, indyTarget, intervening, dt, rate);
            _desiredTrain = LimitThrottleCap.ComputeDesiredBrake(trainVal, trainTarget, intervening, dt, rate);
            var needsWork = LimitThrottleCap.NeedsWork(
                throttleVal, indVal, trainVal, indyTarget, trainTarget);
            PublishCue(LimitThrottleCap.CueForLevers(
                intervening, throttleVal, indVal, trainVal, indyTarget, trainTarget));

            if (!intervening)
            {
                Emit(false, ThreeGateAbortReason.Safety);
                return;
            }

            if (LimitThrottleCap.ShouldHold(intervening, needsWork))
            {
                Emit(true, ThreeGateAbortReason.None);
                return;
            }

            var safe = LimitThrottleCap.IsSafeToWrite(
                hasUsableLoco: hasLoco,
                controlsPresent: controlsPresent,
                controlNotBlocked: !blocked,
                intervening: intervening,
                needsWork: needsWork);
            if (!safe)
            {
                Emit(false, ThreeGateAbortReason.Safety);
                return;
            }

            if (!overlayClear)
            {
                Emit(true, ThreeGateAbortReason.None);
                return;
            }

            _pendingThrottle = throttle;
            _pendingInd = ind;
            _pendingTrain = train;
            _writeThrottle = throttle != null && LimitThrottleCap.ShouldLower(throttleVal, _desiredThrottle);
            _writeInd = ind != null && LimitThrottleCap.ShouldRaise(indVal, _desiredInd);
            _writeTrain = train != null && LimitThrottleCap.ShouldRaise(trainVal, _desiredTrain);
            if (!_writeThrottle && !_writeInd && !_writeTrain)
            {
                Emit(true, ThreeGateAbortReason.None);
                return;
            }

            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, hasLoco),
                ThreeGateWrite.StateRegistry(controlsPresent),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: !blocked),
                _softWrite!);
            if (result.Applied && _writeThrottle)
            {
                ThrottleWriterNote.Note(ThrottleWriterKind.DerailGov, Time.frameCount);
            }

            Emit(result.Applied, result.AbortReason);
        }

        private bool ApplyPending()
        {
            if (_writeThrottle)
            {
                _pendingThrottle!.Set(_desiredThrottle);
            }

            if (_writeInd)
            {
                _pendingInd!.Set(_desiredInd);
            }

            if (_writeTrain)
            {
                _pendingTrain!.Set(_desiredTrain);
            }

            return true;
        }

        private void Emit(bool applied, ThreeGateAbortReason abort)
        {
            var line = LimitGovTelemetry.NextLog(
                applied, abort, limitRounded: 0, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void PublishCue(in LimitGovCue cue)
        {
            if (!LimitGovCueTelemetry.Observe(cue, ref _cue))
            {
                return;
            }

            YmsEventBus.RaiseLimitGovCue(cue);
        }

        private void ClearPending()
        {
            _pendingThrottle = null;
            _pendingInd = null;
            _pendingTrain = null;
            _writeThrottle = false;
            _writeInd = false;
            _writeTrain = false;
        }
    }
}
