namespace YardMasterSuite.Core
{
    /// <summary>Posted Limit snapshot (sticky km/h, Next, last FoT roster size).</summary>
    public readonly struct PostedLimitSnapshot
    {
        public readonly float? Kmh;
        public readonly int RosterCount;
        public readonly float? NextKmh;
        public readonly float? NextAlongMeters;

        public PostedLimitSnapshot(
            float? kmh,
            int rosterCount,
            float? nextKmh = null,
            float? nextAlongMeters = null)
        {
            Kmh = kmh;
            RosterCount = rosterCount;
            NextKmh = nextKmh;
            NextAlongMeters = nextAlongMeters;
        }

        public static PostedLimitSnapshot None => default;
    }

    public struct PostedLimitCache
    {
        public int KmhRounded;
        public int RosterCount;
        public int NextRounded;
        public int NextBucket;
        public bool Seeded;
    }

    public static class PostedLimitTelemetry
    {
        public static bool Observe(
            in PostedLimitSnapshot snapshot,
            ref PostedLimitCache cache,
            out PostedLimitSnapshot published,
            float massTonnes = 40f)
        {
            published = snapshot;
            var kmh = snapshot.Kmh is float value
                ? Round(value)
                : -1;
            var next = snapshot.NextKmh is float n
                ? Round(n)
                : -1;
            var from = snapshot.Kmh ?? SpeedLimitState.UnrestrictedKmh;
            var bucket = snapshot.NextKmh is float nk && snapshot.NextAlongMeters is float along
                ? NextLimitReveal.PublishBucket(along, from, nk, massTonnes)
                : -1;
            if (cache.Seeded
                && cache.KmhRounded == kmh
                && cache.RosterCount == snapshot.RosterCount
                && cache.NextRounded == next
                && cache.NextBucket == bucket)
            {
                return false;
            }

            cache.Seeded = true;
            cache.KmhRounded = kmh;
            cache.RosterCount = snapshot.RosterCount;
            cache.NextRounded = next;
            cache.NextBucket = bucket;
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
