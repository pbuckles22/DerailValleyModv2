namespace YardMasterSuite.Core;

/// <summary>
/// Auto 180° TT spin after consist-center kiss. Core owns when to start / finish;
/// Unity writes yaw via <c>TurntableRailTrack.RotateToTargetRotation</c>.
/// </summary>
public static class TurntableSpinPolicy
{
    public const float TargetDeltaDegrees = 180f;
    public const float LockedAbsDegrees = 0.5f;
    public const float MaxDegreesPerSecond = 12f;

    public static bool StepIsSpin(SwitchListStep? step) =>
        step != null
        && step.Kind == SwitchListStepKind.TurnAround
        && step.Label != null
        && step.Label.IndexOf(
            SwitchListDriveFacing.TurnAroundOnTurntable,
            System.StringComparison.Ordinal) >= 0
        && !SwitchListDriveFacing.IsDriveToTurntable(step.Label);

    public static bool ShouldAdvanceToSpin(
        SwitchListRunMode mode,
        SwitchListStep? current,
        SwitchListStep? next,
        bool onTurntable,
        bool goStopActive,
        float speedKmh,
        bool uniqueOnDest = true) =>
        !goStopActive
        && onTurntable
        && uniqueOnDest
        && mode != SwitchListRunMode.Go
        && TurntableArrivalGate.StepWantsArrival(current)
        && StepIsSpin(next)
        && PidGoStop.IsStopped(speedKmh);

    public static bool ShouldStartSpin(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool onTurntable,
        bool spinActive,
        bool spinLocked,
        bool uniqueOnDest = true) =>
        mode != SwitchListRunMode.Go
        && onTurntable
        && uniqueOnDest
        && StepIsSpin(step)
        && !spinActive
        && !spinLocked;

    public static bool ShouldFinishSpin(
        SwitchListStep? step,
        bool onTurntable,
        bool spinLocked,
        bool uniqueOnDest = true) =>
        onTurntable && uniqueOnDest && spinLocked && StepIsSpin(step);

    public static float AngleRange0To360(float degrees)
    {
        var x = degrees % 360f;
        if (x < 0f)
        {
            x += 360f;
        }

        return x;
    }

    public static float AngleRangeNeg180To180(float degrees)
    {
        var x = AngleRange0To360(degrees);
        return x > 180f ? x - 360f : x;
    }

    public static float OppositeYaw(float currentYaw) =>
        AngleRange0To360(currentYaw + TargetDeltaDegrees);

    public static float DeltaToTarget(float currentYaw, float targetYaw) =>
        AngleRangeNeg180To180(targetYaw - currentYaw);

    public static bool IsLocked(float absDeltaDegrees) =>
        absDeltaDegrees >= 0f && absDeltaDegrees <= LockedAbsDegrees;
}

public static class TurntableSpinSession
{
    public static bool Active { get; private set; }

    public static bool Locked { get; private set; }

    public static void Begin()
    {
        Active = true;
        Locked = false;
    }

    public static void ObserveLocked(bool locked)
    {
        if (locked)
        {
            Active = true;
            Locked = true;
            return;
        }

        Locked = false;
    }

    public static void Clear()
    {
        Active = false;
        Locked = false;
    }
}
