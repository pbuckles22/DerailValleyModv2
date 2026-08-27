using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Speed-limit formatting for the loco HUD bar (v1 1.17 / 6.8).
    /// Yellow from 10 km/h below through 5 km/h above; red beyond. No Recommended/Brake chips.
    /// Posted Next uses <see cref="NextLimitReveal"/> for meters.
    /// </summary>
    public static class SpeedLimitDisplay
    {
        public const float NearBelowKmh = 10f;
        public const float NearAboveKmh = 5f;
        public const float NextKmThresholdMeters = 1000f;
        public const string WarningColor = "#FFD400";
        public const string CriticalColor = "#FF5555";

        public static string Format(
            float? limitKmh,
            float? nextKmh = null,
            float? nextDistanceMeters = null,
            float massTonnes = 40f,
            bool? showNextMeters = null) =>
            FormatCore(limitKmh, richText: false, LimitSeverity.None, nextKmh, nextDistanceMeters, massTonnes, showNextMeters);

        public static string FormatHud(
            float? speedKmh,
            float? limitKmh,
            float? nextKmh = null,
            float? nextDistanceMeters = null,
            float massTonnes = 40f,
            bool? showNextMeters = null) =>
            FormatCore(
                limitKmh,
                richText: true,
                Severity(speedKmh, limitKmh),
                nextKmh,
                nextDistanceMeters,
                massTonnes,
                showNextMeters);

        /// <summary>
        /// HUD chip: omit when unknown (no <c>— Limit</c>). Next is 6.10.
        /// </summary>
        public static string FormatHudOrEmpty(
            float? speedKmh,
            float? limitKmh,
            float? nextKmh = null,
            float? nextDistanceMeters = null,
            float massTonnes = 40f,
            bool? showNextMeters = null) =>
            limitKmh is null
                ? string.Empty
                : FormatHud(speedKmh, limitKmh, nextKmh, nextDistanceMeters, massTonnes, showNextMeters);

        public static LimitSeverity Severity(float? speedKmh, float? limitKmh)
        {
            if (speedKmh is null || limitKmh is null)
            {
                return LimitSeverity.None;
            }

            var speed = Round(speedKmh.Value);
            var limit = Round(limitKmh.Value);
            if (speed > limit + NearAboveKmh)
            {
                return LimitSeverity.Over;
            }

            if (speed >= limit - NearBelowKmh)
            {
                return LimitSeverity.Near;
            }

            return LimitSeverity.None;
        }

        public static string FormatNextDistance(float meters)
        {
            if (meters >= NextKmThresholdMeters)
            {
                return (meters / 1000f).ToString("0.0") + "km";
            }

            return Round(meters) + "m";
        }

        private static string FormatCore(
            float? limitKmh,
            bool richText,
            LimitSeverity severity,
            float? nextKmh,
            float? nextDistanceMeters,
            float massTonnes,
            bool? showNextMeters)
        {
            if (limitKmh is null)
            {
                return "— Limit";
            }

            var text = "Limit " + Round(limitKmh.Value);
            if (richText && severity != LimitSeverity.None)
            {
                var color = severity == LimitSeverity.Over ? CriticalColor : WarningColor;
                text = "<color=" + color + ">" + text + "</color>";
            }

            if (nextKmh is float next && nextDistanceMeters is float along && along > 0f)
            {
                var showMeters = showNextMeters
                    ?? NextLimitReveal.ShowDistance(along, limitKmh.Value, next, massTonnes);
                if (showMeters)
                {
                    text += " | Next " + Round(next) + " (" + FormatNextDistance(along) + ")";
                }
                else
                {
                    text += " | Next " + Round(next);
                }
            }

            return text;
        }

        private static int Round(float value) =>
            (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    public enum LimitSeverity
    {
        None,
        Near,
        Over,
    }
}
