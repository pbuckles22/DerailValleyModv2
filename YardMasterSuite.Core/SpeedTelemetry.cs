using System;

namespace YardMasterSuite.Core
{
    /// <summary>Boarded speed in km/h (rounded). Type A payload.</summary>
    public readonly struct SpeedSnapshot
    {
        public readonly int Kmh;

        public SpeedSnapshot(int kmh)
        {
            Kmh = kmh;
        }
    }

    public struct SpeedCache
    {
        public int Kmh;
        public bool Seeded;
    }

    /// <summary>
    /// Unity-free speed gate. Publishes when rounded km/h changes (not per physics tick).
    /// </summary>
    public static class SpeedTelemetry
    {
        public const float MinChangeKmh = 1f;

        public static void Reset(ref SpeedCache cache)
        {
            cache = default;
        }

        public static bool Observe(float metersPerSecond, ref SpeedCache cache, out SpeedSnapshot snapshot)
        {
            snapshot = default;
            var kmh = RoundKmh(Math.Abs(metersPerSecond) * SpeedDisplay.MetersPerSecondToKmh);
            if (cache.Seeded && cache.Kmh == kmh)
            {
                return false;
            }

            cache.Seeded = true;
            cache.Kmh = kmh;
            snapshot = new SpeedSnapshot(kmh);
            return true;
        }

        public static string? FormatLog(int kmh, bool wasSeeded)
        {
            if (!wasSeeded)
            {
                return "T2 speed init: " + kmh;
            }

            return "T2 speed change: " + kmh;
        }

        private static int RoundKmh(float kmh) =>
            (int)Math.Round(kmh, MidpointRounding.AwayFromZero);
    }
}
