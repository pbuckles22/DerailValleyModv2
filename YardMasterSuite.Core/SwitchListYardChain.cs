namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.4</b> multi-leg yard chain through Prep (Switch List steps 1–5).
/// Auto-arm GO on drive legs in yard scope; CLEARED completes pin legs → Next;
/// kiss consist-center on TT then auto-spin; stop at Prep spur. Haul → Epic <b>15</b>.
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
    /// <summary>Drive-to-TT Stop GO done — Next onto TT turn around.</summary>
    AdvanceToTtSpin = 8,
    /// <summary>On spin row: start 180° table rotate.</summary>
    StartTtSpin = 9,
    /// <summary>Table locked at opposite snap — Next onto leave / Prep.</summary>
    SpinDoneNext = 10,
    /// <summary>On Load HumanHold: start warehouse machine.</summary>
    StartWarehouseLoad = 11,
    /// <summary>Warehouse machine idle after cargo — Next onto leave.</summary>
    WarehouseLoadDone = 12,
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
        bool prepCoupleHold = false,
        bool sawAtSwitchThisLeg = true,
        bool stillOnPreviousPrepSpur = false,
        bool cruiseEnabled = true,
        bool motorsHealthy = true,
        bool prepTipCoupled = false)
    {
        // Cab 22.6: Cruise unchecked + Load Switch List still ArmGo'd.
        if (!cruiseEnabled
            || !motorsHealthy
            || goStopActive
            || mode != SwitchListRunMode.Manual
            || !inYardPrepScope
            || !StepSupportsYardGo(step)
            || prepCoupleHold
            || prepTipCoupled)
        {
            return false;
        }

        // Already on TT for drive-to-TT — wait for spin / Next; do not re-arm.
        if (onTurntable && TurntableArrivalGate.StepWantsArrival(step))
        {
            return false;
        }

        // Pin leg finished only after At switch then CLEARED. Already-CLEARED
        // at rest (frog behind in the spur) still needs a pull-out GO.
        if (step != null
            && SwitchListRunner.StepNeedsPinClearance(step.Kind)
            && phase == RouteClearancePhase.Cleared
            && sawAtSwitchThisLeg
            && !stillOnPreviousPrepSpur)
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
        bool goStopActive = false,
        bool sawAtSwitchThisLeg = true,
        bool stillOnPreviousPrepSpur = false,
        float speedKmh = 0f,
        float throttle01 = 0f) =>
        !stillOnPreviousPrepSpur
        && !goStopActive
        && PidGoStop.ReadyToAdvanceAfterCleared(speedKmh, throttle01)
        && (mode == SwitchListRunMode.Go || mode == SwitchListRunMode.Manual)
        && step != null
        && SwitchListRunner.StepNeedsPinClearance(step.Kind)
        && phase == RouteClearancePhase.Cleared
        && sawAtSwitchThisLeg;

    /// <summary>
    /// After Prep couple-hold, GO means pull-out (advance off Prep), not re-arm
    /// the same kiss row (cab 2.13.2.5.3: GO → instant stop-couple).
    /// </summary>
    public static bool ShouldGoAdvanceAfterCoupleHold(
        SwitchListStep? step,
        bool holdAfterCouple,
        bool hasNext) =>
        holdAfterCouple
        && hasNext
        && step != null
        && step.Kind == SwitchListStepKind.Prep;

    /// <summary>
    /// Cab 22.37: after couple-Next onto pull-out, drop hold + Stop GO only
    /// when rest, idle, and reverser already match the new row.
    /// </summary>
    public static bool ShouldClearCoupleHoldAndResumeGo(
        SwitchListStep? step,
        bool holdAfterCouple,
        bool fullyStoppedAndIdle,
        bool reverserMatches,
        bool motorsHealthy = true,
        bool handbrakesReleased = true) =>
        motorsHealthy
        && handbrakesReleased
        && holdAfterCouple
        && fullyStoppedAndIdle
        && reverserMatches
        && step != null
        && step.Kind != SwitchListStepKind.Prep;

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
    /// After CLEARED complete: Next through yard/Prep and onto haul Transit.
    /// Hold on Delivery — player turns in the booklet for pay.
    /// Cab 2.13.2.5.22.21: last Past-switch (pin 8) auto-Next onto Transit 9.
    /// </summary>
    public static bool ShouldAutoNextAfterCleared(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        bool hasNextStep,
        bool stillOnPreviousPrepSpur = false)
    {
        if (stillOnPreviousPrepSpur || !hasNextStep || steps == null)
        {
            return false;
        }

        var nextIndex = currentIndex + 1;
        if (nextIndex < 0 || nextIndex >= steps.Count)
        {
            return false;
        }

        if (InYardPrepScope(steps, nextIndex))
        {
            return true;
        }

        return steps[nextIndex].Kind != SwitchListStepKind.Delivery;
    }

    /// <summary>
    /// Cab 2.13.2.5.20: after first Prep couple, Past-switch CLEARED on the
    /// throat frog while the loco is still on that Prep spur. Hold Next.
    /// </summary>
    public static bool StillOnPreviousPrepSpur(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? locoTrackId)
    {
        if (steps == null || currentIndex < 1 || currentIndex >= steps.Count)
        {
            return false;
        }

        var current = steps[currentIndex];
        if (!SwitchListRunner.StepNeedsPinClearance(current.Kind))
        {
            return false;
        }

        var prev = steps[currentIndex - 1];
        if (prev.Kind != SwitchListStepKind.Prep)
        {
            return false;
        }

        var spur = prev.DestTrackId?.Trim();
        var loco = locoTrackId?.Trim();
        return !string.IsNullOrEmpty(spur)
            && !string.IsNullOrEmpty(loco)
            && string.Equals(spur, loco, System.StringComparison.OrdinalIgnoreCase);
    }

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
        float speedKmh = 0f,
        bool ttSpinActive = false,
        bool ttSpinLocked = false,
        bool uniqueOnDest = true,
        bool sawAtSwitchThisLeg = true,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes,
        bool stillOnPreviousPrepSpur = false,
        bool cruiseEnabled = true,
        float throttle01 = 0f,
        bool onLoaderTrack = false,
        bool loaderCarsReady = false,
        bool warehouseLoadActive = false,
        bool warehouseLoadLocked = false,
        bool warehouseLoadAttempted = false,
        bool motorsHealthy = true,
        bool prepTipCoupled = false)
    {
        var inYard = InYardPrepScope(steps, currentIndex);
        var holdThroatCleared = stillOnPreviousPrepSpur
            && step != null
            && SwitchListRunner.StepNeedsPinClearance(step.Kind);
        // prepCoupleStop = session latch (rem≤d_stop / mech) from tip sample.
        if (prepCoupleStop
            && step != null
            && step.Kind == SwitchListStepKind.Prep)
        {
            if (mode == SwitchListRunMode.Go)
            {
                return SwitchListYardChainAction.StopGoAtCouple;
            }

            return SwitchListYardChainAction.None;
        }

        if (!holdThroatCleared)
        {
            var predictive = YardKissPolicy.TryKiss(
                mode,
                step,
                remToAimMeters,
                speedKmh,
                inYard,
                sawAtSwitchThisLeg,
                massTonnes);
            if (predictive != SwitchListYardChainAction.None)
            {
                return predictive;
            }
        }

        _ = prepAtSpur;
        // Prep aim is the car (HUD kiss / couple latch), not the spur-end pad.
        // StopGoAtPrepSpur at 25 held short of the knuckle (TT mid stays special).

        if (ShouldStopGoAtTurntable(mode, step, onTurntable))
        {
            return SwitchListYardChainAction.StopGoAtTurntable;
        }

        SwitchListStep? next = null;
        if (steps != null && currentIndex >= 0 && currentIndex + 1 < steps.Count)
        {
            next = steps[currentIndex + 1];
        }

        if (TurntableSpinPolicy.ShouldAdvanceToSpin(
            mode,
            step,
            next,
            onTurntable,
            goStopActive,
            speedKmh,
            uniqueOnDest))
        {
            return SwitchListYardChainAction.AdvanceToTtSpin;
        }

        if (TurntableSpinPolicy.ShouldFinishSpin(step, onTurntable, ttSpinLocked, uniqueOnDest))
        {
            return SwitchListYardChainAction.SpinDoneNext;
        }

        if (TurntableSpinPolicy.ShouldStartSpin(
            mode,
            step,
            onTurntable,
            ttSpinActive,
            ttSpinLocked,
            uniqueOnDest))
        {
            return SwitchListYardChainAction.StartTtSpin;
        }

        if (WarehouseLoadPolicy.ShouldFinishLoad(step, warehouseLoadLocked))
        {
            return SwitchListYardChainAction.WarehouseLoadDone;
        }

        if (WarehouseLoadPolicy.ShouldStartLoad(
            mode,
            step,
            onLoaderTrack,
            loaderCarsReady,
            goStopActive,
            speedKmh,
            throttle01,
            warehouseLoadActive,
            warehouseLoadLocked,
            warehouseLoadAttempted))
        {
            return SwitchListYardChainAction.StartWarehouseLoad;
        }

        if (step != null
            && SwitchListRunner.StepNeedsPinClearance(step.Kind)
            && phase == RouteClearancePhase.Cleared
            && sawAtSwitchThisLeg
            && !holdThroatCleared
            && !goStopActive
            && !PidGoStop.ReadyToAdvanceAfterCleared(speedKmh, throttle01))
        {
            return SwitchListYardChainAction.StopGoKissCleared;
        }

        if (ShouldCompleteOnCleared(
            mode,
            step,
            phase,
            goStopActive,
            sawAtSwitchThisLeg,
            holdThroatCleared,
            speedKmh,
            throttle01))
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
                prepCoupleHold,
                sawAtSwitchThisLeg,
                holdThroatCleared,
                cruiseEnabled,
                motorsHealthy,
                prepTipCoupled))
        {
            // Kiss zone: sit. CLEARED uses cruise rem so a 25-envelope stop does not re-arm.
            // Prep uses actual speed so rem=kiss-trigger at 0 km/h can continue, but leftover
            // ~2 m (cab 4.8) sits — no second creep GO.
            var aim = YardKissPolicy.AimFor(step, inYard);
            if (!holdThroatCleared
                && aim == YardKissAim.Cleared
                && sawAtSwitchThisLeg
                && YardArrivalStopPolicy.InClearedKissZone(
                    remToAimMeters,
                    PidSpeedTarget.DefaultRequestKmh,
                    YardKissAim.Cleared,
                    massTonnes))
            {
                return SwitchListYardChainAction.None;
            }

            // Leave-TT dest-side CLEARED with rem still to Prep (cab 2.13.2.5.8
            // 315 m Forward). Approach Idle still ArmGo. Pull-out rem=0 ArmGo.
            if (aim == YardKissAim.Cleared
                && phase == RouteClearancePhase.Cleared
                && !sawAtSwitchThisLeg
                && remToAimMeters is float remAway
                && !YardArrivalStopPolicy.InClearedKissZone(
                    remAway,
                    speedKmh,
                    YardKissAim.Cleared,
                    massTonnes))
            {
                return SwitchListYardChainAction.None;
            }

            if (aim == YardKissAim.PrepCars)
            {
                if (PrepCreepPolicy.ShouldArmCreepInSafetyZone(
                    remToAimMeters,
                    speedKmh,
                    prepTipCoupled,
                    prepCoupleHold))
                {
                    return SwitchListYardChainAction.ArmGo;
                }

                if (YardArrivalStopPolicy.InClearedKissZone(
                    remToAimMeters,
                    speedKmh,
                    YardKissAim.PrepCars,
                    massTonnes))
                {
                    return SwitchListYardChainAction.None;
                }

                // Cab 22.53: kiss-prep dump then 0 km/h rem~25 is outside the
                // 0-speed envelope and outside the 10 m creep band → used to
                // fall through to 25 km/h ArmGo.
                var kissTrigger = YardArrivalStopPolicy.KissTriggerRemMeters(
                    PidSpeedTarget.DefaultRequestKmh,
                    YardKissAim.PrepCars,
                    massTonnes);
                if (remToAimMeters is float remSit && remSit <= kissTrigger)
                {
                    return SwitchListYardChainAction.None;
                }
            }

            return SwitchListYardChainAction.ArmGo;
        }

        return SwitchListYardChainAction.None;
    }
}
