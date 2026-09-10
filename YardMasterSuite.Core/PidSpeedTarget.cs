using System;

namespace YardMasterSuite.Core;

/// <summary>
/// <b>9.1</b> cruise. Yard kiss legs (CLEARED / TT mid / Prep cars) stay 25
/// until Stop GO — <see cref="YardKissPolicy"/>.
/// </summary>
public static class PidSpeedTarget
{
    public const float DefaultRequestKmh = 25f;

    /// <summary>Mid taper band (10–30 m rem). Alias of approach band.</summary>
    public const float YardApproachRequestKmh = YardApproachKinematics.ApproachSpeedKmh;

    /// <summary>Deprecated: kiss legs cruise 25 when blind (cab 4.6 TT/Prep locked at 12).</summary>
    public const float BlindApproachFallbackKmh = DefaultRequestKmh;

    public static bool WantsYardTaper(SwitchListStep? step) =>
        WantsYardTaper(step, inYardPrepScope: true);

    public static bool WantsYardTaper(SwitchListStep? step, bool inYardPrepScope)
    {
        if (step == null)
        {
            return false;
        }

        if (step.Kind == SwitchListStepKind.Prep
            || step.Kind == SwitchListStepKind.ReverseInto)
        {
            return true;
        }

        if (SwitchListDriveFacing.IsDriveToTurntable(step.Label))
        {
            return true;
        }

        return inYardPrepScope && SwitchListRunner.StepNeedsPinClearance(step.Kind);
    }

    public static bool WantsYardApproachCap(SwitchListStep? step) => WantsYardTaper(step);

    public static float RequestForStep(SwitchListStep? step) =>
        RequestForYardStep(step, null, null, null, null, atDestTrack: false);

    public static float RequestForStep(SwitchListStep? step, float? remainingMeters) =>
        RequestForYardStep(step, remainingMeters, null, null, null, atDestTrack: false);

    /// <summary>
    /// Yard kiss legs request cruise; Stop GO does the stop. No second creep GO.
    /// </summary>
    public static float RequestForYardStep(
        SwitchListStep? step,
        float? corridorRemMeters,
        float? hudProximityMeters,
        float? pinRemToClearedMeters,
        float? ttRemToMidMeters,
        bool atDestTrack = false,
        bool inYardPrepScope = true)
    {
        _ = atDestTrack;
        _ = corridorRemMeters;
        _ = hudProximityMeters;
        _ = pinRemToClearedMeters;
        _ = ttRemToMidMeters;
        return YardKissPolicy.RequestKmh(step, inYardPrepScope);
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
