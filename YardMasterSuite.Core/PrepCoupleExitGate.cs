namespace YardMasterSuite.Core;

/// <summary>
/// Prep reverse: creep 3 km/h until the couple window, then Stop GO.
/// List Next / facing / ArmGo wait on <see cref="ReadyToNext"/> so TMS
/// does not see a reverser flip under load (dropzone 22.43 Issue 1).
/// </summary>
public static class PrepCoupleExitGate
{
    /// <summary>
    /// Auto-couple write window (green ≤0.5 m), not the 1.5 m vanilla scan
    /// and not the 25 km/h kiss envelope.
    /// </summary>
    public static bool InTouchWindow(float? clearanceMeters) =>
        AutoCoupleAssist.ClearanceAllowsCouple(clearanceMeters);

    public static bool KnuckleMade(
        bool spurPickupComplete,
        bool tipCoupled,
        int unattachedOnPrepSpur) =>
        unattachedOnPrepSpur <= 0 && (spurPickupComplete || tipCoupled);

    public static bool ConsistAtRest(float speedKmh) =>
        PidGoStop.IsFullyStopped(speedKmh);

    public static bool MotorsSafe(MotorStatus? motors) =>
        MotorDisplay.AllowsGoWrites(motors);

    public static bool LeversIdle(float throttle01) =>
        PidGoStop.IsThrottleIdle(throttle01);

    /// <summary>
    /// Leave Prep: knuckle + rest + idle throttle + motors OK.
    /// Facing/ArmGo use the same rest check via
    /// <see cref="PidGoStop.ReadyToAdvanceAfterCleared"/>.
    /// </summary>
    public static bool ReadyToNext(
        SwitchListStepKind? kind,
        bool hasNextStep,
        bool coupleSuccess,
        bool spurPickupComplete,
        float speedKmh,
        float throttle01,
        MotorStatus? motors,
        bool tipCoupled = false,
        int unattachedOnPrepSpur = 0) =>
        coupleSuccess
        && hasNextStep
        && kind == SwitchListStepKind.Prep
        && KnuckleMade(spurPickupComplete, tipCoupled, unattachedOnPrepSpur)
        && ConsistAtRest(speedKmh)
        && LeversIdle(throttle01)
        && MotorsSafe(motors);

    public static bool ShouldStopGoOnPrepApproach(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool mechanicallyCoupled,
        bool spurPickupComplete,
        int unattachedOnPrepSpur,
        float? clearanceMeters)
    {
        if (mode != SwitchListRunMode.Go
            || (step != null && step.Kind != SwitchListStepKind.Prep))
        {
            return false;
        }

        if (KnuckleMade(spurPickupComplete, mechanicallyCoupled, unattachedOnPrepSpur))
        {
            return true;
        }

        return InTouchWindow(clearanceMeters);
    }

    public static bool ShouldStopGoAfterKnuckle(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool mechanicallyCoupled,
        bool spurPickupComplete,
        int unattachedOnPrepSpur = 0) =>
        ShouldStopGoOnPrepApproach(
            mode,
            step,
            mechanicallyCoupled,
            spurPickupComplete,
            unattachedOnPrepSpur,
            clearanceMeters: null);
}
