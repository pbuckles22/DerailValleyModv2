using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Meters to Regular destroy edge for job Preview (**6.20**).
/// Taken jobs never use this chip. HUD subtracts a 30 m consist buffer;
/// the game wipe distance is unchanged.
/// </summary>
public static class PreviewEdgeDisplay
{
    public const float WarningMeters = 200f;
    public const float CriticalMeters = 50f;
    public const float SafetyBufferMeters = 30f;
    public const string WarningColor = "#FFD400";
    public const string CriticalColor = "#FF5555";
    public const string Label = "Preview";

    public static float? MetersRemaining(float? playerDistanceFromCenter, float? zoneRadiusMeters)
    {
        if (playerDistanceFromCenter is null || zoneRadiusMeters is null || zoneRadiusMeters.Value <= 0f)
        {
            return null;
        }

        return (zoneRadiusMeters.Value - playerDistanceFromCenter.Value) - SafetyBufferMeters;
    }

    public static float? RadiusFromSqr(float? zoneRadiusSquared)
    {
        if (zoneRadiusSquared is null || zoneRadiusSquared.Value <= 0f)
        {
            return null;
        }

        return (float)Math.Sqrt(zoneRadiusSquared.Value);
    }

    public static float? DistanceFromSqr(float? playerDistanceSquared)
    {
        if (playerDistanceSquared is null || playerDistanceSquared.Value < 0f)
        {
            return null;
        }

        return (float)Math.Sqrt(playerDistanceSquared.Value);
    }

    /// <summary>Keep the smaller remaining distance (most urgent wipe).</summary>
    public static void ConsiderUrgent(ref float? best, float? candidate)
    {
        if (candidate is not float c)
        {
            return;
        }

        if (best is null || c < best.Value)
        {
            best = c;
        }
    }

    public static string Format(float? metersRemaining, bool richText = false)
    {
        if (metersRemaining is null)
        {
            return $"— {Label}";
        }

        string text;
        if (metersRemaining.Value < 0f)
        {
            text = $"{Label} OUT";
        }
        else
        {
            var meters = (int)Math.Round(metersRemaining.Value, MidpointRounding.AwayFromZero);
            text = $"{Label} {meters}m";
        }

        if (!richText)
        {
            return text;
        }

        if (metersRemaining.Value < CriticalMeters)
        {
            return $"<color={CriticalColor}>{text}</color>";
        }

        if (metersRemaining.Value < WarningMeters)
        {
            return $"<color={WarningColor}>{text}</color>";
        }

        return text;
    }
}
