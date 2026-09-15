using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class PidCruiseSessionTests
{
    public PidCruiseSessionTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Sit_still_gather_unarmed_when_cruise_unchecked()
    {
        Assert.False(PidCruiseSession.Enabled);
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
    public void Smoke_22_6_cruise_off_load_list_does_not_yard_chain_arm_go()
    {
        var step = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Past switch until CLEARED");
        var steps = new[]
        {
            step,
            new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
        };
        Assert.True(SwitchListYardChain.InYardPrepScope(steps, 0));
        Assert.True(
            SwitchListYardChain.ShouldAutoArmGo(
                SwitchListRunMode.Manual,
                step,
                inYardPrepScope: true,
                pinBlocksAlign: true,
                RouteClearancePhase.Idle,
                cruiseEnabled: true));
        Assert.False(
            SwitchListYardChain.ShouldAutoArmGo(
                SwitchListRunMode.Manual,
                step,
                inYardPrepScope: true,
                pinBlocksAlign: true,
                RouteClearancePhase.Idle,
                cruiseEnabled: false));
        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                step,
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                pinBlocksAlign: true,
                cruiseEnabled: true));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                step,
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                pinBlocksAlign: true,
                cruiseEnabled: false));
    }

    [Fact]
    public void Cruise_defaults_off_and_world_leave_restores_off()
    {
        Assert.False(PidCruiseSession.Enabled);
        PidCruiseSession.SetEnabled(true);
        Assert.True(PidCruiseSession.Enabled);
        YmsRouteSessions.ClearAll();
        Assert.False(PidCruiseSession.Enabled);
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
