using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Pure speed formatting for Monitor HUD (no Unity / game refs).
    /// Game speeds are meters/second; DV UI uses km/h.
    /// </summary>
    public static class SpeedDisplay
    {
        public const float MetersPerSecondToKmh = 3.6f;

        public static float ToKilometersPerHour(float metersPerSecond) =>
            metersPerSecond * MetersPerSecondToKmh;

        public static string FormatKmh(float kilometersPerHour) =>
            "Speed " + RoundHalfAwayFromZero(kilometersPerHour) + " km/h";

        public static string FormatFromMetersPerSecond(float? metersPerSecond) =>
            metersPerSecond is null
                ? "— Speed"
                : FormatKmh(ToKilometersPerHour(metersPerSecond.Value));

        public static string FormatFromKmh(int? kilometersPerHour) =>
            kilometersPerHour is null
                ? "— Speed"
                : FormatKmh(kilometersPerHour.Value);

        /// <summary>HUD chip: omit when unknown (no <c>— Speed</c>).</summary>
        public static string FormatOrEmpty(int? kilometersPerHour) =>
            kilometersPerHour is null ? string.Empty : FormatKmh(kilometersPerHour.Value);

        private static int RoundHalfAwayFromZero(float value) =>
            (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
