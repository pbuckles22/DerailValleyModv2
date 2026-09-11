namespace YardMasterSuite.Core;

/// <summary>
/// Soft Stop-GO for yard arrival aims. Prep/TT use d_stop + slack − bias so the
/// knuckle/mid lands short; CLEARED is d_stop only (cab 2.13.2.5.17 rem=39→5).
/// </summary>
public static class YardArrivalStopPolicy
{
    public const float DefaultAimToleranceMeters = 2f;

    public static bool ShouldStopGo(
        SwitchListRunMode mode,
        float? remToAimMeters,
        float speedKmh,
        float aimToleranceMeters)
    {
        if (mode != SwitchListRunMode.Go || remToAimMeters is not float rem)
        {
            return false;
        }

        return YardStopKinematics.ShouldStartStop(rem, speedKmh, aimToleranceMeters);
    }

    /// <summary>
    /// Extra meters so a 25 km/h Prep/TT kiss starts Stop GO before rem = d_stop.
    /// CLEARED does not add this (cab 2.13.2.5.17 rem=39→5).
    /// </summary>
    public const float ClearedKissSlackMeters = 15f;

    /// <summary>
    /// Cab 4.8–4.10: kiss at d_stop+slack rests ~2 m short of the knuckle (pin band).
    /// Fire that much later so one Stop GO lands in couple scan.
    /// </summary>
    public const float KissLandingBiasMeters = 2f;

    /// <summary>
    /// Cab 4.13: Prep-biased 25-kiss rested <c>along=21</c> vs consist-mid 18.5.
    /// Fire this much earlier on TT so the same brake lands table-mid.
    /// </summary>
    public const float TurntableMidLeadMeters = 2.5f;

    /// <summary>
    /// rem where cruise 25 should Stop GO. CLEARED = d_stop (tail must reach the
    /// frog). Prep/None = d_stop + slack − bias. TT mid adds lead.
    /// </summary>
    public static float KissTriggerRemMeters(
        float speedKmh,
        YardKissAim aim = YardKissAim.None,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes)
    {
        var dStop = YardStopKinematics.StoppingDistanceMeters(speedKmh, massTonnes);
        if (float.IsInfinity(dStop) || float.IsNaN(dStop))
        {
            return float.PositiveInfinity;
        }

        if (aim == YardKissAim.Cleared)
        {
            return dStop < 0f ? 0f : dStop;
        }

        var trigger = dStop + ClearedKissSlackMeters - KissLandingBiasMeters;
        if (aim == YardKissAim.TurntableMid)
        {
            trigger += TurntableMidLeadMeters;
        }

        return trigger < 0f ? 0f : trigger;
    }

    public static bool InClearedKissZone(
        float? remToClearedMeters,
        float speedKmh,
        YardKissAim aim = YardKissAim.None,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes)
    {
        if (remToClearedMeters is not float rem
            || float.IsNaN(rem)
            || float.IsInfinity(rem))
        {
            return false;
        }

        if (rem <= DefaultAimToleranceMeters)
        {
            return true;
        }

        var trigger = KissTriggerRemMeters(speedKmh, aim, massTonnes);
        return !float.IsInfinity(trigger) && rem <= trigger;
    }

    public static bool ShouldKissCleared(
        SwitchListRunMode mode,
        float? remToClearedMeters,
        float speedKmh,
        float massTonnes = YardStopKinematics.ReferenceMassTonnes) =>
        mode == SwitchListRunMode.Go
            && InClearedKissZone(
                remToClearedMeters,
                speedKmh,
                YardKissAim.Cleared,
                massTonnes);
}
