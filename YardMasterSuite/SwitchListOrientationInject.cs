using System.Collections.Generic;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Face-into-Exit TurnAround + reverse-into inject for Switch List Load (**8.5**).
    /// </summary>
    internal static class SwitchListOrientationInject
    {
        public static void Apply(JobSummary summary, PathGraphMapper? graph)
        {
            if (summary == null || graph == null || !graph.HasFrozenPathCheck)
            {
                return;
            }

            var origin = summary.OriginTrackId?.Trim();
            var dest = summary.DestTrackId?.Trim();
            if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(dest))
            {
                return;
            }

            TryInjectTurnAround(summary, graph, origin!, dest!);
            TryInjectReverseInto(summary, graph, origin!, dest!);
        }

        private static void TryInjectTurnAround(
            JobSummary summary,
            PathGraphMapper graph,
            string origin,
            string dest)
        {
            if (!TryGetLocoForward(out var fwdX, out var fwdZ))
            {
                return;
            }

            if (!TryGetExitHopDelta(graph, origin, dest, out var exitDx, out var exitDz))
            {
                return;
            }

            if (!SwitchListTurnAround.NeedsTurntableBeforePrep(fwdX, fwdZ, exitDx, exitDz))
            {
                MapsDeskPanel.EmitLog?.Invoke(
                    "T2 switch-list: facing opposite Exit — Prep-ready (no TurnAround)");
                return;
            }

            if (!TryGetPlayerXZ(out var ox, out var oz))
            {
                return;
            }

            var candidates = TurntableLocator.PeekCandidates(ox, oz);
            if (candidates == null || candidates.Count == 0)
            {
                MapsDeskPanel.EmitLog?.Invoke("T2 switch-list: TurnAround needed but no TT candidates");
                return;
            }

            var selected = new Dictionary<string, int>(64);
            graph.CopyJunctionSelected(selected);
            var destYard = PathRouteConstraints.EffectiveDestYardId(
                dest, summary.DestYardId, PathRouteConstraints.YardIdOf);
            var filtered = PathRouteConstraints.FilterEdges(
                graph.PathCheckEdges,
                graph.ClassFor,
                occupied: null,
                origin,
                dest,
                PathRouteConstraints.YardIdOf,
                destYard);

            bool BothLegs(string tableId)
            {
                var modeOt = PathPlanModeSelect.ForTrip(origin, tableId, summary.OriginYardId, PathRouteConstraints.YardIdOf);
                var toTable = PathPlan.Find(
                    filtered, selected, origin, tableId, graph.ClassFor,
                    destYardId: summary.OriginYardId, yardFor: PathRouteConstraints.YardIdOf, mode: modeOt);
                if (toTable.Status == PathCheckStatus.NoPath
                    || toTable.Status == PathCheckStatus.NoOrigin
                    || toTable.Status == PathCheckStatus.NoDestination)
                {
                    return false;
                }

                var modeTd = PathPlanModeSelect.ForTrip(tableId, dest, summary.DestYardId, PathRouteConstraints.YardIdOf);
                var fromTable = PathPlan.Find(
                    filtered, selected, tableId, dest, graph.ClassFor,
                    destYardId: destYard, yardFor: PathRouteConstraints.YardIdOf, mode: modeTd);
                return fromTable.Status != PathCheckStatus.NoPath
                    && fromTable.Status != PathCheckStatus.NoOrigin
                    && fromTable.Status != PathCheckStatus.NoDestination;
            }

            var tt = SwitchListTurnAround.ResolveTurntable(
                summary.OriginYardId,
                summary.DestYardId,
                candidates,
                BothLegs);
            if (tt == null)
            {
                MapsDeskPanel.EmitLog?.Invoke("T2 switch-list: TurnAround needed but no pathable TT");
                return;
            }

            summary.NeedsTurnAround = true;
            summary.TurntableTrackId = tt;
            MapsDeskPanel.EmitLog?.Invoke(
                "T2 switch-list: inject TurnAround → " + tt + " (face into Exit)");
        }

        private static void TryInjectReverseInto(
            JobSummary summary,
            PathGraphMapper graph,
            string origin,
            string dest)
        {
            if (!string.IsNullOrEmpty(summary.ReverseIntoTrackId) && summary.NeedsReverseInto)
            {
                return;
            }

            // Job penultimate dest (intermediate spur) wins when present.
            if (!string.IsNullOrEmpty(summary.ReverseIntoTrackId))
            {
                summary.NeedsReverseInto = true;
                return;
            }

            var selected = new Dictionary<string, int>(64);
            graph.CopyJunctionSelected(selected);
            var destYard = PathRouteConstraints.EffectiveDestYardId(
                dest, summary.DestYardId, PathRouteConstraints.YardIdOf);
            var filtered = PathRouteConstraints.FilterEdges(
                graph.PathCheckEdges,
                graph.ClassFor,
                occupied: null,
                origin,
                dest,
                PathRouteConstraints.YardIdOf,
                destYard);
            var mode = PathPlanModeSelect.ForTrip(origin, dest, summary.DestYardId, PathRouteConstraints.YardIdOf);
            var plan = PathPlan.Find(
                filtered, selected, origin, dest, graph.ClassFor,
                destYardId: destYard, yardFor: PathRouteConstraints.YardIdOf, mode: mode);
            if (plan.Status == PathCheckStatus.NoPath
                || plan.Status == PathCheckStatus.NoOrigin
                || plan.Status == PathCheckStatus.NoDestination)
            {
                return;
            }

            if (!SwitchListTurnAround.NeedsReverseInto(plan.LastHopRequiresReverse))
            {
                return;
            }

            summary.NeedsReverseInto = true;
            summary.ReverseIntoTrackId = dest;
            MapsDeskPanel.EmitLog?.Invoke("T2 switch-list: inject ReverseInto → " + dest + " (last hop reverse)");
        }

        private static bool TryGetExitHopDelta(
            PathGraphMapper graph,
            string origin,
            string dest,
            out float exitDx,
            out float exitDz)
        {
            exitDx = exitDz = 0f;
            var selected = new Dictionary<string, int>(64);
            graph.CopyJunctionSelected(selected);
            var destYard = PathRouteConstraints.EffectiveDestYardId(dest, null, PathRouteConstraints.YardIdOf);
            var filtered = PathRouteConstraints.FilterEdges(
                graph.PathCheckEdges,
                graph.ClassFor,
                occupied: null,
                origin,
                dest,
                PathRouteConstraints.YardIdOf,
                destYard);
            var mode = PathPlanModeSelect.ForTrip(origin, dest, null, PathRouteConstraints.YardIdOf);
            var plan = PathPlan.Find(
                filtered, selected, origin, dest, graph.ClassFor,
                destYardId: destYard, yardFor: PathRouteConstraints.YardIdOf, mode: mode);
            if (plan.TrackIds.Count < 2)
            {
                // No path — try loco→dest crow-flies as Exit (still better than nothing).
                if (!TryGetLocoPos(out var lx, out var lz)
                    || !graph.TryGetRailTrack(dest, out var destRail)
                    || destRail == null)
                {
                    return false;
                }

                try
                {
                    var p = destRail.transform.position;
                    exitDx = p.x - lx;
                    exitDz = p.z - lz;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (!graph.TryGetRailTrack(plan.TrackIds[0], out var a) || a == null
                || !graph.TryGetRailTrack(plan.TrackIds[1], out var b) || b == null)
            {
                return false;
            }

            try
            {
                var pa = a.transform.position;
                var pb = b.transform.position;
                exitDx = pb.x - pa.x;
                exitDz = pb.z - pa.z;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetLocoForward(out float fwdX, out float fwdZ)
        {
            fwdX = fwdZ = 0f;
            try
            {
                var loco = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (loco == null)
                {
                    return false;
                }

                var f = loco.transform.forward;
                fwdX = f.x;
                fwdZ = f.z;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetLocoPos(out float x, out float z)
        {
            x = z = 0f;
            try
            {
                var loco = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (loco == null)
                {
                    return false;
                }

                var p = loco.transform.position;
                x = p.x;
                z = p.z;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetPlayerXZ(out float x, out float z)
        {
            x = z = 0f;
            try
            {
                var t = PlayerManager.PlayerTransform;
                if (t == null)
                {
                    return TryGetLocoPos(out x, out z);
                }

                var p = t.position;
                x = p.x;
                z = p.z;
                return true;
            }
            catch
            {
                return TryGetLocoPos(out x, out z);
            }
        }
    }
}
