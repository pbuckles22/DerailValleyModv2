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
    }
}
