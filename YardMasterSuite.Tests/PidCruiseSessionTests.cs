using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class PidCruiseSessionTests
{
    public PidCruiseSessionTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Sit_still_gather_unarmed_when_cruise_unchecked()
    {
        Assert.True(PidCruiseSession.Enabled);
        Assert.True(PidSpeedArm.IsArmed(
            hasMapsDest: true,
            switchListActiveIncomplete: false,
            facingReady: true,
            cruiseEnabled: true));
        Assert.False(PidSpeedArm.IsArmed(
            hasMapsDest: true,
            switchListActiveIncomplete: false,
            facingReady: true,
            cruiseEnabled: false));
        Assert.False(PidSpeedArm.IsArmed(
            hasMapsDest: false,
            switchListActiveIncomplete: true,
            facingReady: true,
            cruiseEnabled: false));

        var state = default(PidSpeedState);
        var cmd = PidSpeedHold.Tick(
            new PidSpeedInput(
                0.05f,
                0f,
                25f,
                null,
                0.45f,
                0.1f,
                armed: false,
                derailIntervening: false,
                thermalCeiling: 1f,
                reverser: 1f,
                legNeedsReverse: false),
            ref state);
        Assert.False(cmd.Active);
        Assert.Equal(0.45f, cmd.DesiredThrottle);
    }

    [Fact]
    public void Cruise_defaults_on_and_world_leave_restores_on()
    {
        Assert.True(PidCruiseSession.Enabled);
        PidCruiseSession.SetEnabled(false);
        Assert.False(PidCruiseSession.Enabled);
        YmsRouteSessions.ClearAll();
        Assert.True(PidCruiseSession.Enabled);
    }

    [Fact]
    public void Toggle_log_names_on_and_off()
    {
        Assert.Equal("T2 pid: cruise-off", PidSpeedTelemetry.CruiseOff);
        Assert.Equal("T2 pid: cruise-on", PidSpeedTelemetry.CruiseOn);
        Assert.Equal(PidSpeedTelemetry.CruiseOff, PidSpeedTelemetry.FormatCruise(enabled: false));
        Assert.Equal(PidSpeedTelemetry.CruiseOn, PidSpeedTelemetry.FormatCruise(enabled: true));
    }
}
