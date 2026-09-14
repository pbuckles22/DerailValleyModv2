namespace YardMasterSuite.Core;

/// <summary>
/// <b>9.1</b> arms only on an active Maps dest or an incomplete Switch List,
/// and only after facing is known (plan / latch / list). Desk <c>Cruise</c>
/// (default on) is a manual master switch so gather can sit still. GO/Human is <b>13.1</b>.
/// </summary>
public static class PidSpeedArm
{
    public static bool IsArmed(
        bool hasMapsDest,
        bool switchListActiveIncomplete,
        bool facingReady) =>
        IsArmed(hasMapsDest, switchListActiveIncomplete, facingReady, cruiseEnabled: true);

    public static bool IsArmed(
        bool hasMapsDest,
        bool switchListActiveIncomplete,
        bool facingReady,
        bool cruiseEnabled)
    {
        if (!facingReady)
        {
            return false;
        }

        // Switch List manual / Human legs — player drives; GO arms via the 5-arg overload.
        if (switchListActiveIncomplete)
        {
            return false;
        }

        return cruiseEnabled && hasMapsDest;
    }

    /// <summary>
    /// <b>13.1</b> GO arms PID only when Cruise is on. Cruise off = player
    /// drives; Next/GO still Align, set reverser, and spin TT.
    /// </summary>
    public static bool IsArmed(
        bool goActive,
        bool hasMapsDest,
        bool switchListActiveIncomplete,
        bool facingReady,
        bool cruiseEnabled)
    {
        if (!facingReady)
        {
            return false;
        }

        if (switchListActiveIncomplete)
        {
            return goActive && cruiseEnabled;
        }

        return cruiseEnabled && hasMapsDest;
    }
}
