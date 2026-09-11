namespace YardMasterSuite.Core;

/// <summary>
/// One kiss(aim): cruise 25 until rem ≤ trigger, then Stop GO.
/// Prep knuckle and TT mid use slack (−2 m / +2.5 m). CLEARED is d_stop only.
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

    /// <summary>Cruise until Stop GO — Prep stays 25 even when the knuckle laser is blind.</summary>
    public static float RequestKmh(SwitchListStep? step, bool inYardPrepScope = true) =>
        AimFor(step, inYardPrepScope) == YardKissAim.None
            ? PidSpeedTarget.DefaultRequestKmh
            : CruiseKmh;

    public static bool InKissZone(
        float? remToAimMeters,
        float speedKmh,
        YardKissAim aim = YardKissAim.None,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes) =>
        YardArrivalStopPolicy.InClearedKissZone(remToAimMeters, speedKmh, aim, massTonnes);

    public static bool ShouldKiss(
        SwitchListRunMode mode,
        float? remToAimMeters,
        float speedKmh,
        YardKissAim aim = YardKissAim.None,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes) =>
        mode == SwitchListRunMode.Go && InKissZone(remToAimMeters, speedKmh, aim, massTonnes);

    public static SwitchListYardChainAction StopAction(YardKissAim aim) =>
        aim switch
        {
            YardKissAim.Cleared => SwitchListYardChainAction.StopGoKissCleared,
            YardKissAim.TurntableMid => SwitchListYardChainAction.StopGoAtTurntable,
            YardKissAim.PrepCars => SwitchListYardChainAction.StopGoKissPrep,
            _ => SwitchListYardChainAction.None,
        };

    public static SwitchListYardChainAction TryKiss(
        SwitchListRunMode mode,
        SwitchListStep? step,
        float? remToAimMeters,
        float speedKmh,
        bool inYardPrepScope = true,
        bool sawAtSwitchThisLeg = true,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes)
    {
        var aim = AimFor(step, inYardPrepScope);
        if (aim == YardKissAim.None
            || !ShouldKiss(mode, remToAimMeters, speedKmh, aim, massTonnes))
        {
            return SwitchListYardChainAction.None;
        }

        // Cab 2.13.2.5.3: rem=0 CLEARED at rest (frog already behind) is not a kiss.
        if (aim == YardKissAim.Cleared && !sawAtSwitchThisLeg)
        {
            return SwitchListYardChainAction.None;
        }

        // Cab 4.8: rem=2 rest is the 2 m pin band, not the knuckle. Couple latch owns ≤1.5 m.
        // Gemini 4.9: InKissZone(2.1, 0) is true via 15 m slack → kiss fights creep (chatter).
        if (aim == YardKissAim.PrepCars && remToAimMeters is float rem)
        {
            if (rem <= BackupProximityDisplay.CoupleNearRangeMeters)
            {
                return SwitchListYardChainAction.None;
            }

            if (rem <= YardArrivalStopPolicy.ClearedKissSlackMeters
                && speedKmh <= PrepCreepPolicy.CreepRequestKmh + 3f)
            {
                return SwitchListYardChainAction.None;
            }
        }

        return StopAction(aim);
    }
}
