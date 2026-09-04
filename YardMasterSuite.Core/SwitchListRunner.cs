namespace YardMasterSuite.Core;

/// <summary>
/// Pure GO / Human / Done policy on a Switch List (**13.1** / HTP CP2).
/// **13.4** thin: Derail Risk refuse on GO arm; Prep approach is a drive GO leg.
/// Unity wires desk buttons; PID arms via <see cref="PidGoActive"/>.
/// </summary>
public static class SwitchListRunner
{
    /// <summary>
    /// Knuckle / stall work only — not Prep approach (drive under GO).
    /// </summary>
    public static bool StepRequiresHuman(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.ReverseInto
            or SwitchListStepKind.Delivery;

    /// <summary>Past-switch legs only — Align/Next wait for CLEARED.</summary>
    public static bool StepNeedsPinClearance(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.Transit or SwitchListStepKind.Pivot;

    /// <summary>
    /// After CLEARED + Next, keep the frog pin only for consecutive
    /// past-switch / pivot. Next off spin or to-TT dismisses so the leave
    /// frog can re-latch.
    /// </summary>
    public static bool PinStaysAfterNext(SwitchListStep? next) =>
        PinStaysAfterNext(current: null, next);

    public static bool PinStaysAfterNext(SwitchListStep? current, SwitchListStep? next)
    {
        if (next == null || !StepNeedsPinClearance(next.Kind))
        {
            return false;
        }

        return current == null || StepNeedsPinClearance(current.Kind);
    }

    /// <summary>Drive-set follows path pin approach (not only past-switch CLEARED legs).</summary>
    public static bool StepUsesApproachPinFacing(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.TurnAround
            or SwitchListStepKind.Prep
            or SwitchListStepKind.Transit
            or SwitchListStepKind.Pivot;

    /// <summary>Gate Align/Next on the latched pin frog. GO arms without CLEARED.</summary>
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

    /// <summary>
    /// Any consist-move leg in yard/haul scope: Transit / Pivot / Prep approach /
    /// drive-to-TT. On-table TT spin stays manual. Couple knuckles until <b>13.2.4</b>;
    /// Delivery drop is <b>15.2</b>.
    /// </summary>
    public static bool StepSupportsGo(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.Transit
            or SwitchListStepKind.Pivot
            or SwitchListStepKind.Prep;

    public static bool StepSupportsGo(SwitchListStep? step)
    {
        if (step == null)
        {
            return false;
        }

        if (StepSupportsGo(step.Kind))
        {
            return true;
        }

        return step.Kind == SwitchListStepKind.TurnAround
            && SwitchListDriveFacing.IsDriveToTurntable(step.Label);
    }

    public static SwitchListRunMode EnterModeForStep(SwitchListStep? step) =>
        step != null && StepRequiresHuman(step.Kind)
            ? SwitchListRunMode.HumanHold
            : SwitchListRunMode.Manual;

    public static bool AllowsManualNext(SwitchListRunMode mode) =>
        AllowsManualNext(mode, hasNextStep: true);

    /// <summary>
    /// Next on HumanHold when another row remains (reach later Align).
    /// Last Human row stays Done-only so Next cannot complete the list.
    /// GO still blocks Next.
    /// </summary>
    public static bool AllowsManualNext(SwitchListRunMode mode, bool hasNextStep)
    {
        if (mode == SwitchListRunMode.Go)
        {
            return false;
        }

        if (mode == SwitchListRunMode.Manual)
        {
            return true;
        }

        return mode == SwitchListRunMode.HumanHold && hasNextStep;
    }

    public static bool PidGoActive(SwitchListRunMode mode, SwitchListStep? step) =>
        mode == SwitchListRunMode.Go
        && StepSupportsGo(step);

    public static SwitchListRunnerResult TrySetGo(
        SwitchListStep? step,
        bool hasPlan,
        bool pinForAlign,
        RouteClearancePhase clearancePhase,
        float? derailRiskPercent = null)
    {
        if (step == null)
        {
            return SwitchListRunnerResult.NoActiveStep;
        }

        if (!StepSupportsGo(step))
        {
            return SwitchListRunnerResult.WrongStepKind;
        }

        if (!hasPlan)
        {
            return SwitchListRunnerResult.NeedPlan;
        }

        // CLEARED is the pin-leg *stop* cue (yard chain / Next), not an arm gate.
        // Requiring it here deadlocks Load→GO: you never reach the frog.
        _ = pinForAlign;
        _ = clearancePhase;

        // 13.4: fail-closed Transit arm (same 7.5 intervene threshold as mid-GO soft-stop).
        if (LimitThrottleCap.ShouldIntervene(derailRiskPercent))
        {
            return SwitchListRunnerResult.RefuseDerail;
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
        TryManualNext(mode, hasNextStep: true);

    public static SwitchListRunnerResult TryManualNext(SwitchListRunMode mode, bool hasNextStep) =>
        AllowsManualNext(mode, hasNextStep)
            ? SwitchListRunnerResult.Ok
            : SwitchListRunnerResult.NextBlocked;

    /// <summary>
    /// **13.2.1 / CP4:** 7.4 couple success on a Prep row with a later step → auto Next.
    /// </summary>
    public static bool ShouldAdvanceOnCoupleSuccess(
        SwitchListStepKind? kind,
        SwitchListRunMode mode,
        bool hasNextStep,
        bool coupleSuccess) =>
        coupleSuccess
        && kind == SwitchListStepKind.Prep
        && AllowsManualNext(mode, hasNextStep);
}
