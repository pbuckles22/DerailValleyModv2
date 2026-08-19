using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure grade formatting. Slope ratio is rise/run (m/m); display as percent with sign.
/// </summary>
public static class GradeDisplay
{
    /// <summary>
    /// Grade percent from a direction vector (e.g. car forward). Positive = climbing.
    /// </summary>
    public static float PercentFromDirection(float x, float y, float z)
    {
        var horizontal = Math.Sqrt((x * x) + (z * z));
        if (horizontal < 1e-6)
        {
            return 0f;
        }

        return (float)(y / horizontal * 100.0);
    }

    public static string FormatPercent(float? gradePercent) =>
        gradePercent is null
            ? "— Grade"
            : $"Grade {FormatSignedToken(gradePercent)} %";

    /// <summary>Display bucket: tenths of a percent. Unknown is <see cref="int.MinValue"/>.</summary>
    public static int BucketTenths(float? gradePercent)
    {
        if (gradePercent is null)
        {
            return int.MinValue;
        }

        var rounded = RoundDisplay(gradePercent.Value);
        return (int)Math.Round(rounded * 10f, MidpointRounding.AwayFromZero);
    }

    public static string FormatSignedToken(float? gradePercent)
    {
        if (gradePercent is null)
        {
            return "—";
        }

        var rounded = RoundDisplay(gradePercent.Value);
        if (rounded == 0f)
        {
            return "0.0";
        }

        return rounded > 0f ? $"+{rounded:0.0}" : $"{rounded:0.0}";
    }

    private static float RoundDisplay(float value)
    {
        var rounded = (float)Math.Round(value, 1, MidpointRounding.AwayFromZero);
        return Math.Abs(rounded) < 0.05f ? 0f : rounded;
    }
}
