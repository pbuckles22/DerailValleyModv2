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
    /// Align / reverser / TT wait until the consist is stopped. Next still
    /// advances the row and Sets dest; mechanical prep waits for standstill.
    /// </summary>
    public static bool AllowsAutoPrep(float absSpeedKmh) =>
        PidSpeedGear.AllowsReverserWrite(absSpeedKmh);

    /// <summary>
    /// Live dest/pin facing wins; else planner label Set Reverse / Set Forward.
    /// Cruise off still uses this — only PID/drive is gated on Cruise.
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
}
