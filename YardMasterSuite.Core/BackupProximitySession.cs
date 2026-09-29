namespace YardMasterSuite.Core;

/// <summary>
/// Last HUD proximity sample (6.18) for yard taper — updated every poll, not only chip-key change.
/// </summary>
public static class BackupProximitySession
{
    public static float? ClearanceMeters { get; private set; }

    /// <summary>Approach knuckle is coupled to a car outside this consist.</summary>
    public static bool TipCoupled { get; private set; }

    public static void Observe(float? clearanceMeters, bool tipCoupled = false)
    {
        TipCoupled = tipCoupled;
        if (clearanceMeters is float m
            && !float.IsNaN(m)
            && !float.IsInfinity(m)
            && m >= 0f)
        {
            ClearanceMeters = m;
            return;
        }

        ClearanceMeters = null;
    }

    public static void Clear()
    {
        ClearanceMeters = null;
        TipCoupled = false;
    }
}
