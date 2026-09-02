using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class SwitchListRunnerTests
{
    public SwitchListRunnerTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_1_leave_past_switch_blocks_align_prep_does_not()
    {
        var leave = new SwitchListStep(
            3,
            SwitchListStepKind.Transit,
            "SW",
            "#Y-#S1774#T",
            "Past switch → #Y-#S1774#T until CLEARED");
        var prep = new SwitchListStep(4, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        Assert.True(SwitchListRunner.StepNeedsPinClearance(leave.Kind));
        Assert.True(SwitchListRunner.PinBlocksAlignOrNext(leave, planArmedForClearance: true, sessionHasPin: true));
        Assert.False(SwitchListRunner.PinBlocksAlignOrNext(prep, planArmedForClearance: true, sessionHasPin: true));
        Assert.Equal(SwitchListRunMode.HumanHold, SwitchListRunner.EnterModeForStep(prep));
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunner.EnterModeForStep(leave));
    }

    [Fact]
    public void Smoke_13_1_reload_prep_ready_list_must_not_paint_stale_CLEARED()
    {
        var prep = new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        Assert.True(SwitchListRunner.StepUsesApproachPinFacing(prep.Kind));
        Assert.False(SwitchListRunner.PinDisplayAllowed(prep, switchListActive: true));
        Assert.True(SwitchListRunner.PinDisplayAllowed(
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past switch"),
            switchListActive: true));
        Assert.True(SwitchListRunner.PinDisplayAllowed(prep, switchListActive: false));
        Assert.Equal(
            "T2 switch-list: list-load drop stale pin 990152",
            SwitchListRunner.FormatDropStalePinLog("990152"));
    }

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
    public void Smoke_13_1_prep_human_hold_still_allows_next_to_later_align()
    {
        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
                new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
                new SwitchListStep(7, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery → GF-D5I"),
            });

        Assert.Equal(SwitchListRunMode.HumanHold, SwitchListRunnerSession.Mode);
        Assert.True(SwitchListRunnerSession.AllowsManualNext);
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Delivery, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal(SwitchListRunMode.HumanHold, SwitchListRunnerSession.Mode);
        Assert.False(SwitchListRunnerSession.AllowsManualNext);
        Assert.False(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryMarkDone());
        Assert.True(SwitchListRunnerSession.AllowsManualNext);
    }

    [Fact]
    public void Smoke_13_1_seven_row_desk_list_fits_last_step()
    {
        Assert.Equal(144, SwitchListStepDisplay.DeskListViewHeightPx(7, compact: false));
        Assert.True(SwitchListStepDisplay.DeskListViewHeightPx(7, compact: false) >= 7 * 20);
        Assert.Equal(56, SwitchListStepDisplay.DeskListViewHeightPx(7, compact: true));
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
        Assert.Equal(SwitchListRunnerResult.NextBlocked, SwitchListRunner.TryManualNext(
            SwitchListRunnerSession.Mode,
            hasNextStep: false));
        Assert.False(SwitchListRunnerSession.AllowsManualNext);
    }

    [Fact]
    public void Smoke_13_1_to_tt_after_inbound_is_not_cleared_gate()
    {
        var toTt = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                true,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"),
            bindNeedsReverse: true);
        Assert.False(SwitchListRunner.StepNeedsPinClearance(toTt.Kind));
        Assert.False(SwitchListRunner.PinBlocksAlignOrNext(toTt, planArmedForClearance: true, sessionHasPin: true));
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunner.EnterModeForStep(toTt));
        Assert.False(SwitchListRunner.PinStaysAfterNext(toTt));
    }

    [Fact]
    public void Smoke_13_1_pin_stays_only_when_next_needs_clearance()
    {
        var nextTransit = new SwitchListStep(
            2,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Past switch → SW-B4L until CLEARED");
        var nextPivot = new SwitchListStep(
            2,
            SwitchListStepKind.Pivot,
            "SW",
            "#Y-#S23#T",
            "Pivot → #Y-#S23#T until CLEARED");
        var nextSpin = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.TurnAroundOnTurntable);
        var nextPrep = new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        Assert.True(SwitchListRunner.PinStaysAfterNext(nextTransit));
        Assert.True(SwitchListRunner.PinStaysAfterNext(nextPivot));
        Assert.False(SwitchListRunner.PinStaysAfterNext(nextSpin));
        Assert.False(SwitchListRunner.PinStaysAfterNext(nextPrep));
        Assert.False(SwitchListRunner.PinStaysAfterNext(null));

        var inbound = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Past switch → SW-B4L until CLEARED");
        var leave = new SwitchListStep(
            4,
            SwitchListStepKind.Transit,
            "SW",
            "#Y-#S1512#T",
            "Past switch → #Y-#S1512#T until CLEARED");
        Assert.True(SwitchListRunner.PinStaysAfterNext(inbound, nextTransit));
        Assert.False(SwitchListRunner.PinStaysAfterNext(nextSpin, leave));
        Assert.False(SwitchListRunner.PinStaysAfterNext(leave, nextPrep));
    }
}
