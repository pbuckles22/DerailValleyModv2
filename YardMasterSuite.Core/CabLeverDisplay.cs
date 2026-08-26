using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Cab lever position chips (0–1 control value → whole percent).
/// TrainBrake = automatic/train brake lever — distinct from advisory <c>Brake N in …</c>.
/// </summary>
public static class CabLeverDisplay
{
    public static float? PercentFromNormalized(float? normalized01)
    {
        if (normalized01 is null)
        {
            return null;
        }

        return ClampPercent(normalized01.Value * 100f);
    }

    public static string FormatThrottle(float? percent) => FormatCore("Throttle", percent);

    public static string FormatThrottle(float? percent, bool flashRed, bool flashLit) =>
        FormatCore("Throttle", percent, emphasize: flashRed && flashLit);

    public static string FormatIndy(float? percent) => FormatCore("Indy", percent);

    public static string FormatIndy(float? percent, bool flashRed, bool flashLit) =>
        FormatCore("Indy", percent, emphasize: flashRed && flashLit);

    public static string FormatTrainBrake(float? percent) => FormatCore("TrainBrake", percent);

    public static string FormatTrainBrake(float? percent, bool flashRed, bool flashLit) =>
        FormatCore("TrainBrake", percent, emphasize: flashRed && flashLit);

    private static string FormatCore(string label, float? percent, bool emphasize = false)
    {
        string core;
        if (percent is null)
        {
            core = "— " + label;
        }
        else
        {
            var whole = (int)Math.Round(percent.Value, MidpointRounding.AwayFromZero);
            core = label + " " + whole + " %";
        }

        if (!emphasize)
        {
            return core;
        }

        return "<color=" + DerailRiskDisplay.CriticalColor + ">" + core + "</color>";
    }

    private static float ClampPercent(float value)
    {
        if (value < 0f)
        {
            return 0f;
        }

        if (value > 100f)
        {
            return 100f;
        }

        return value;
    }
}
