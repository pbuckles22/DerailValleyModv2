using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>Cab→dest facing and Exit cue for Maps route HUD (**8.2** / **8.7** coach).</summary>
    internal static class RouteFacingResolver
    {
        internal static bool IsTargetBehind(PathPlanResult? plan, PathGraphMapper? graph)
        {
            if (plan == null
                || !TryGetLoco(out var fwdX, out var fwdZ, out var posX, out var posZ)
                || !TryGetTargetPos(plan, graph, out var tx, out _, out var tz))
            {
                return false;
            }

            return DriveSetFacing.IsTargetBehind(fwdX, fwdZ, tx - posX, tz - posZ);
        }

        /// <summary>Facing toward the armed pin junction (switch-back Step 1).</summary>
        internal static bool IsPinBehind(PathPlanResult? plan, PathGraphMapper? graph)
        {
            if (plan == null
                || !TryGetLoco(out var fwdX, out var fwdZ, out var posX, out var posZ)
                || !TryGetPinPos(plan, graph, out var tx, out _, out var tz))
            {
                return false;
            }

            return DriveSetFacing.IsTargetBehind(fwdX, fwdZ, tx - posX, tz - posZ);
        }

        /// <summary>Facing toward final dest track (switch-back Step 2 / Path OK).</summary>
        internal static bool IsDestBehind(PathPlanResult? plan, PathGraphMapper? graph)
        {
            if (plan == null
                || !TryGetLoco(out var fwdX, out var fwdZ, out var posX, out var posZ)
                || !TryGetDestPos(plan, graph, out var tx, out _, out var tz))
            {
                return false;
            }

            return DriveSetFacing.IsTargetBehind(fwdX, fwdZ, tx - posX, tz - posZ);
        }

        internal static string? TryGetExitCue(PathPlanResult? plan, PathGraphMapper? graph)
        {
            if (plan == null
                || !TryGetLoco(out _, out _, out var posX, out var posZ)
                || !TryGetTargetPos(plan, graph, out var tx, out _, out var tz))
            {
                return null;
            }

            return RouteExitDisplay.Format(posX, posZ, tx, tz);
        }

        private static bool TryGetLoco(out float fwdX, out float fwdZ, out float posX, out float posZ)
        {
            fwdX = fwdZ = posX = posZ = 0f;
            try
            {
                var loco = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (loco == null)
                {
                    return false;
                }

                var t = loco.transform;
                var fwd = t.forward;
                var pos = t.position;
                fwdX = fwd.x;
                fwdZ = fwd.z;
                posX = pos.x;
                posZ = pos.z;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetTargetPos(
            PathPlanResult plan,
            PathGraphMapper? graph,
            out float x,
            out float y,
            out float z)
        {
            if (TryGetPinPos(plan, graph, out x, out y, out z))
            {
                return true;
            }

            return TryGetDestPos(plan, graph, out x, out y, out z);
        }

        private static bool TryGetPinPos(
            PathPlanResult plan,
            PathGraphMapper? graph,
            out float x,
            out float y,
            out float z)
        {
            x = y = z = 0f;
            var pinId = RoutePinLatch.EffectivePin(plan);
            if (string.IsNullOrEmpty(pinId)
                || graph == null
                || !graph.TryGetJunction(pinId!, out var junction)
                || junction == null)
            {
                return false;
            }

            return JunctionPinWorld.TryGet(junction, out x, out y, out z);
        }

        private static bool TryGetDestPos(
            PathPlanResult plan,
            PathGraphMapper? graph,
            out float x,
            out float y,
            out float z)
        {
            x = y = z = 0f;
            if (plan.TrackIds.Count == 0)
            {
                return false;
            }

            var destId = plan.TrackIds[plan.TrackIds.Count - 1];
            if (graph != null && graph.TryGetRailTrack(destId, out var rail) && rail != null)
            {
                try
                {
                    var p = rail.transform.position;
                    x = p.x;
                    y = p.y;
                    z = p.z;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
