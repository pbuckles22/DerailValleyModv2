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
        bool cruiseEnabled) =>
        cruiseEnabled && (hasMapsDest || switchListActiveIncomplete) && facingReady;
}
