namespace YardMasterSuite.Core
{
    /// <summary>HUD Limit authority: posted sticky only (geometry retired).</summary>
    public enum LimitAuthority
    {
        None = 0,
        /// <summary>Usable loco present, no posted take yet — show 120.</summary>
        Default = 1,
        Posted = 2,
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

    /// <summary>
    /// Posted sticky Limit for the HUD. No geometry. 120 until a take;
    /// hide when no usable loco.
    /// </summary>
    public static class SpeedLimitState
    {
        public const float UnrestrictedKmh = 120f;

        public static SpeedLimitSnapshot Resolve(bool hasUsableLoco, float? postedKmh)
        {
            if (!hasUsableLoco)
            {
                return SpeedLimitSnapshot.None;
            }

            if (postedKmh is float kmh && kmh > 0f)
            {
                return new SpeedLimitSnapshot(kmh, LimitAuthority.Posted);
            }

            return new SpeedLimitSnapshot(UnrestrictedKmh, LimitAuthority.Default);
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
