namespace YardMasterSuite.Core;

/// <summary>
/// Cab 2.13.2.5.22.21: PID / gauges follow the consist you stand on,
/// not the look-at car (last-car shove).
/// </summary>
public static class UsableLocoPolicy
{
    public static bool PreferStandingConsist(bool standingOnCar) => standingOnCar;
}
