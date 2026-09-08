using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Auto 180° turntable rotate after consist-mid kiss. Uses native
    /// <c>TurntableRailTrack.RotateToTargetRotation</c> (v1 QOL path).
    /// </summary>
    internal static class TurntableSpinGovernor
    {
        internal static System.Action<string>? EmitLog;

        private static float _targetYaw;
        private static bool _hasTarget;

        internal static void Reset()
        {
            _hasTarget = false;
            _targetYaw = 0f;
        }

        internal static bool Tick(float dt, Vector3 locoWorld)
        {
            if (!TryFindClosest(locoWorld, out var ctrl) || ctrl == null || ctrl.turntable == null)
            {
                return TurntableSpinSession.Locked;
            }

            var track = ctrl.turntable;
            try
            {
                ctrl.PlayerControlAllowed = true;
            }
            catch
            {
                // keep going
            }

            if (!TurntableSpinSession.Active)
            {
                TurntableSpinSession.Begin();
                _targetYaw = TurntableSpinPolicy.OppositeYaw(track.currentYRotation);
                _hasTarget = true;
                EmitLog?.Invoke(SwitchListRunnerTelemetry.YardChainTtSpin);
            }

            if (!_hasTarget)
            {
                _targetYaw = TurntableSpinPolicy.OppositeYaw(track.currentYRotation);
                _hasTarget = true;
            }

            var delta = TurntableSpinPolicy.DeltaToTarget(track.currentYRotation, _targetYaw);
            var abs = delta < 0f ? -delta : delta;
            if (TurntableSpinPolicy.IsLocked(abs))
            {
                track.targetYRotation = TurntableSpinPolicy.AngleRange0To360(_targetYaw);
                track.RotateToTargetRotation(true);
                TurntableSpinSession.ObserveLocked(true);
                return true;
            }

            var step = System.Math.Sign(delta)
                * TurntableSpinPolicy.MaxDegreesPerSecond
                * (dt > 0f ? (dt > 0.05f ? 0.05f : dt) : 0.02f);
            if (step > abs)
            {
                step = delta;
            }
            else if (step < -abs)
            {
                step = delta;
            }

            var next = TurntableSpinPolicy.AngleRange0To360(track.currentYRotation + step);
            track.targetYRotation = next;
            track.RotateToTargetRotation(false);
            TurntableSpinSession.ObserveLocked(false);
            return false;
        }

        private static bool TryFindClosest(Vector3 locoWorld, out TurntableController? ctrl)
        {
            ctrl = null;
            try
            {
                ctrl = TurntableController.FindClosestTo(locoWorld);
                return ctrl != null;
            }
            catch
            {
                ctrl = null;
                return false;
            }
        }
    }
}
