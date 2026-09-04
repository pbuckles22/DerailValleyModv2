namespace YardMasterSuite.Core;

/// <summary>
/// Foundational Switch List step rule (<b>13.4</b>):
/// <list type="number">
/// <item>Prerequisites for <em>this</em> step only (Align when 8.7 Ok, facing, …).</item>
/// <item>GO / drive that step.</item>
/// <item>Stop when the step ends (CLEARED can be the stop cue for an approach).</item>
/// </list>
/// CLEARED does <b>not</b> Align — that would prep work while finishing the prior
/// row. Align runs when entering the drive step (Next / Load) or as GO prep on
/// that same row. Never prep the next index while still on the current one.
/// </summary>
public static class SwitchListAutoAlign
{
    public static bool StepWantsAlignPrep(SwitchListStep? step) =>
        step != null
        && (SwitchListRunner.StepNeedsPinClearance(step.Kind)
            || SwitchListRunner.StepUsesApproachPinFacing(step.Kind));

    /// <summary>
    /// True when the <em>current</em> step may Align now (CLEARED / no pin gate Ok).
    /// Caller must already be on that step (enter or GO prep) — not on CLEARED rise.
    /// </summary>
    public static bool ShouldAutoAlign(
        SwitchListStep? step,
        bool pinBlocksAlign,
        RouteClearancePhase clearancePhase) =>
        StepWantsAlignPrep(step)
        && RouteClearanceGate.Align(pinBlocksAlign, clearancePhase)
            == RouteClearanceGateReason.Ok;
}
