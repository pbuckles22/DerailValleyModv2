using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Discrete look-at identity for T2. HUD may still refresh analog pipe/mass.
    /// </summary>
    public struct LookAtBarCache
    {
        public bool Seeded;
        public bool Visible;
        public int CarToken;
        public string CargoRaw;
        public string TrackId;
        public string JobId;
    }

    /// <summary>
    /// Unity-free look-at log gate (**6.2**). Emit on appear / identity change / hide.
    /// Pipe pressure chatter is silent.
    /// </summary>
    public static class LookAtBarTelemetry
    {
        public const int CarTokenLoco = -1;
        public const int CarTokenUnknown = 0;

        public static void Reset(ref LookAtBarCache cache)
        {
            cache = default;
        }

        public static int CarToken(bool isLoco, int? freightNumberFromLoco)
        {
            if (isLoco)
            {
                return CarTokenLoco;
            }

            return freightNumberFromLoco ?? CarTokenUnknown;
        }

        public static string? Observe(
            bool visible,
            int carToken,
            string? cargoRaw,
            string? trackId,
            ref LookAtBarCache cache,
            string? jobId = null)
        {
            cargoRaw = cargoRaw ?? string.Empty;
            trackId = trackId ?? string.Empty;
            jobId = jobId ?? string.Empty;

            if (!visible)
            {
                if (!cache.Seeded || !cache.Visible)
                {
                    cache.Seeded = true;
                    cache.Visible = false;
                    return null;
                }

                cache.Visible = false;
                return "T2 look-at bar: hide";
            }

            if (cache.Seeded
                && cache.Visible
                && cache.CarToken == carToken
                && string.Equals(cache.CargoRaw, cargoRaw, StringComparison.Ordinal)
                && string.Equals(cache.TrackId, trackId, StringComparison.Ordinal)
                && string.Equals(cache.JobId, jobId, StringComparison.Ordinal))
            {
                return null;
            }

            cache.Seeded = true;
            cache.Visible = true;
            cache.CarToken = carToken;
            cache.CargoRaw = cargoRaw;
            cache.TrackId = trackId;
            cache.JobId = jobId;
            return FormatLog(carToken, cargoRaw, trackId, jobId);
        }

        public static string FormatLog(int carToken, string cargoRaw, string trackId, string? jobId = null)
        {
            var car = carToken == CarTokenLoco
                ? "NA"
                : carToken == CarTokenUnknown
                    ? "XX"
                    : carToken.ToString();
            var cargo = CargoKey(isLoco: carToken == CarTokenLoco, cargoRaw);
            var line = "T2 look-at bar: car=" + car + " cargo=" + cargo + " track=" + trackId;
            if (!string.IsNullOrEmpty(jobId))
            {
                line += " job=" + jobId;
            }

            return line;
        }

        public static string CargoKey(bool isLoco, string? cargoRaw)
        {
            if (isLoco)
            {
                return string.Empty;
            }

            var formatted = CargoDisplay.Format(isLoco: false, cargoRaw);
            if (formatted == null || formatted == "Empty Cargo")
            {
                return "Empty";
            }

            const string prefix = "Cargo ";
            if (formatted.StartsWith(prefix, StringComparison.Ordinal))
            {
                return formatted.Substring(prefix.Length);
            }

            return formatted;
        }
    }
}
