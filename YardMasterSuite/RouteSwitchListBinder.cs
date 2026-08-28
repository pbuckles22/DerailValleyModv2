using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// After Set dest pathfind, bind Route-tab Switch List when sawtooth + reverse-into (**8.7**).
    /// Does not clobber Per job lists or mid-route advances (armed once per Set dest).
    /// </summary>
    internal static class RouteSwitchListBinder
    {
        private static int _armGeneration;

        public static void ArmForNextPlan() =>
            _armGeneration++;

        public static void Disarm() =>
            _armGeneration = 0;

        public static bool IsRouteBound =>
            SwitchListSession.JobId != null
            && SwitchListSession.JobId.StartsWith("route:", System.StringComparison.Ordinal);

        /// <summary>
        /// Returns true when a new route list was bound and step-1 dest should be applied.
        /// </summary>
        public static bool TryBindIfArmed(
            PathPlanResult plan,
            string? yardId,
            string? destTrackId,
            bool pinNeedsReverse,
            bool destNeedsReverse,
            out string? logLine)
        {
            logLine = null;
            if (_armGeneration <= 0)
            {
                return false;
            }

            _armGeneration = 0;

            if (SwitchListSession.HasActive && !IsRouteBound)
            {
                return false;
            }

            var steps = SwitchListPlanner.BuildFromRoute(
                yardId,
                destTrackId,
                plan,
                pinNeedsReverse,
                destNeedsReverse);
            if (steps == null || steps.Count == 0)
            {
                if (IsRouteBound)
                {
                    SwitchListSession.Clear();
                }

                return false;
            }

            var yard = string.IsNullOrWhiteSpace(yardId) ? "?" : yardId!.Trim();
            SwitchListSession.Bind("route:" + yard, steps);
            logLine = "T2 switch-list: route " + yard + " · " + steps.Count + " steps · sawtooth+reverse";
            return true;
        }
    }
}
