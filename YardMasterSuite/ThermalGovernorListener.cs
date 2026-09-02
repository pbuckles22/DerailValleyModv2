using System;
using DV.Simulation.Controllers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// <b>7.2</b> Thermal governor — soft-roll usable-loco throttle when Motors
    /// are Hot (Warning 75% / Critical 55%). Fail closed via Three-Gate.
    /// Cached write delegate so the cap tick does not alloc.
    /// </summary>
    public sealed class ThermalGovernorListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private ThermalCapLogCache _log;
        private Func<bool>? _softWrite;
        private OverridableBaseControl? _pendingThrottle;
        private float _pendingDesired;

        private void OnEnable()
        {
            _log = default;
            _softWrite = ApplyPendingThrottle;
            _pendingThrottle = null;
            _pendingDesired = 0f;
        }

        private void OnDisable()
        {
            _log = default;
            _softWrite = null;
            _pendingThrottle = null;
        }

        private void FixedUpdate()
        {
            try
            {
                Tick();
            }
            catch
            {
                Emit(false, ThreeGateAbortReason.SoftWrite, ThermalTelemetry.KindHot);
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

            MotorStatus? motors = null;
            MotorCabTempBand? band = null;
            if (hasLoco)
            {
                LocoSimReader.ReadPower(loco!, out _, out _, out _, out motors);
                band = LocoSimReader.TryReadCabTempBand(loco!);
            }

            var motorsHot = motors == MotorStatus.Hot;
            var capKind = ThermalTelemetry.CapKind(band);
            var throttle = hasLoco ? loco!.SimController?.controlsOverrider?.Throttle : null;
            var controlsPresent = throttle != null;
            var blocked = throttle != null && throttle.IsControlBlocked;
            var current = throttle != null ? throttle.Value : 0f;
            var ceiling = ThermalThrottleCap.CeilingWhenHot(motorsHot, band);
            var desired = ThermalThrottleCap.ComputeDesiredThrottle(
                current,
                motorsHot,
                ceiling,
                Time.fixedDeltaTime);
            var aboveCap = ThermalThrottleCap.ShouldSoftWrite(current, desired);
            var safeToCap = ThermalThrottleCap.IsSafeToCap(
                hasUsableLoco: hasLoco,
                controlsPresent: controlsPresent,
                controlNotBlocked: !blocked,
                motorsHot: motorsHot,
                currentAboveCap: aboveCap);

            if (!safeToCap)
            {
                Emit(false, ThreeGateAbortReason.Safety, capKind);
                return;
            }

            _pendingThrottle = throttle;
            _pendingDesired = desired;
            var result = ThreeGate.TryApply(
                ThreeGateWrite.Integrity(worldActive, hasLoco),
                ThreeGateWrite.StateRegistry(controlsPresent),
                ThreeGateWrite.Safety(overlayClear, controlNotBlocked: !blocked),
                _softWrite!);
            if (result.Applied)
            {
                ThrottleWriterNote.Note(ThrottleWriterKind.Thermal, Time.frameCount);
            }

            Emit(result.Applied, result.AbortReason, capKind);
        }

        private bool ApplyPendingThrottle()
        {
            _pendingThrottle!.Set(_pendingDesired);
            return true;
        }

        private void Emit(bool applied, ThreeGateAbortReason abort, int capKind)
        {
            var line = ThermalTelemetry.NextLog(applied, abort, capKind, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }
    }
}
