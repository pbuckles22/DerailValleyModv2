namespace YardMasterSuite.Core;

/// <summary>
/// Prep GO: sustain <see cref="CreepRequestKmh"/> until touch window or knuckle.
/// Kiss rem / mass envelope must not Stop GO (2nd pickup short-stop).
/// </summary>
public static class PrepCreepPolicy
{
    /// <summary>
    /// Engineer reverse creep (km/h). Distinct from
    /// <see cref="PidSpeedHold.DepartureCrawlKmh"/> (2 km/h gear-wait).
    /// </summary>
    public const float CreepRequestKmh = 3f;

    public static bool WantsCreepCap(SwitchListStep? step) =>
        step != null && step.Kind == SwitchListStepKind.Prep;

    public static bool IsGreenClearance(float? clearanceMeters, bool partnerInCoupleRange) =>
        partnerInCoupleRange && AutoCoupleAssist.ClearanceAllowsCouple(clearanceMeters);

    public static bool ShouldStopGoForCouple(
        SwitchListRunMode mode,
        SwitchListStep? step,
        float? clearanceMeters,
        float speedKmh,
        bool mechanicallyCoupled,
        bool spurPickupComplete = false)
    {
        _ = speedKmh;
        return PrepCoupleExitGate.ShouldStopGoOnPrepApproach(
            mode,
            step,
            mechanicallyCoupled,
            spurPickupComplete,
            PrepSpurPickupSession.UnattachedOnPrepSpur,
            clearanceMeters);
    }
}
