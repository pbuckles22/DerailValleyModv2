using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>Cab→dest facing and Exit cue for Maps route HUD (**8.2**).</summary>
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
            x = y = z = 0f;

            // Active Switch List leg: pin to junction-first / first flip (current-leg AR target).
            var pinId = SwitchListRouteLeg.PickPinJunctionId(plan);
            if (!string.IsNullOrEmpty(pinId)
                && graph != null
                && graph.TryGetJunction(pinId!, out var junction)
                && junction != null)
            {
                try
                {
                    var p = junction.transform.position;
                    x = p.x;
                    y = p.y;
                    z = p.z;
                    return true;
                }
                catch
                {
                    // fall through to dest track
                }
            }

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
