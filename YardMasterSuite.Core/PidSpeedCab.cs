namespace YardMasterSuite.Core;

/// <summary>
/// Headless cab tick: Three-Gate write intent then DE2 expander. 2.9.1.6
/// Player.log replay uses this, not raw <see cref="PidSpeedCommand"/> into
/// <see cref="PidSpeedPlant"/>.
/// </summary>
public static class PidSpeedCab
{
    public static void Apply(
        in PidSpeedCommand cmd,
        bool wantThrottle,
        ref float throttle,
        ref float independent)
    {
        var nextThr = PidSpeedWrite.Throttle(
            throttle,
            cmd.DesiredThrottle,
            cmd.GearPending,
            cmd.BrakePending,
            wantThrottle)
            ? PidSpeedWrite.Quantize(cmd.DesiredThrottle, throttle)
            : throttle;
        var nextInd = independent;
        if (PidSpeedWrite.Independent(
            independent,
            cmd.DesiredIndependent,
            cmd.GearPending,
            cmd.BrakePending))
        {
            nextInd = cmd.DesiredIndependent + PidSpeedNotch.ExactEpsilon < independent
                ? cmd.DesiredIndependent
                : PidSpeedWrite.Quantize(cmd.DesiredIndependent, independent);
        }

        throttle = PidSpeedNotch.ApplyExpander(nextThr, throttle, firstPunchFromZero: true);
        independent = nextInd + PidSpeedNotch.ExactEpsilon < independent
            ? nextInd
            : PidSpeedNotch.ApplyExpander(nextInd, independent, firstPunchFromZero: false);
    }
}
