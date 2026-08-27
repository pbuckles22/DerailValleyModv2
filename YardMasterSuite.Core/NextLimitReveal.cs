using System;

namespace YardMasterSuite.Core
{
    /// <summary>When to show meters on the Next chip (v1 1.17).</summary>
    public static class NextLimitReveal
    {
        public const float ComfortDecelMps2 = 0.25f;
        public const float MinRevealMeters = 120f;
        public const float MaxRevealMeters = 600f;
        public const float HysteresisMeters = 40f;

        public static float RevealMeters(float fromKmh, float toKmh, float massTonnes = 40f)
        {
            if (!(fromKmh > 0f) || !IsFinite(fromKmh) || !IsFinite(toKmh))
            {
                return MinRevealMeters;
            }

            if (toKmh + 0.5f >= fromKmh)
            {
                return MinRevealMeters;
            }

            var v0 = fromKmh / 3.6f;
            var v1 = Math.Max(0f, toKmh) / 3.6f;
            var mass = massTonnes > 0f && IsFinite(massTonnes) ? massTonnes : 40f;
            var a = ComfortDecelMps2 * (40f / Math.Max(40f, Math.Min(mass, 400f)));
            a = Math.Max(0.12f, a);
            var d = ((v0 * v0) - (v1 * v1)) / (2f * a);
            if (!IsFinite(d) || d < MinRevealMeters)
            {
                return MinRevealMeters;
            }

            return d > MaxRevealMeters ? MaxRevealMeters : d;
        }

        public static bool ShowDistance(
            float alongMeters,
            float fromKmh,
            float toKmh,
            float massTonnes = 40f,
            bool wasShowing = false)
        {
            if (!(alongMeters > 0f) || !IsFinite(alongMeters))
            {
                return false;
            }

            var reveal = RevealMeters(fromKmh, toKmh, massTonnes);
            if (alongMeters <= reveal)
            {
                return true;
            }

            return wasShowing && alongMeters <= reveal + HysteresisMeters;
        }

        /// <summary>
        /// Change-only publish key: none, Next without meters, or 10 m bucket when meters show.
        /// </summary>
        public static int PublishBucket(
            float alongMeters,
            float fromKmh,
            float toKmh,
            float massTonnes = 40f,
            bool wasShowing = false)
        {
            if (alongMeters <= 0f)
            {
                return -1;
            }

            if (!ShowDistance(alongMeters, fromKmh, toKmh, massTonnes, wasShowing))
            {
                return -2;
            }

            var round = (int)Math.Round(alongMeters, MidpointRounding.AwayFromZero);
            return (round / 10) * 10;
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
