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
    }
}
