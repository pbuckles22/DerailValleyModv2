using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class SwitchListRunnerTests
{
    public SwitchListRunnerTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_1_prep_align_not_blocked_by_stale_transit_pin()
    {
        var prep = new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep");
        Assert.False(SwitchListRunner.PinBlocksAlignOrNext(prep, planArmedForClearance: true, sessionHasPin: true));
        var transit = new SwitchListStep(2, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit");
        Assert.True(SwitchListRunner.PinBlocksAlignOrNext(transit, planArmedForClearance: true, sessionHasPin: false));
    }

    [Fact]
    public void Smoke_13_1_turnaround_is_manual_not_human_hold()
    {
        var turn = new SwitchListStep(1, SwitchListStepKind.TurnAround, "SW", "#Y-#S1774#T", "Turn around");
        SwitchListSession.Bind("route:SW", new[] { turn });

        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(SwitchListRunnerSession.AllowsManualNext);
        Assert.False(SwitchListRunner.StepNeedsPinClearance(turn.Kind));
        Assert.Equal(SwitchListRunnerResult.NotHumanHold, SwitchListRunner.TryMarkDone(SwitchListRunnerSession.Mode));
    }

    [Fact]
    public void Smoke_13_1_turnaround_align_not_blocked_by_transit_pin()
    {
        Assert.True(RouteStepDestPolicy.ShouldRetargetMapsDest(
            "list-align",
            RouteClearancePhase.AtSwitch,
            SwitchListStepKind.TurnAround));
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest(
            "list-align",
            RouteClearancePhase.AtSwitch,
            SwitchListStepKind.Transit));
    }

    [Fact]
    public void Smoke_13_1_prep_enters_human_hold_blocks_next_until_done()
    {
        SwitchListSession.Bind(
            "SW-FH-1",
            new[]
            {
                new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-B3I", "Prep"),
                new SwitchListStep(2, SwitchListStepKind.Transit, "FH", "FH-A1", "Transit"),
            });

        Assert.Equal(SwitchListRunMode.HumanHold, SwitchListRunnerSession.Mode);
        Assert.False(SwitchListRunnerSession.AllowsManualNext);
        Assert.False(SwitchListSession.TryAdvance());

        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryMarkDone());
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
    }

    [Fact]
    public void Smoke_13_1_transit_go_arms_pid_even_when_cruise_off()
    {
        var transit = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Transit");
        SwitchListSession.Bind("route:SW", new[] { transit });
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);

        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));

        Assert.True(SwitchListRunner.PidGoActive(SwitchListRunnerSession.Mode, transit));
        Assert.False(SwitchListRunnerSession.AllowsManualNext);
        Assert.True(PidSpeedArm.IsArmed(
            goActive: true,
            hasMapsDest: true,
            switchListActiveIncomplete: true,
            facingReady: true,
            cruiseEnabled: false));

        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryStopGo());
        Assert.False(PidSpeedArm.IsArmed(
            goActive: false,
            hasMapsDest: true,
            switchListActiveIncomplete: true,
            facingReady: true,
            cruiseEnabled: false));
    }

    [Fact]
    public void Smoke_13_1_manual_switch_list_does_not_arm_cruise_pid()
    {
        Assert.False(PidSpeedArm.IsArmed(
            goActive: false,
            hasMapsDest: true,
            switchListActiveIncomplete: true,
            facingReady: true,
            cruiseEnabled: true));
    }

    [Fact]
    public void Smoke_13_1_go_fail_closed_without_plan_or_cleared()
    {
        var transit = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "#Y-#S969#T", "Past switch");
        Assert.Equal(
            SwitchListRunnerResult.NeedPlan,
            SwitchListRunner.TrySetGo(transit, hasPlan: false, pinForAlign: true, RouteClearancePhase.Idle));
        Assert.Equal(
            SwitchListRunnerResult.NeedCleared,
            SwitchListRunner.TrySetGo(transit, hasPlan: true, pinForAlign: true, RouteClearancePhase.AtSwitch));
        Assert.Equal(
            SwitchListRunnerResult.WrongStepKind,
            SwitchListRunner.TrySetGo(
                new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-B3I", "Prep"),
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));
    }

    [Fact]
    public void Smoke_13_1_go_blocked_while_human_hold()
    {
        SwitchListSession.Bind(
            "SW-FH-1",
            new[] { new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-B3I", "Prep") });
        Assert.Equal(SwitchListRunnerResult.NextBlocked, SwitchListRunner.TryManualNext(SwitchListRunnerSession.Mode));
    }
}
