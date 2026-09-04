using System;

namespace YardMasterSuite.Core;

/// <summary>
/// <b>9.1</b> cruise target: <c>min(request, Posted Limit)</c>. Missing/non-positive
/// posted → request only. Default request is 25 km/h.
/// <b>13.4.18:</b> drive-to-TT uses yard crawl (~10 km/h) so rem ≤ d_stop fits on the table.
/// <b>13.2.4:</b> Prep uses creep ≤ <see cref="AutoCoupleAssist.MaxCoupleSpeedKmh"/> (couple window).
/// </summary>
public static class PidSpeedTarget
{
    public const float DefaultRequestKmh = 25f;

    /// <summary>Gemini yard/TT approach ceiling (8–10 band).</summary>
    public const float YardApproachRequestKmh = 10f;

    public static bool WantsYardApproachCap(SwitchListStep? step)
    {
        if (step == null)
        {
            return false;
        }

        // Prep creep is owned by 13.2.4 — not the TT yard crawl.
        if (step.Kind == SwitchListStepKind.Prep)
        {
            return false;
        }

        return SwitchListDriveFacing.IsDriveToTurntable(step.Label);
    }

    /// <summary>Cruise request for the active Switch List step (GO / hold).</summary>
    public static float RequestForStep(SwitchListStep? step)
    {
        if (PrepCreepPolicy.WantsCreepCap(step))
        {
            return PrepCreepPolicy.CreepRequestKmh;
        }

        return WantsYardApproachCap(step) ? YardApproachRequestKmh : DefaultRequestKmh;
    }

    public static float Resolve(float requestKmh, float? postedKmh)
    {
        var request = PositiveOrDefault(requestKmh, DefaultRequestKmh);
        if (postedKmh is float posted && posted > 0f && !float.IsNaN(posted))
        {
            return Math.Min(request, posted);
        }

        return request;
    }

    private static float PositiveOrDefault(float value, float fallback)
    {
        if (float.IsNaN(value) || value <= 0f)
        {
            return fallback;
        }

        return value;
    }
}
