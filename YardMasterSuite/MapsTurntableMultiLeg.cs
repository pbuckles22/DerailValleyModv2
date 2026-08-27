using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// When Town TT Set dest yields NoPath, bind pivot→TT Switch List (**8.5**).
    /// Armed only for synthetic Turntable token resolves — not every anonymous track.
    /// </summary>
    internal static class MapsTurntableMultiLeg
    {
        private static bool _armed;
        private static string? _yardId;
        private static string? _ttTrackId;

        public static void Arm(string yardId, string ttTrackId)
        {
            _armed = true;
            _yardId = yardId?.Trim();
            _ttTrackId = ttTrackId?.Trim();
        }

        public static void Disarm()
        {
            _armed = false;
            _yardId = null;
            _ttTrackId = null;
        }

        public static bool IsArmed =>
            _armed && !string.IsNullOrEmpty(_yardId) && !string.IsNullOrEmpty(_ttTrackId);

        /// <summary>
        /// On NoPath after armed TT Set dest: pick pivot, bind BuildTownTurntable, retarget dest to first step.
        /// Returns status line for desk / log, or null if not applicable.
        /// </summary>
        public static string? TryBindOnNoPath(PathGraphMapper? graph, string? noPathLine)
        {
            if (!IsArmed || graph == null || !graph.HasFrozenPathCheck)
            {
                return null;
            }

            var yard = _yardId!;
            var tt = _ttTrackId!;
            Disarm();

            var origin = RouteOriginProbe.TryGet();
            if (origin == null)
            {
                return noPathLine;
            }

            var pivot = RoutePivotFinder.TryFindFirstPivotTrackId(graph, origin, tt, yard);
            if (string.IsNullOrWhiteSpace(pivot))
            {
                MapsDeskPanel.EmitLog?.Invoke("T2 path: TT multi-step no pivot from " + origin + " → " + tt);
                return noPathLine;
            }

            RouteDestSession.Set(yard, pivot);
            var pivotRev = false;
            if (MapsRouteListener.Instance != null
                && MapsRouteListener.Instance.TryComputeSyncPublic("tt-probe", out var probePlan, out _))
            {
                pivotRev = RouteFacingResolver.IsTargetBehind(probePlan, graph);
            }

            var steps = SwitchListPlanner.BuildTownTurntable(
                yard,
                tt,
                pivot,
                pivotNeedsReverse: pivotRev,
                turntableNeedsReverse: false,
                insertFacingBeforeTurntable: false);
            if (steps == null || steps.Count == 0)
            {
                return "T2 path: could not build TT Switch List";
            }

            SwitchListSession.Bind("tt:" + yard, steps);
            MapsDeskPanel.EmitLog?.Invoke(
                "T2 switch-list: loaded tt:"
                + yard
                + " · "
                + steps.Count
                + " steps · TT "
                + tt
                + " via pivot "
                + pivot);

            var step = SwitchListSession.CurrentStep;
            if (step == null || string.IsNullOrEmpty(step.DestTrackId))
            {
                return "T2 path: no active TT step";
            }

            RouteDestSession.Set(step.DestYardId, step.DestTrackId);
            if (MapsRouteListener.Instance != null)
            {
                MapsRouteListener.Instance.TryComputeSyncPublic("tt-list", out _, out var line);
                var suffix = " · Switch List " + step.Index + "/" + steps.Count + " " + step.Label;
                return (line ?? "T2 route: tt-list") + suffix;
            }

            return "T2 route: Switch List " + step.Index + "/" + steps.Count + " " + step.Label;
        }
    }

    /// <summary>Nearby-track pivot pick when origin→final is NoPath (**8.5**).</summary>
    internal static class RoutePivotFinder
    {
        internal static string? TryFindFirstPivotTrackId(
            PathGraphMapper graph,
            string origin,
            string finalTrackId,
            string? sessionYardId)
        {
            if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(finalTrackId))
            {
                return null;
            }

            if (!graph.TryGetRailTrack(origin, out var originRail) || originRail == null)
            {
                return null;
            }

            Vector3 op;
            try
            {
                op = originRail.transform.position;
            }
            catch
            {
                return null;
            }

            float fx = op.x, fz = op.z;
            var haveFinal = false;
            if (graph.TryGetRailTrack(finalTrackId, out var finalRail) && finalRail != null)
            {
                try
                {
                    var fp = finalRail.transform.position;
                    fx = fp.x;
                    fz = fp.z;
                    haveFinal = true;
                }
                catch
                {
                    // keep origin as fallback
                }
            }

            var selected = new Dictionary<string, int>(64);
            graph.CopyJunctionSelected(selected);
            var destYard = PathRouteConstraints.EffectiveDestYardId(
                finalTrackId, sessionYardId, PathRouteConstraints.YardIdOf);
            var filtered = PathRouteConstraints.FilterEdges(
                graph.PathCheckEdges,
                graph.ClassFor,
                occupied: null,
                origin,
                finalTrackId,
                PathRouteConstraints.YardIdOf,
                destYard);
            var mode = PathPlanModeSelect.ForTrip(origin, finalTrackId, sessionYardId, PathRouteConstraints.YardIdOf);

            const float radius = 400f;
            const int maxTry = 12;
            var scored = new List<(string Id, float DistFinal, PathTrackClass Cls)>(48);
            foreach (var kv in graph.EnumerateRails())
            {
                var id = kv.Key;
                var rail = kv.Value;
                if (string.IsNullOrWhiteSpace(id)
                    || rail == null
                    || string.Equals(id, origin, System.StringComparison.Ordinal)
                    || string.Equals(id, finalTrackId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                var cls = graph.ClassFor(id);
                if (cls != PathTrackClass.Through && cls != PathTrackClass.YardService)
                {
                    continue;
                }

                var yard = PathRouteConstraints.YardIdOf(id);
                if (destYard != null
                    && yard != null
                    && !string.Equals(yard, destYard, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Vector3 p;
                try
                {
                    p = rail.transform.position;
                }
                catch
                {
                    continue;
                }

                var dx = p.x - op.x;
                var dz = p.z - op.z;
                if ((dx * dx) + (dz * dz) > radius * radius)
                {
                    continue;
                }

                var distFinal = haveFinal
                    ? Mathf.Sqrt(((p.x - fx) * (p.x - fx)) + ((p.z - fz) * (p.z - fz)))
                    : 9999f;
                scored.Add((id, distFinal, cls));
            }

            scored.Sort((a, b) =>
            {
                var thru = (a.Cls == PathTrackClass.Through ? 0 : 1)
                    .CompareTo(b.Cls == PathTrackClass.Through ? 0 : 1);
                return thru != 0 ? thru : a.DistFinal.CompareTo(b.DistFinal);
            });

            var candidates = new List<RoutePivotCandidate>(maxTry);
            var n = Mathf.Min(maxTry, scored.Count);
            for (var i = 0; i < n; i++)
            {
                var id = scored[i].Id;
                var toPivot = PathPlan.Find(
                    filtered, selected, origin, id, graph.ClassFor,
                    destYardId: destYard, yardFor: PathRouteConstraints.YardIdOf, mode: mode);
                var fromPivot = PathPlan.Find(
                    filtered, selected, id, finalTrackId, graph.ClassFor,
                    destYardId: destYard, yardFor: PathRouteConstraints.YardIdOf, mode: mode);
                candidates.Add(new RoutePivotCandidate(
                    id,
                    canReachFromOrigin: IsUsable(toPivot),
                    canReachFinal: IsUsable(fromPivot),
                    originToPivotCost: toPivot.TotalCost,
                    metersToFinal: scored[i].DistFinal));
            }

            return RouteFirstPivot.Pick(origin, finalTrackId, candidates);
        }

        private static bool IsUsable(PathPlanResult plan) =>
            plan.Status != PathCheckStatus.NoPath
            && plan.Status != PathCheckStatus.NoOrigin
            && plan.Status != PathCheckStatus.NoDestination;
    }
}
