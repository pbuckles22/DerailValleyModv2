using System;

namespace YardMasterSuite.Core;

/// <summary>
/// DE2 expander: 11 notches as HUD percents (9, 18, 27…), not <c>n/11</c>.
/// 2.9.1.8 <c>Set(2/11)</c> / <c>Set(3/11)</c> stayed at <c>thr=9 indy=0</c>
/// because the cab only accepts <c>0.09</c> / <c>0.18</c> / <c>0.27</c>.
/// </summary>
public static class PidSpeedNotch
{
    public const int Steps = 11;

    /// <summary>HUD first notch (9%). Not <c>1/11</c>.</summary>
    public const float Step = 0.09f;

    public const float ExactEpsilon = 1e-4f;

    public static float FromNotch(int n)
    {
        if (n <= 0)
        {
            return 0f;
        }

        if (n >= Steps)
        {
            return 1f;
        }

        return n * Step;
    }

    public static float Snap(float value)
    {
        var v = Clamp01(value);
        var n = (int)Math.Round(v / Step, MidpointRounding.AwayFromZero);
        if (n < 0)
        {
            n = 0;
        }

        if (n > Steps)
        {
            n = Steps;
        }

        return FromNotch(n);
    }

    public static float Floor(float value)
    {
        var v = Clamp01(value);
        if (v <= ExactEpsilon)
        {
            return 0f;
        }

        var n = (int)Math.Floor((v / Step) + ExactEpsilon);
        if (n < 0)
        {
            n = 0;
        }

        if (n > Steps)
        {
            n = Steps;
        }

        return FromNotch(n);
    }

    public static bool IsExact(float value)
    {
        var v = Clamp01(value);
        return Math.Abs(v - Snap(v)) <= ExactEpsilon
            && Math.Abs(v - Hud(v)) <= ExactEpsilon;
    }

    /// <summary>HUD percent grid (0.09, 0.18, 0.27). <c>2/11</c> is not this.</summary>
    public static float Hud(float value)
    {
        var n = (int)Math.Round(Clamp01(value) * 100.0, MidpointRounding.AwayFromZero);
        if (n < 0)
        {
            n = 0;
        }

        if (n > 100)
        {
            n = 100;
        }

        return n / 100f;
    }

    /// <param name="firstPunchFromZero">
    /// Throttle: <c>Set(0.125)</c> from 0 becomes first notch. Independent
    /// overspeed <c>0.22</c> must stay 0 until an exact HUD notch (2.9.1.6).
    /// </param>
    public static float ApplyExpander(float desired, float current, bool firstPunchFromZero)
    {
        var cur = Snap(current);
        if (IsExact(desired))
        {
            return Snap(desired);
        }

        if (firstPunchFromZero && cur <= ExactEpsilon && desired + ExactEpsilon >= Step)
        {
            return Step;
        }

        return cur;
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
