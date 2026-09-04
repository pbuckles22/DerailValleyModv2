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
}
