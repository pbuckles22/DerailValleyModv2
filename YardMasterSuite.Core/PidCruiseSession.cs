namespace YardMasterSuite.Core;

/// <summary>
/// Manual cruise master switch. Default off while Switch List drive is
/// cab-tested (player drives; Align / reverser / TT still run on Next once
/// stopped). Check Cruise on the Maps desk to arm PID. World leave restores off.
/// </summary>
public static class PidCruiseSession
{
    public static bool Enabled { get; private set; }

    public static void SetEnabled(bool enabled) => Enabled = enabled;

    public static void Reset() => Enabled = false;
}
