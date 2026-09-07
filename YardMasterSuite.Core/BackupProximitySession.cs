namespace YardMasterSuite.Core;

/// <summary>
/// Last HUD proximity sample (6.18) for yard taper — updated every poll, not only chip-key change.
/// </summary>
public static class BackupProximitySession
{
    public static float? ClearanceMeters { get; private set; }

    public static void Observe(float? clearanceMeters)
    {
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

    public static void Clear() => ClearanceMeters = null;
}
