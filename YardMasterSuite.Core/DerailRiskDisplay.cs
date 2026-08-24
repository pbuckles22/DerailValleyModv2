using System;

namespace YardMasterSuite.Core;

/// <summary>Lead loco vs consist-max buildup %. HUD uses <see cref="MaxPercent"/>.</summary>
public readonly struct DerailRiskScan
{
    public readonly float? LeadPercent;
    public readonly float? MaxPercent;

    public DerailRiskScan(float? leadPercent, float? maxPercent)
    {
        LeadPercent = leadPercent;
        MaxPercent = maxPercent;
    }
}

/// <summary>
/// Cab Derail Risk chip (**6.19**): consist-max <c>derailBuildUp</c> % of game threshold
/// (worst car, including wagons). Coupler tension is ignored. Always shown while boarded.
/// Yellow ≥ 15 % (slow down); red ≥ 95 % (stop now).
/// </summary>
public static class DerailRiskDisplay
{
    public const float WarningThresholdPercent = 15f;
    public const float CriticalThresholdPercent = 95f;

    /// <summary>Green — no threat.</summary>
    public const string OkColor = "#55FF55";

    /// <summary>Yellow — slow down. Matches MU / Load warning tone.</summary>
    public const string WarningColor = "#FFD400";

    /// <summary>Red — stop now.</summary>
    public const string CriticalColor = "#FF5555";

    public static float? PercentOfBuildUp(float? derailBuildUp, float? buildUpThreshold)
    {
        if (derailBuildUp is not float v
            || buildUpThreshold is not float thr
            || thr < 0.01f)
        {
            return null;
        }

        return (v / thr) * 100f;
    }

    /// <summary>Keep the worse usable percent. Null candidates are skipped.</summary>
    public static void ConsiderMax(ref float? worst, float? candidate)
    {
        if (candidate is not float c)
        {
            return;
        }

        if (worst is null || c > worst.Value)
        {
            worst = c;
        }
    }

    /// <summary>Whole-percent bucket. Unknown is <see cref="int.MinValue"/>.</summary>
    public static int BucketPercent(float? riskPercent)
    {
        if (riskPercent is null)
        {
            return int.MinValue;
        }

        var whole = (int)Math.Round(riskPercent.Value, MidpointRounding.AwayFromZero);
        if (whole < 0)
        {
            return 0;
        }

        return whole;
    }

    public static string FormatPercentToken(float? riskPercent) =>
        riskPercent is null ? "—" : BucketPercent(riskPercent).ToString();

    public static string Format(float? riskPercent) =>
        FormatCore(riskPercent, richText: false);

    public static string FormatHud(float? riskPercent) =>
        FormatCore(riskPercent, richText: true);

    private static string FormatCore(float? riskPercent, bool richText)
    {
        if (riskPercent is null)
        {
            return "— Derail Risk";
        }

        var whole = BucketPercent(riskPercent);
        var text = $"Derail Risk {whole} %";
        if (!richText)
        {
            return text;
        }

        if (whole >= CriticalThresholdPercent)
        {
            return $"<color={CriticalColor}>{text}</color>";
        }

        if (whole >= WarningThresholdPercent)
        {
            return $"<color={WarningColor}>{text}</color>";
        }

        return $"<color={OkColor}>{text}</color>";
    }
}
