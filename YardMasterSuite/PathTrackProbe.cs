using System;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>RailTrack → path-plan class / length for through-lane edge costs (**8.2**).</summary>
    internal static class PathTrackProbe
    {
        internal static float LengthMeters(RailTrack? track)
        {
            if (track == null)
            {
                return PathTrackCosts.MinLengthMeters;
            }

            try
            {
                var curve = track.curve;
                if (curve != null && curve.pointCount >= 2)
                {
                    var len = curve.length;
                    if (len > 0f)
                    {
                        return len;
                    }
                }
            }
            catch
            {
                // fall through
            }

            return PathTrackCosts.MinLengthMeters;
        }

        internal static PathTrackClass Classify(RailTrack? track)
        {
            if (track == null)
            {
                return PathTrackClass.Unknown;
            }

            try
            {
                var logic = RailTrackRegistry.RailTrackToLogicTrack != null
                    && RailTrackRegistry.RailTrackToLogicTrack.TryGetValue(track, out var lt)
                    && lt != null
                    ? lt
                    : null;
                var id = logic?.ID;
                if (id == null)
                {
                    return PathTrackClass.Unknown;
                }

                var typeToken = TryReadTrackType(id) ?? id.FullID ?? id.FullDisplayID;
                return PathTrackCosts.Classify(typeToken);
            }
            catch
            {
                return PathTrackClass.Unknown;
            }
        }

        internal static float EnterCostSeconds(RailTrack? track)
        {
            var cls = Classify(track);
            return PathTrackCosts.TravelSeconds(LengthMeters(track), geometryLimitKmh: null, cls);
        }

        internal static void RegisterTrack(
            RailTrack track,
            string? key,
            Dictionary<string, float> enterCost,
            Dictionary<string, PathTrackClass> classByKey,
            Dictionary<string, RailTrack> railsByKey)
        {
            if (track == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            railsByKey[key] = track;
            var cls = Classify(track);
            classByKey[key] = cls;
            enterCost[key] = PathTrackCosts.TravelSeconds(
                LengthMeters(track),
                geometryLimitKmh: null,
                cls);
        }

        private static string? TryReadTrackType(TrackID id)
        {
            try
            {
                var field = typeof(TrackID).GetField(
                    "trackType",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field?.GetValue(id) is { } raw)
                {
                    return raw.ToString();
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }
    }
}
