namespace YardMasterSuite.Core
{
    /// <summary>Posted Limit snapshot (sticky km/h + last FoT roster size).</summary>
    public readonly struct PostedLimitSnapshot
    {
        public readonly float? Kmh;
        public readonly int RosterCount;

        public PostedLimitSnapshot(float? kmh, int rosterCount)
        {
            Kmh = kmh;
            RosterCount = rosterCount;
        }

        public static PostedLimitSnapshot None => default;
    }

    public struct PostedLimitCache
    {
        public int KmhRounded;
        public int RosterCount;
        public bool Seeded;
    }

    public static class PostedLimitTelemetry
    {
        public static bool Observe(
            in PostedLimitSnapshot snapshot,
            ref PostedLimitCache cache,
            out PostedLimitSnapshot published)
        {
            published = snapshot;
            var kmh = snapshot.Kmh is float value
                ? Round(value)
                : -1;
            if (cache.Seeded
                && cache.KmhRounded == kmh
                && cache.RosterCount == snapshot.RosterCount)
            {
                return false;
            }

            cache.Seeded = true;
            cache.KmhRounded = kmh;
            cache.RosterCount = snapshot.RosterCount;
            return true;
        }

        public static void Reset(ref PostedLimitCache cache) => cache = default;

        private static int Round(float value) =>
            (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    public static class PostedBoardTelemetry
    {
        public static string FormatFot(int rawCount, int parsedCount) =>
            "T2 boards fot: raw=" + rawCount.ToString() + " parsed=" + parsedCount.ToString();
    }
}
