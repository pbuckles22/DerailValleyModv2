namespace YardMasterSuite.Core;

/// <summary>
/// 6.7 older-save helper. When <see cref="Enabled"/> is true, first world load
/// acquires every obtainable general + job license (including Multiple Unit).
/// Set <see cref="Enabled"/> to <c>false</c> to disable. Grants persist on the save.
/// </summary>
public static class SmokeLicenseGrantGate
{
    /// <summary>
    /// Smoke-only. <c>true</c> grants licenses on world load. Ship default is <c>false</c>.
    /// Flip to <c>true</c> for an older-save MU smoke, then back to <c>false</c> before commit.
    /// </summary>
    public static bool Enabled = false;

    public static string FormatDisabled() => "T2 licenses skip: flag off";

    public static string FormatGranted(int acquired) =>
        "T2 licenses granted: n=" + acquired;

    public static string FormatFail(string reason) =>
        "T2 licenses fail: " + reason;
}
