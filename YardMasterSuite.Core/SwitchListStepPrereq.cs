namespace YardMasterSuite.Core;

/// <summary>
/// Beginning-of-step prerequisites only (never end-of-step / CLEARED rise):
/// Align when 8.7 Ok, then Facing → reverser for the <em>current</em> row.
/// </summary>
public static class SwitchListStepPrereq
{
    public static bool WantsFacingPrep(SwitchListStep? step) =>
        step != null && SwitchListStepDisplay.UsesLiveDriveFacing(step.Kind);

    /// <summary>
    /// Live dest/pin facing wins; else planner label Set Reverse / Set Forward.
    /// </summary>
    public static bool ResolveNeedsReverse(string? stepLabel, bool? liveNeedsReverse)
    {
        if (liveNeedsReverse.HasValue)
        {
            return liveNeedsReverse.Value;
        }

        return PidSpeedGear.LabelNeedsReverse(stepLabel);
    }

    public static float TargetReverser(bool needsReverse) =>
        PidSpeedGear.TargetReverser(needsReverse);

    /// <summary>
    /// Cab knuckle: facing-prep at ~2 km/h + first notch blows TMS. Wait for crawl.
    /// </summary>
    public static bool ShouldWriteFacingPrep(float speedKmh) =>
        speedKmh < PidSpeedHold.DepartureCrawlKmh;

    /// <summary>
    /// Cab 22.37 / dropzone Issue 1 D: leftover GO thr + R after couple-Next.
    /// Flip the new row only when fully stopped and idle — not crawl 2 km/h.
    /// Cab 22.41: never while TMS Hot/Dead.
    /// </summary>
    public static bool ShouldWriteFacingPostCouple(
        float speedKmh,
        float throttle01,
        MotorStatus? motors = null) =>
        MotorDisplay.AllowsGoWrites(motors)
        && PidGoStop.ReadyToAdvanceAfterCleared(speedKmh, throttle01);
}
