namespace YardMasterSuite.Core;

/// <summary>Live consist length (InterCouplerDistance sum) for TT consist-mid rem.</summary>
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

    public static void Clear() => Meters = 0f;
}
