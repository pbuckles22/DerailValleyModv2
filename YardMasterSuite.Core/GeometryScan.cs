using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>Current-segment geometry snapshot. Type A payload. SegmentId 0 = unboarded.</summary>
    public readonly struct GeometryScanResult
    {
        public readonly int SegmentId;
        public readonly bool HasLimit;
        public readonly float LimitKmh;
        public readonly float StartSpanMeters;
        public readonly float EndSpanMeters;

        public GeometryScanResult(
            int segmentId,
            bool hasLimit,
            float limitKmh,
            float startSpanMeters,
            float endSpanMeters)
        {
            SegmentId = segmentId;
            HasLimit = hasLimit;
            LimitKmh = limitKmh;
            StartSpanMeters = startSpanMeters;
            EndSpanMeters = endSpanMeters;
        }

        public static GeometryScanResult None => default;
    }

    /// <summary>Last published segment. Rescan only when this changes.</summary>
    public struct GeometryScanCache
    {
        public int SegmentId;
        public bool Seeded;
    }

    /// <summary>
    /// Bezier-once store keyed by RailTrack instance id. Re-entering a track
    /// still <see cref="GeometryScan.ShouldRescan"/>s (segment changed) but
    /// skips <c>CalculateArcs</c> when the store hits.
    /// </summary>
    public sealed class GeometrySegmentStore
    {
        private readonly Dictionary<int, GeometryScanResult> _byId = new Dictionary<int, GeometryScanResult>();

        public bool TryGet(int segmentId, out GeometryScanResult result)
        {
            if (segmentId == 0)
            {
                result = default;
                return false;
            }

            return _byId.TryGetValue(segmentId, out result);
        }

        public void Remember(in GeometryScanResult result)
        {
            if (result.SegmentId == 0)
            {
                return;
            }

            _byId[result.SegmentId] = result;
        }

        public void Clear()
        {
            _byId.Clear();
        }
    }

    /// <summary>
    /// Unity-free cache-until-segment-change gate. Sample bezier only when
    /// <see cref="ShouldRescan"/> is true and the store misses.
    /// </summary>
    public static class GeometryScan
    {
        public static bool ShouldRescan(int segmentId, in GeometryScanCache cache)
        {
            if (!cache.Seeded)
            {
                return segmentId != 0;
            }

            return cache.SegmentId != segmentId;
        }

        public static GeometryScanResult Evaluate(
            int segmentId,
            IReadOnlyList<SpeedLimitGeometryZones.ArcSample> arcs)
        {
            if (segmentId == 0)
            {
                return GeometryScanResult.None;
            }

            if (SpeedLimitGeometryZones.TryGoverningZone(arcs, out var limit, out var start, out var end))
            {
                return new GeometryScanResult(segmentId, true, limit, start, end);
            }

            return new GeometryScanResult(segmentId, false, 0f, 0f, 0f);
        }

        public static void Remember(in GeometryScanResult result, ref GeometryScanCache cache)
        {
            cache.Seeded = true;
            cache.SegmentId = result.SegmentId;
        }
    }

    /// <summary>T2 lines for geometry scan. Silent when there is nothing to say.</summary>
    public static class GeometryTelemetry
    {
        public static string Format(in GeometryScanResult result)
        {
            if (result.SegmentId == 0)
            {
                return "T2 geometry: segment=—";
            }

            if (!result.HasLimit)
            {
                return "T2 geometry: segment=" + result.SegmentId.ToString() + " limit=—";
            }

            return "T2 geometry: segment=" + result.SegmentId.ToString()
                + " limit=" + ((int)result.LimitKmh).ToString()
                + " start=" + ((int)result.StartSpanMeters).ToString()
                + " end=" + ((int)result.EndSpanMeters).ToString();
        }
    }
}
