namespace YardMasterSuite.Core;

/// <summary>
/// Manual cruise master switch. Default off. Check on the Maps desk to let
/// PID drive; leave unchecked to sit still with a dest (graph dump).
/// World leave restores off.
/// </summary>
public static class PidCruiseSession
{
    public static bool Enabled { get; private set; }

    public static void SetEnabled(bool enabled) => Enabled = enabled;

    public static void Reset() => Enabled = false;
}
