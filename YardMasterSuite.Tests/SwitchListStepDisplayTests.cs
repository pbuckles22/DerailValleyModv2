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

        var prep = new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        Assert.Equal("Set Forward · Prep → SW-C1O", SwitchListStepDisplay.LiveLabel(prep, false));
        Assert.Equal("Set Reverse · Prep → SW-C1O", SwitchListStepDisplay.LiveLabel(prep, true));
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
