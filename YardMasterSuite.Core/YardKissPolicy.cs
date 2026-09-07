namespace YardMasterSuite.Core;

/// <summary>
/// One kiss(aim): cruise 25 until rem ≤ d_stop+slack, then Stop GO.
/// Cab 2.13.2.4.6: CLEARED kiss PASS; TT/Prep after that stayed blind-12 / 18-10-5 taper.
/// </summary>
public enum YardKissAim
{
    None = 0,
    Cleared = 1,
    TurntableMid = 2,
    PrepCars = 3,
}

public static class YardKissPolicy
{
    public const float CruiseKmh = 25f;

    public static YardKissAim AimFor(SwitchListStep? step, bool inYardPrepScope = true)
    {
        if (step == null)
        {
            return YardKissAim.None;
        }

        if (step.Kind == SwitchListStepKind.Prep
            || step.Kind == SwitchListStepKind.ReverseInto)
        {
            return YardKissAim.PrepCars;
        }

        if (SwitchListDriveFacing.IsDriveToTurntable(step.Label))
        {
            return YardKissAim.TurntableMid;
        }

        if (inYardPrepScope && SwitchListRunner.StepNeedsPinClearance(step.Kind))
        {
            return YardKissAim.Cleared;
        }

        return YardKissAim.None;
    }

    /// <summary>Cruise until Stop GO — blind rem is 25, not fail-closed 12.</summary>
    public static float RequestKmh(SwitchListStep? step, bool inYardPrepScope = true) =>
        AimFor(step, inYardPrepScope) == YardKissAim.None
            ? PidSpeedTarget.DefaultRequestKmh
            : CruiseKmh;

    public static bool InKissZone(float? remToAimMeters, float speedKmh) =>
        YardArrivalStopPolicy.InClearedKissZone(remToAimMeters, speedKmh);

    public static bool ShouldKiss(
        SwitchListRunMode mode,
        float? remToAimMeters,
        float speedKmh) =>
        mode == SwitchListRunMode.Go && InKissZone(remToAimMeters, speedKmh);

    public static SwitchListYardChainAction StopAction(YardKissAim aim) =>
        aim switch
        {
            YardKissAim.Cleared => SwitchListYardChainAction.StopGoKissCleared,
            YardKissAim.TurntableMid => SwitchListYardChainAction.StopGoAtTurntable,
            YardKissAim.PrepCars => SwitchListYardChainAction.StopGoAtCouple,
            _ => SwitchListYardChainAction.None,
        };

    public static SwitchListYardChainAction TryKiss(
        SwitchListRunMode mode,
        SwitchListStep? step,
        float? remToAimMeters,
        float speedKmh,
        bool inYardPrepScope = true)
    {
        var aim = AimFor(step, inYardPrepScope);
        if (aim == YardKissAim.None || !ShouldKiss(mode, remToAimMeters, speedKmh))
        {
            return SwitchListYardChainAction.None;
        }

        return StopAction(aim);
    }
}
