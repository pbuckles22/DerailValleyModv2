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
        public readonly float? NextKmh;
        public readonly float? NextAlongMeters;

        public SpeedLimitSnapshot(
            float? limitKmh,
            LimitAuthority authority,
            float? nextKmh = null,
            float? nextAlongMeters = null)
        {
            LimitKmh = limitKmh;
            Authority = authority;
            NextKmh = nextKmh;
            NextAlongMeters = nextAlongMeters;
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

        public static SpeedLimitSnapshot Resolve(
            bool hasUsableLoco,
            float? postedKmh,
            float? nextKmh = null,
            float? nextAlongMeters = null)
        {
            if (!hasUsableLoco)
            {
                return SpeedLimitSnapshot.None;
            }

            float? next = null;
            float? along = null;
            if (nextKmh is float nk && nk > 0f && nextAlongMeters is float d && d > 0f)
            {
                next = nk;
                along = d;
            }

            if (postedKmh is float kmh && kmh > 0f)
            {
                return new SpeedLimitSnapshot(kmh, LimitAuthority.Posted, next, along);
            }

            return new SpeedLimitSnapshot(UnrestrictedKmh, LimitAuthority.Default, next, along);
        }
    }

    public struct SpeedLimitCache
    {
        public int LimitRounded;
        public LimitAuthority Authority;
        public int NextRounded;
        public int NextBucket;
        public int LogBand;
        public bool EmitLog;
        public bool Seeded;
    }

    /// <summary>Publish limit only when rounded km/h, authority, or Next chip changes.</summary>
    public static class SpeedLimitTelemetry
    {
        public static bool Observe(
            in SpeedLimitSnapshot snapshot,
            ref SpeedLimitCache cache,
            out SpeedLimitSnapshot published,
            float massTonnes = 40f)
        {
            published = snapshot;
            var limit = snapshot.LimitKmh is float value
                ? Round(value)
                : -1;
            var next = snapshot.NextKmh is float n
                ? Round(n)
                : -1;
            var bucket = NextBucket(in snapshot, massTonnes);
            var logBand = bucket < 0 ? bucket : 0;
            var log = !cache.Seeded
                || cache.LimitRounded != limit
                || cache.Authority != snapshot.Authority
                || cache.NextRounded != next
                || cache.LogBand != logBand;
            var hud = log || cache.NextBucket != bucket;
            if (cache.Seeded && !hud)
            {
                cache.EmitLog = false;
                return false;
            }

            cache.Seeded = true;
            cache.LimitRounded = limit;
            cache.Authority = snapshot.Authority;
            cache.NextRounded = next;
            cache.NextBucket = bucket;
            cache.LogBand = logBand;
            cache.EmitLog = log;
            return true;
        }

        public static string Format(in SpeedLimitSnapshot snapshot, bool wasSeeded, float massTonnes = 40f)
        {
            if (!wasSeeded)
            {
                return FormatInit(snapshot, massTonnes);
            }

            return FormatChange(snapshot, massTonnes);
        }

        public static string FormatInit(in SpeedLimitSnapshot snapshot, float massTonnes = 40f) =>
            "T2 limit init: " + LimitToken(snapshot, massTonnes);

        public static string FormatChange(in SpeedLimitSnapshot snapshot, float massTonnes = 40f) =>
            "T2 limit change: " + LimitToken(snapshot, massTonnes);

        private static string LimitToken(in SpeedLimitSnapshot snapshot, float massTonnes)
        {
            string core;
            if (snapshot.LimitKmh is not float limit)
            {
                core = "— auth=" + snapshot.Authority.ToString().ToLowerInvariant();
            }
            else
            {
                core = Round(limit) + " auth=" + snapshot.Authority.ToString().ToLowerInvariant();
            }

            if (snapshot.NextKmh is not float next
                || snapshot.NextAlongMeters is not float along
                || along <= 0f)
            {
                return core;
            }

            var token = core + " next=" + Round(next);
            var from = snapshot.LimitKmh ?? SpeedLimitState.UnrestrictedKmh;
            if (NextLimitReveal.ShowDistance(along, from, next, massTonnes))
            {
                token += " " + SpeedLimitDisplay.FormatNextDistance(along);
            }

            return token;
        }

        private static int NextBucket(in SpeedLimitSnapshot snapshot, float massTonnes)
        {
            if (snapshot.NextKmh is not float next
                || snapshot.NextAlongMeters is not float along)
            {
                return -1;
            }

            var from = snapshot.LimitKmh ?? SpeedLimitState.UnrestrictedKmh;
            return NextLimitReveal.PublishBucket(along, from, next, massTonnes);
        }

        private static int Round(float value) =>
            (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }
}
