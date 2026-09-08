namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.4</b> multi-leg yard chain through Prep (Switch List steps 1–5).
/// Auto-arm GO on drive legs in yard scope; CLEARED completes pin legs → Next;
/// stop on TT rail (spin human); stop at Prep spur (couple human). Haul → Epic <b>15</b>.
/// </summary>
public enum SwitchListYardChainAction
{
    None = 0,
    ArmGo = 1,
    StopGoCompleteCleared = 2,
    StopGoAtPrepSpur = 3,
    StopGoAtTurntable = 4,
    /// <summary><b>13.2.4</b> green / mechanical couple — Stop GO before shove.</summary>
    StopGoAtCouple = 5,
    /// <summary>Brake to kiss CLEARED — Stop GO only; Next waits for phase Cleared.</summary>
    StopGoKissCleared = 6,
    /// <summary>Brake from 25 toward the car — Stop GO only; last meters creep, then couple-hold.</summary>
    StopGoKissPrep = 7,
}

public static class SwitchListYardChain
{
    public static int LastPrepIndex(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps)
    {
        if (steps == null || steps.Count == 0)
        {
            return -1;
        }

        var last = -1;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Kind == SwitchListStepKind.Prep)
            {
                last = i;
            }
        }

        return last;
    }

    /// <summary>True while current index is at or before the last Prep row.</summary>
    public static bool InYardPrepScope(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex)
    {
        var lastPrep = LastPrepIndex(steps);
        return lastPrep >= 0
            && currentIndex >= 0
            && currentIndex <= lastPrep;
    }

    public static bool StepSupportsYardGo(SwitchListStep? step) =>
        SwitchListRunner.StepSupportsGo(step);

    public static bool ShouldAutoArmGo(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool inYardPrepScope,
        bool pinBlocksAlign,
        RouteClearancePhase phase,
        bool goStopActive = false,
        bool onTurntable = false,
        bool prepCoupleHold = false)
    {
        if (goStopActive
            || mode != SwitchListRunMode.Manual
            || !inYardPrepScope
            || !StepSupportsYardGo(step)
            || prepCoupleHold)
        {
            return false;
        }

        // Already on TT for drive-to-TT — wait for spin / Next; do not re-arm.
        if (onTurntable && TurntableArrivalGate.StepWantsArrival(step))
        {
            return false;
        }

        // Pin leg already CLEARED — wait for Next; do not re-arm the finished approach.
        if (step != null
            && SwitchListRunner.StepNeedsPinClearance(step.Kind)
            && phase == RouteClearancePhase.Cleared)
        {
            return false;
        }

        _ = pinBlocksAlign;
        return true;
    }

    public static bool ShouldCompleteOnCleared(
        SwitchListRunMode mode,
        SwitchListStep? step,
        RouteClearancePhase phase,
        bool goStopActive = false) =>
        !goStopActive
        && (mode == SwitchListRunMode.Go || mode == SwitchListRunMode.Manual)
        && step != null
        && SwitchListRunner.StepNeedsPinClearance(step.Kind)
        && phase == RouteClearancePhase.Cleared;

    public static bool ShouldStopGoAtPrepSpur(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool prepAtSpur) =>
        mode == SwitchListRunMode.Go
        && step != null
        && step.Kind == SwitchListStepKind.Prep
        && prepAtSpur;

    public static bool ShouldStopGoAtTurntable(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool onTurntable) =>
        mode == SwitchListRunMode.Go
        && onTurntable
        && TurntableArrivalGate.StepWantsArrival(step);

    /// <summary>
    /// After CLEARED complete: Next only when the next row is still yard/Prep scope
    /// (do not auto-advance onto haul Transit).
    /// </summary>
    public static bool ShouldAutoNextAfterCleared(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        bool hasNextStep) =>
        hasNextStep && InYardPrepScope(steps, currentIndex + 1);

    public static SwitchListYardChainAction Evaluate(
        SwitchListRunMode mode,
        SwitchListStep? step,
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        RouteClearancePhase phase,
        bool prepAtSpur,
        bool hasPlan,
        bool pinBlocksAlign = false,
        bool goStopActive = false,
        bool onTurntable = false,
        bool prepCoupleStop = false,
        bool prepCoupleHold = false,
        float? remToAimMeters = null,
        float speedKmh = 0f)
    {
        var inYard = InYardPrepScope(steps, currentIndex);
        // prepCoupleStop = session latch (rem≤d_stop / mech) from tip sample.
        if (prepCoupleStop
            && mode == SwitchListRunMode.Go
            && step != null
            && step.Kind == SwitchListStepKind.Prep)
        {
            return SwitchListYardChainAction.StopGoAtCouple;
        }

        var predictive = YardKissPolicy.TryKiss(
            mode,
            step,
            remToAimMeters,
            speedKmh,
            inYard);
        if (predictive != SwitchListYardChainAction.None)
        {
            return predictive;
        }

        _ = prepAtSpur;
        // Prep aim is the car (HUD kiss / couple latch), not the spur-end pad.
        // StopGoAtPrepSpur at 25 held short of the knuckle (TT mid stays special).

        if (ShouldStopGoAtTurntable(mode, step, onTurntable))
        {
            return SwitchListYardChainAction.StopGoAtTurntable;
        }

        if (ShouldCompleteOnCleared(mode, step, phase, goStopActive))
        {
            return SwitchListYardChainAction.StopGoCompleteCleared;
        }

        if (hasPlan
            && ShouldAutoArmGo(
                mode,
                step,
                inYard,
                pinBlocksAlign,
                phase,
                goStopActive,
                onTurntable,
                prepCoupleHold))
        {
            // Kiss zone: sit. CLEARED uses cruise rem so a 25-envelope stop does not re-arm.
            // Prep uses actual speed so rem=kiss-trigger at 0 km/h can continue, but leftover
            // ~2 m (cab 4.8) sits — no second creep GO.
            var aim = YardKissPolicy.AimFor(step, inYard);
            if (aim == YardKissAim.Cleared
                && YardArrivalStopPolicy.InClearedKissZone(
                    remToAimMeters,
                    PidSpeedTarget.DefaultRequestKmh))
            {
                return SwitchListYardChainAction.None;
            }

            if (aim == YardKissAim.PrepCars
                && YardArrivalStopPolicy.InClearedKissZone(remToAimMeters, speedKmh))
            {
                return SwitchListYardChainAction.None;
            }

            return SwitchListYardChainAction.ArmGo;
        }

        return SwitchListYardChainAction.None;
    }
}
