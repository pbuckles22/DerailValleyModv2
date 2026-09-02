namespace YardMasterSuite.Core;

/// <summary>
/// Pure GO / Human / Done policy on a Switch List (**13.1** / HTP CP2).
/// Unity wires desk buttons; PID arms via <see cref="PidGoActive"/>.
/// </summary>
public static class SwitchListRunner
{
    public static bool StepRequiresHuman(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.Prep
            or SwitchListStepKind.ReverseInto
            or SwitchListStepKind.Delivery;

    /// <summary>Past-switch legs only — Align/Next wait for CLEARED.</summary>
    public static bool StepNeedsPinClearance(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.Transit or SwitchListStepKind.Pivot;

    /// <summary>Drive-set follows path pin approach (not only past-switch CLEARED legs).</summary>
    public static bool StepUsesApproachPinFacing(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.TurnAround
            or SwitchListStepKind.Prep
            or SwitchListStepKind.Transit
            or SwitchListStepKind.Pivot;

    /// <summary>Gate Align/Next/GO on the latched pin frog.</summary>
    public static bool PinBlocksAlignOrNext(
        SwitchListStep? step,
        bool planArmedForClearance,
        bool sessionHasPin) =>
        step != null
        && StepNeedsPinClearance(step.Kind)
        && (planArmedForClearance || sessionHasPin);

    /// <summary>
    /// Frog AR / 1/2 CLEARED coach. Route tab (no list) still shows the pin.
    /// Active Switch List only on Past-switch / Pivot — not leftover inbound
    /// CLEARED painted onto Prep after reload.
    /// </summary>
    public static bool PinDisplayAllowed(SwitchListStep? step, bool switchListActive)
    {
        if (!switchListActive)
        {
            return true;
        }

        return step != null && StepNeedsPinClearance(step.Kind);
    }

    public static string? FormatDropStalePinLog(string? pinId)
    {
        var id = pinId?.Trim();
        return string.IsNullOrEmpty(id)
            ? null
            : "T2 switch-list: list-load drop stale pin " + id;
    }

    public static bool StepSupportsGo(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.Transit or SwitchListStepKind.Pivot;

    public static SwitchListRunMode EnterModeForStep(SwitchListStep? step) =>
        step != null && StepRequiresHuman(step.Kind)
            ? SwitchListRunMode.HumanHold
            : SwitchListRunMode.Manual;

    public static bool AllowsManualNext(SwitchListRunMode mode) =>
        mode == SwitchListRunMode.Manual;

    public static bool PidGoActive(SwitchListRunMode mode, SwitchListStep? step) =>
        mode == SwitchListRunMode.Go
        && step != null
        && StepSupportsGo(step.Kind);

    public static SwitchListRunnerResult TrySetGo(
        SwitchListStep? step,
        bool hasPlan,
        bool pinForAlign,
        RouteClearancePhase clearancePhase)
    {
        if (step == null)
        {
            return SwitchListRunnerResult.NoActiveStep;
        }

        if (!StepSupportsGo(step.Kind))
        {
            return SwitchListRunnerResult.WrongStepKind;
        }

        if (!hasPlan)
        {
            return SwitchListRunnerResult.NeedPlan;
        }

        if (RouteClearanceGate.Align(pinForAlign, clearancePhase) == RouteClearanceGateReason.NeedCleared)
        {
            return SwitchListRunnerResult.NeedCleared;
        }

        return SwitchListRunnerResult.Ok;
    }

    public static SwitchListRunnerResult TryMarkDone(SwitchListRunMode mode) =>
        mode == SwitchListRunMode.HumanHold
            ? SwitchListRunnerResult.Ok
            : SwitchListRunnerResult.NotHumanHold;

    public static SwitchListRunnerResult TryStopGo(SwitchListRunMode mode) =>
        mode == SwitchListRunMode.Go
            ? SwitchListRunnerResult.Ok
            : SwitchListRunnerResult.NotGoActive;

    public static SwitchListRunnerResult TryManualNext(SwitchListRunMode mode) =>
        AllowsManualNext(mode)
            ? SwitchListRunnerResult.Ok
            : SwitchListRunnerResult.NextBlocked;
}
