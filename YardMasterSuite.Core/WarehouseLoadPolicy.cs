namespace YardMasterSuite.Core;

/// <summary>
/// Cab auto warehouse load/unload after Into-loader stop. Core owns when to
/// start / finish; Unity calls <c>WarehouseMachineController.ActivateExternally</c>.
/// </summary>
public static class WarehouseLoadPolicy
{
    public static bool StepIsLoad(SwitchListStep? step) =>
        step != null && step.Kind == SwitchListStepKind.Load;

    public static bool IsUnload(string? label) =>
        !string.IsNullOrEmpty(label)
        && label!.StartsWith("Unload", System.StringComparison.OrdinalIgnoreCase);

    public static bool OnLoaderTrack(string? destTrackId, string? locoTrackId, bool uniqueTrack)
    {
        if (!uniqueTrack)
        {
            return false;
        }

        var dest = destTrackId?.Trim();
        var loco = locoTrackId?.Trim();
        return !string.IsNullOrEmpty(dest)
            && !string.IsNullOrEmpty(loco)
            && string.Equals(dest, loco, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldStartLoad(
        SwitchListRunMode mode,
        SwitchListStep? step,
        bool onLoaderTrack,
        bool carsReady,
        bool goStopActive,
        float speedKmh,
        float throttle01,
        bool loadActive,
        bool loadLocked,
        bool loadAttempted) =>
        mode == SwitchListRunMode.HumanHold
        && StepIsLoad(step)
        && onLoaderTrack
        && carsReady
        && !goStopActive
        && PidGoStop.ReadyToAdvanceAfterCleared(speedKmh, throttle01)
        && !loadActive
        && !loadLocked
        && !loadAttempted;

    public static bool ShouldFinishLoad(SwitchListStep? step, bool loadLocked) =>
        StepIsLoad(step) && loadLocked;
}

public static class WarehouseLoadSession
{
    public static bool Active { get; private set; }

    public static bool Locked { get; private set; }

    public static bool Attempted { get; private set; }

    private static bool _sawWork;

    public static void Begin()
    {
        Attempted = true;
        Active = true;
        Locked = false;
        _sawWork = false;
    }

    public static void Observe(bool ongoing, bool stillHasWork)
    {
        if (!Attempted)
        {
            return;
        }

        if (ongoing)
        {
            Active = true;
            _sawWork = true;
            Locked = false;
            return;
        }

        if (_sawWork && !stillHasWork)
        {
            Active = false;
            Locked = true;
        }
    }

    public static void Clear()
    {
        Active = false;
        Locked = false;
        Attempted = false;
        _sawWork = false;
    }
}
