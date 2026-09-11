namespace YardMasterSuite.Core;

/// <summary>Live consist occupancy (max coupler/bounds) for CLEARED rem and TT mid.</summary>
public static class ConsistLengthSession
{
    public static float Meters { get; private set; }

    public static void Observe(float meters)
    {
        if (float.IsNaN(meters) || float.IsInfinity(meters) || meters <= 0f)
        {
            return;
        }

        Meters = meters;
    }

    /// <summary>
    /// Couple / trainset growth. Logs once when length extends so Past-switch
    /// rem-to-CLEARED can shift in the same GO (cab 2.13.2.5.14).
    /// </summary>
    public static string? ObserveIncrease(float meters)
    {
        var prev = Meters;
        Observe(meters);
        if (Meters <= prev + 0.5f)
        {
            return null;
        }

        return "T2 route-pin: consist-clear len=" + ((int)(Meters + 0.5f)).ToString();
    }

    public static void Clear() => Meters = 0f;
}
