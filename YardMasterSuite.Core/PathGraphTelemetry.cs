namespace YardMasterSuite.Core
{
    /// <summary>T2 lines for graph mapping. Silent when there is nothing to say.</summary>
    public static class PathGraphTelemetry
    {
        public static string FormatStart(int units)
        {
            if (units <= 0)
            {
                return "T2 graph fail";
            }

            return "T2 graph start: units=" + units.ToString();
        }

        public static string FormatReady(in PathGraphReady ready)
        {
            var hops = ready.PathFound ? ready.PathHops.ToString() : "—";
            return "T2 graph ready: nodes=" + ready.NodeCount.ToString()
                + " edges=" + ready.EdgeCount.ToString()
                + " hops=" + hops;
        }

        public static string FormatFail()
        {
            return "T2 graph fail";
        }

        /// <summary>
        /// Spatial graph loaded for A* routing. Logs junction count and sample coordinates.
        /// </summary>
        public static string FormatSpatialGraph(SpatialGraph spatial, string? sampleJunctionId = null)
        {
            if (!spatial.HasCoordinates)
            {
                return "T2 spatial-graph: none";
            }

            var msg = "T2 spatial-graph: junctions=" + spatial.JunctionCount.ToString();
            if (!string.IsNullOrEmpty(sampleJunctionId)
                && spatial.TryGetJunctionXz(sampleJunctionId, out var x, out var z))
            {
                msg += " sample=[" + sampleJunctionId + "] X=" + x.ToString("F1") + " Z=" + z.ToString("F1");
            }

            return msg;
        }

        /// <summary>Single junction coordinate for detailed logging.</summary>
        public static string FormatSpatialNode(string junctionId, float x, float z)
        {
            return "T2 spatial-graph: node [" + junctionId + "] loaded at X=" + x.ToString("F1") + " Z=" + z.ToString("F1");
        }

        /// <summary>
        /// One line per path choice. <paramref name="preferred"/> is false when no path was kept.
        /// </summary>
        public static string FormatSpatialChoice(
            string? fromTrack,
            string? destTrack,
            float costSeconds,
            float spatialPenaltySeconds,
            bool preferred)
        {
            var from = string.IsNullOrWhiteSpace(fromTrack) ? "—" : fromTrack!.Trim();
            var dest = string.IsNullOrWhiteSpace(destTrack) ? "—" : destTrack!.Trim();
            var verdict = preferred ? "PREFERRED" : "REJECTED";
            return "T2 path-eval: " + from + "→" + dest
                + " | cost=" + costSeconds.ToString("0")
                + " spatial_penalty=" + spatialPenaltySeconds.ToString("0")
                + " | " + verdict;
        }

        /// <summary>
        /// Path evaluation result for pin selection diagnostics.
        /// </summary>
        public static string FormatPathEval(
            string? fromTrack,
            string? destTrack,
            PathPlanResult? plan,
            string? selectedPin)
        {
            var from = fromTrack?.Trim() ?? "—";
            var dest = destTrack?.Trim() ?? "—";
            var pin = selectedPin?.Trim() ?? "—";

            if (plan == null || plan.Status == PathCheckStatus.NoPath)
            {
                return "T2 path-eval: " + from + "→" + dest + " NoPath pin=" + pin;
            }

            var tracks = plan.TrackIds?.Count ?? 0;
            var junctions = plan.Junctions?.Count ?? 0;

            var junctionList = "";
            if (plan.Junctions != null && plan.Junctions.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (var i = 0; i < plan.Junctions.Count && i < 5; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(",");
                    }

                    sb.Append(plan.Junctions[i].JunctionId ?? "?");
                }

                if (plan.Junctions.Count > 5)
                {
                    sb.Append("...");
                }

                junctionList = " via=[" + sb + "]";
            }

            return "T2 path-eval: " + from + "→" + dest
                + " hops=" + tracks
                + " jnct=" + junctions
                + junctionList
                + " pin=" + pin;
        }
    }
}
