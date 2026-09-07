namespace YardMasterSuite.Core;

/// <summary>
/// Soft Stop-GO for any yard arrival aim (cars, TT mid, CLEARED) — same rem ≤ d_stop
/// recipe as Prep creep-to-couple.
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
    /// Extra meters so a 25 km/h pin approach starts Stop GO before rem = d_stop
    /// (cab 2.13.2.4.5: stop at rem=12 still doing 26 → CLEARED while rolling).
    /// </summary>
    public const float ClearedKissSlackMeters = 15f;

    public static bool InClearedKissZone(float? remToClearedMeters, float speedKmh)
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

        var dStop = YardStopKinematics.StoppingDistanceMeters(speedKmh);
        return !float.IsInfinity(dStop) && rem <= dStop + ClearedKissSlackMeters;
    }

    public static bool ShouldKissCleared(
        SwitchListRunMode mode,
        float? remToClearedMeters,
        float speedKmh) =>
        mode == SwitchListRunMode.Go && InClearedKissZone(remToClearedMeters, speedKmh);
}
