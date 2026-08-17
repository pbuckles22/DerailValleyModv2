namespace YardMasterSuite.Core
{
    /// <summary>Active speed-limit source for the HUD (geometry first; posted boards later).</summary>
    public enum LimitAuthority
    {
        None = 0,
        Geometry = 1,
    }

    /// <summary>Current limit snapshot. Type A payload.</summary>
    public readonly struct SpeedLimitSnapshot
    {
        public readonly float? LimitKmh;
        public readonly LimitAuthority Authority;

        public SpeedLimitSnapshot(float? limitKmh, LimitAuthority authority)
        {
            LimitKmh = limitKmh;
            Authority = authority;
        }

        public static SpeedLimitSnapshot None => default;
    }

    /// <summary>Merge geometry scan into a display limit.</summary>
    public static class SpeedLimitState
    {
        public static SpeedLimitSnapshot FromGeometry(in GeometryScanResult scan)
        {
            if (scan.SegmentId == 0 || !scan.HasLimit)
            {
                return SpeedLimitSnapshot.None;
            }

            return new SpeedLimitSnapshot(scan.LimitKmh, LimitAuthority.Geometry);
        }
    }

    public struct SpeedLimitCache
    {
        public int LimitRounded;
        public LimitAuthority Authority;
        public bool Seeded;
    }

    /// <summary>Publish limit only when rounded km/h or authority changes.</summary>
    public static class SpeedLimitTelemetry
    {
        public static bool Observe(
            in SpeedLimitSnapshot snapshot,
            ref SpeedLimitCache cache,
            out SpeedLimitSnapshot published)
        {
            published = snapshot;
            var limit = snapshot.LimitKmh is float value
                ? Round(value)
                : -1;
            if (cache.Seeded
                && cache.LimitRounded == limit
                && cache.Authority == snapshot.Authority)
            {
                return false;
            }

            cache.Seeded = true;
            cache.LimitRounded = limit;
            cache.Authority = snapshot.Authority;
            return true;
        }

        public static string Format(in SpeedLimitSnapshot snapshot, bool wasSeeded)
        {
            if (!wasSeeded)
            {
                return FormatInit(snapshot);
            }

            return FormatChange(snapshot);
        }

        public static string FormatInit(in SpeedLimitSnapshot snapshot) =>
            "T2 limit init: " + LimitToken(snapshot);

        public static string FormatChange(in SpeedLimitSnapshot snapshot) =>
            "T2 limit change: " + LimitToken(snapshot);

        private static string LimitToken(in SpeedLimitSnapshot snapshot)
        {
            if (snapshot.LimitKmh is not float limit)
            {
                return "— auth=" + snapshot.Authority.ToString().ToLowerInvariant();
            }

            return Round(limit) + " auth=" + snapshot.Authority.ToString().ToLowerInvariant();
        }

        private static int Round(float value) =>
            (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }
}
