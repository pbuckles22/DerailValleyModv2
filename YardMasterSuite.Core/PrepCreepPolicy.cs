namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.2.4</b> Prep GO creep-to-couple — crawl well under couple max so tip-scan
/// rem ≤ d_stop can brake before a knuckle slam; sticky hold blocks shove re-arm.
/// </summary>
public static class PrepCreepPolicy
{
    /// <summary>
    /// Prep GO cruise (km/h). Below <see cref="AutoCoupleAssist.MaxCoupleSpeedKmh"/> so
    /// d_stop fits inside the 1.5 m couple scan window.
    /// </summary>
    public const float CreepRequestKmh = 5f;

    public static bool WantsCreepCap(SwitchListStep? step) =>
        step != null && step.Kind == SwitchListStepKind.Prep;

    public static bool IsGreenClearance(float? clearanceMeters, bool partnerInCoupleRange) =>
        partnerInCoupleRange && AutoCoupleAssist.ClearanceAllowsCouple(clearanceMeters);

    /// <summary>
    /// Brake when tip is inside couple scan (1.5 m), rem ≤ d_stop / green band,
    /// or already mechanically coupled. Creep alone is not enough — at 5 km/h
    /// d_stop ≈ 0.5 m, so rem≤d_stop alone only fires on the knuckle.
    /// </summary>
    public static bool ShouldStopGoForCouple(
        SwitchListRunMode mode,
        SwitchListStep? step,
        float? clearanceMeters,
        float speedKmh,
        bool mechanicallyCoupled)
    {
        if (mode != SwitchListRunMode.Go
            || step == null
            || step.Kind != SwitchListStepKind.Prep)
        {
            return false;
        }

        if (mechanicallyCoupled)
        {
            return true;
        }

        if (clearanceMeters is null || float.IsNaN(clearanceMeters.Value))
        {
            return false;
        }

        var rem = clearanceMeters.Value;
        if (rem <= BackupProximityDisplay.CoupleNearRangeMeters)
        {
            return true;
        }

        return YardStopKinematics.ShouldStartStop(
            rem,
            speedKmh,
            BackupProximityDisplay.GreenMaxDisplayMeters);
    }
}
