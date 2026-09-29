using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// When the knuckle laser misses on a curve, the next car is still on a
    /// plan hop. Read bogies already on those rails. No scene search.
    /// </summary>
    internal static class RouteCarAheadProbe
    {
        internal static PathGraphMapper? Graph;

        private const float SearchMeters = 120f;

        private static readonly float[] HopLengths = new float[128];

        internal static bool TryMeters(TrainCar loco, bool useFront, out float meters)
        {
            meters = 0f;
            var plan = RoutePlanSession.Plan;
            var graph = Graph;
            if (plan?.TrackIds == null || graph == null)
            {
                return false;
            }

            var hopCount = plan.TrackIds.Count;
            if (hopCount <= 0 || hopCount > HopLengths.Length)
            {
                return false;
            }

            var leadBogie = useFront ? loco.FrontBogie : loco.RearBogie;
            if (!TryBogie(leadBogie, out var leadTrack, out var leadSpan, out var leadLen))
            {
                return false;
            }

            var leadHop = RouteTailAlongTrack.IndexOfHop(plan.TrackIds, leadTrack);
            if (leadHop < 0 || !TryHopLength(graph, plan.TrackIds[leadHop], out var leadHopLen))
            {
                return false;
            }

            HopLengths[leadHop] = leadLen > 0f ? leadLen : leadHopLen;

            var leadIncreasing = TravelIncreasesSpan(graph, plan, leadHop);
            var leadInto = TrackPathSpan.WithinTrackMeters(leadSpan, HopLengths[leadHop], leadIncreasing);
            var own = loco.trainset;
            var best = float.MaxValue;
            var found = false;
            if (useFront)
            {
                var walked = 0f;
                for (var hop = leadHop; hop < hopCount && walked <= SearchMeters; hop++)
                {
                    if (hop != leadHop)
                    {
                        if (!TryHopLength(graph, plan.TrackIds[hop], out var hopLen))
                        {
                            break;
                        }

                        HopLengths[hop] = hopLen;
                    }

                    ConsiderHop(graph, plan, hop, leadHop, leadInto, own, towardPlanEnd: true, ref best, ref found);
                    walked += hop == leadHop ? HopLengths[hop] - leadInto : HopLengths[hop];
                }
            }
            else
            {
                var walked = 0f;
                for (var hop = leadHop; hop >= 0 && walked <= SearchMeters; hop--)
                {
                    if (hop != leadHop)
                    {
                        if (!TryHopLength(graph, plan.TrackIds[hop], out var hopLen))
                        {
                            break;
                        }

                        HopLengths[hop] = hopLen;
                    }

                    ConsiderHop(graph, plan, hop, leadHop, leadInto, own, towardPlanEnd: false, ref best, ref found);
                    walked += hop == leadHop ? leadInto : HopLengths[hop];
                }
            }

            if (!found)
            {
                return false;
            }

            meters = best;
            return true;
        }

        private static bool TryHopLength(PathGraphMapper graph, string? trackId, out float length)
        {
            length = 0f;
            if (string.IsNullOrEmpty(trackId))
            {
                return false;
            }

            if (!graph.TryGetRailTrack(trackId!, out var rail) || rail == null)
            {
                return false;
            }

            length = PathTrackProbe.LengthMeters(rail);
            return length > 0f;
        }

        private static void ConsiderHop(
            PathGraphMapper graph,
            PathPlanResult plan,
            int hop,
            int leadHop,
            float leadInto,
            Trainset? own,
            bool towardPlanEnd,
            ref float best,
            ref bool found)
        {
            if (!graph.TryGetRailTrack(plan.TrackIds[hop], out var rail) || rail == null)
            {
                return;
            }

            var bogies = rail.BogiesOnTrack();
            if (bogies == null)
            {
                return;
            }

            var increasing = TravelIncreasesSpan(graph, plan, hop);

            foreach (var bogie in bogies)
            {
                if (bogie == null || !TryBogie(bogie, out _, out var span, out _))
                {
                    continue;
                }

                var car = bogie.Car;
                if (car == null || (own != null && ReferenceEquals(car.trainset, own)))
                {
                    continue;
                }

                var into = TrackPathSpan.WithinTrackMeters(span, HopLengths[hop], increasing);
                if (!RouteCarAlongTrack.TryMetersAhead(
                        HopLengths,
                        plan.TrackIds.Count,
                        leadHop,
                        leadInto,
                        hop,
                        into,
                        towardPlanEnd,
                        out var gap)
                    || gap >= best
                    || gap > SearchMeters)
                {
                    continue;
                }

                best = gap;
                found = true;
            }
        }

        private static bool TryBogie(Bogie? bogie, out string? trackId, out float span, out float length)
        {
            trackId = null;
            span = float.NaN;
            length = 0f;
            if (bogie == null || bogie.track == null || bogie.traveller == null)
            {
                return false;
            }

            trackId = LogicTrackKey.FromRail(bogie.track);
            if (string.IsNullOrEmpty(trackId))
            {
                return false;
            }

            span = (float)bogie.traveller.Span;
            length = PathTrackProbe.LengthMeters(bogie.track);
            return length > 0f;
        }

        private static bool TravelIncreasesSpan(PathGraphMapper graph, PathPlanResult plan, int hopIndex)
        {
            if (hopIndex < 0 || hopIndex >= plan.TrackIds.Count)
            {
                return true;
            }

            var hopId = plan.TrackIds[hopIndex];
            if (!graph.TryGetRailTrack(hopId, out var rail) || rail == null)
            {
                return true;
            }

            string? neighborId = null;
            if (hopIndex + 1 < plan.TrackIds.Count)
            {
                neighborId = plan.TrackIds[hopIndex + 1];
            }
            else if (hopIndex > 0)
            {
                neighborId = plan.TrackIds[hopIndex - 1];
            }

            if (string.IsNullOrEmpty(neighborId)
                || !graph.TryGetRailTrack(neighborId!, out var neighbor)
                || neighbor == null)
            {
                return true;
            }

            try
            {
                var inPos = rail.curve != null && rail.curve.pointCount > 0
                    ? rail.curve[0].position
                    : rail.transform.position;
                var outPos = rail.curve != null && rail.curve.pointCount > 1
                    ? rail.curve[rail.curve.pointCount - 1].position
                    : inPos;
                var nIn = neighbor.curve != null && neighbor.curve.pointCount > 0
                    ? neighbor.curve[0].position
                    : neighbor.transform.position;
                var nOut = neighbor.curve != null && neighbor.curve.pointCount > 1
                    ? neighbor.curve[neighbor.curve.pointCount - 1].position
                    : nIn;
                var toNextFromOut = MinDistSq(outPos, nIn, nOut);
                var toNextFromIn = MinDistSq(inPos, nIn, nOut);
                if (hopIndex + 1 < plan.TrackIds.Count)
                {
                    return toNextFromOut <= toNextFromIn;
                }

                return toNextFromIn <= toNextFromOut;
            }
            catch
            {
                return true;
            }
        }

        private static float MinDistSq(UnityEngine.Vector3 a, UnityEngine.Vector3 b, UnityEngine.Vector3 c)
        {
            var db = (a - b).sqrMagnitude;
            var dc = (a - c).sqrMagnitude;
            return db <= dc ? db : dc;
        }
    }
}
