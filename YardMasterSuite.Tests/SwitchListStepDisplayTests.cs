using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListStepDisplayTests
{
    [Fact]
    public void Smoke_13_1_past_switch_bind_needs_reverse_overrides_live_forward()
    {
        var step = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Reverse · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: true);
        var needsReverse = SwitchListStepDisplay.ResolveDriveNeedsReverse(
            step,
            RouteClearancePhase.AtSwitch,
            planPinArmed: true,
            sessionHasPin: true,
            pinLatched: false,
            pinTravelReverse: false,
            pinBehindLive: false,
            destBehindLive: false);
        Assert.True(needsReverse);
        var live = SwitchListStepDisplay.LiveLabel(step, needsReverse);
        Assert.StartsWith("Set Reverse · Past switch → SW-B4L", live);
    }

    [Fact]
    public void FormatDeskLine_marks_active_step_compactly()
    {
        var step = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "C",
            "Set Reverse · Past switch → C until CLEARED");
        var line = SwitchListStepDisplay.FormatDeskLine(step, 0, 2, isActive: true);
        Assert.StartsWith("▶ 1/2 · ", line);
        Assert.Contains("Past switch", line);
        Assert.DoesNotContain("until CLEARED", line);
    }

    [Fact]
    public void Smoke_13_2_2_desk_line_appends_at_track_on_active_prep()
    {
        var step = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        var line = SwitchListStepDisplay.FormatDeskLine(
            step, 4, 7, isActive: true, destNeedsReverse: true, atTrack: true);
        Assert.Contains("Prep → SW-C1O", line);
        Assert.EndsWith(" · at track", line);
        var idle = SwitchListStepDisplay.FormatDeskLine(
            step, 4, 7, isActive: false, destNeedsReverse: true, atTrack: true);
        Assert.DoesNotContain("at track", idle);
    }

    [Fact]
    public void Smoke_13_1_turnaround_uses_pin_approach_facing_before_cleared()
    {
        var step = new SwitchListStep(
            1,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.TurnAroundOnTurntable);
        var needsReverse = SwitchListStepDisplay.ResolveDriveNeedsReverse(
            step,
            RouteClearancePhase.AtSwitch,
            planPinArmed: true,
            sessionHasPin: true,
            pinLatched: true,
            pinTravelReverse: true,
            pinBehindLive: false,
            destBehindLive: false);
        Assert.True(needsReverse);
        Assert.Equal("Set Reverse · TT turn around", SwitchListStepDisplay.LiveLabel(step, needsReverse));
    }

    [Fact]
    public void Smoke_13_1_turnaround_and_prep_live_labels_include_set_word()
    {
        var turn = new SwitchListStep(
            1,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.TurnAroundOnTurntable);
        Assert.Equal("Set Forward · TT turn around", SwitchListStepDisplay.LiveLabel(turn, false));
        Assert.Equal("Set Reverse · TT turn around", SwitchListStepDisplay.LiveLabel(turn, true));

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
        Assert.Equal(
            "Set Reverse · to TT → #Y-#S1774#T",
            SwitchListStepDisplay.LiveLabel(toTt, true));
        Assert.Equal(
            "Set Forward · to TT → #Y-#S1774#T",
            SwitchListStepDisplay.LiveLabel(toTt, false));
        Assert.DoesNotContain(SwitchListDriveFacing.TurnAroundOnTurntable, SwitchListStepDisplay.LiveLabel(toTt, true));

        var prep = new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        Assert.Equal("Set Forward · Prep → SW-C1O", SwitchListStepDisplay.LiveLabel(prep, false));
        Assert.Equal("Set Reverse · Prep → SW-C1O", SwitchListStepDisplay.LiveLabel(prep, true));
    }

    [Fact]
    public void Smoke_13_1_drive_to_tt_after_inbound_cleared_is_Set_Forward()
    {
        var job = new JobSummary
        {
            JobId = "SW-FH-82",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C1O",
            DestTrackId = "GF-D5I",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
        };
        var toTt = SwitchListPlanner.Build(job)![1];
        Assert.True(SwitchListDriveFacing.IsDriveToTurntable(toTt.Label));
        Assert.Null(toTt.BindNeedsReverse);
        Assert.False(RouteDestFacingPolicy.DestNeedsReverse(
            pinNeedsReverse: true,
            destCrowFliesBehind: true));

        var needsReverse = SwitchListStepDisplay.ResolveDriveNeedsReverse(
            toTt,
            RouteClearancePhase.Cleared,
            planPinArmed: false,
            sessionHasPin: false,
            pinLatched: true,
            pinTravelReverse: true,
            pinBehindLive: false,
            destBehindLive: false);
        Assert.False(needsReverse);
        var live = SwitchListStepDisplay.LiveLabel(toTt, needsReverse);
        Assert.StartsWith("Set Forward · to TT", live);
        Assert.DoesNotContain("Set Reverse", live);
        Assert.Contains("Set Forward", SwitchListStepDisplay.FormatDeskLine(
            toTt, 1, 6, isActive: true, destNeedsReverse: needsReverse));
    }

    [Fact]
    public void Smoke_8_7_after_CLEARED_dest_ahead_is_Set_Forward_into_TT()
    {
        var frozen = new SwitchListStep(
            2,
            SwitchListStepKind.ReverseInto,
            "SW",
            "#Y-#S1774#T",
            "Set Reverse · Reverse into → #Y-#S1774#T");
        Assert.True(SwitchListStepDisplay.UsesLiveDestFacing(frozen.Kind));
        Assert.Contains("Set Reverse", frozen.Label);

        var live = SwitchListStepDisplay.LiveLabel(frozen, false);
        Assert.Contains("Set Forward", live);
        Assert.DoesNotContain("Set Reverse", live);
        Assert.Contains("#Y-#S1774#T", live);
        Assert.Contains("into", live);

        var stillBehind = SwitchListStepDisplay.LiveLabel(frozen, true);
        Assert.StartsWith("Set Reverse", stillBehind);

        var pastSwitch = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "#Y-#S989#T",
            "Set Reverse · Past switch → #Y-#S989#T until CLEARED");
        Assert.True(SwitchListStepDisplay.UsesLiveDriveFacing(pastSwitch.Kind));
        Assert.Equal(pastSwitch.Label, SwitchListStepDisplay.LiveLabel(pastSwitch, null));
        Assert.Contains("Set Forward", SwitchListStepDisplay.LiveLabel(pastSwitch, false));
    }

    [Fact]
    public void Smoke_8_7_B4L_Set_dest_live_overlay_does_not_use_crow_flies_behind()
    {
        var frozen = new SwitchListStep(
            2,
            SwitchListStepKind.ReverseInto,
            "SW",
            "#Y-#S1774#T",
            "Set Reverse · Reverse into → #Y-#S1774#T");
        var destSetReverse = RouteDestFacingPolicy.DestNeedsReverse(
            pinNeedsReverse: true,
            destCrowFliesBehind: true);
        var live = SwitchListStepDisplay.LiveLabel(frozen, destSetReverse);
        Assert.Contains("Set Forward", live);
        Assert.DoesNotContain("Set Reverse", live);
        Assert.Contains("into", live);

        var line = SwitchListStepDisplay.FormatDeskLine(
            frozen, 1, 2, isActive: false, destNeedsReverse: destSetReverse);
        Assert.Contains("2/2", line);
        Assert.Contains("Set Forward", line);
    }
}
