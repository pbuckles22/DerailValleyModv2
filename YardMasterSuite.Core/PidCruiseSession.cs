namespace YardMasterSuite.Core;

/// <summary>
/// Manual cruise master switch. Default on. Uncheck on the Maps desk to sit
/// still with a dest (graph dump) without PID takeoff. World leave restores on.
/// </summary>
public static class PidCruiseSession
{
    public static bool Enabled { get; private set; } = true;

    public static void SetEnabled(bool enabled) => Enabled = enabled;

    public static void Reset() => Enabled = true;
}
