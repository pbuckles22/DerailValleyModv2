using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListPinFacingTests
{
    [Fact]
    public void AlternateNeedsReverse_flips_F_to_R_and_R_to_F()
    {
        Assert.True(SwitchListPinFacing.AlternateNeedsReverse(false));
        Assert.False(SwitchListPinFacing.AlternateNeedsReverse(true));
    }

    [Fact]
    public void AlternateAfter_past_switch_flips_and_prep_does_not()
    {
        var forwardPin = new SwitchListStep(
            6,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Forward · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: false);
        var prep = new SwitchListStep(
            5,
            SwitchListStepKind.Prep,
            "SW",
            "SW-B1S",
            "Prep → SW-B1S");
        var ttSpin = new SwitchListStep(
            3,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.TurnAroundOnTurntable,
            bindNeedsReverse: false);

        Assert.True(SwitchListPinFacing.IsDrivePin(forwardPin));
        Assert.False(SwitchListPinFacing.IsDrivePin(prep));
        Assert.False(SwitchListPinFacing.IsDrivePin(ttSpin));
        Assert.True(SwitchListPinFacing.IsClearedFrogPin(forwardPin));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(prep));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(ttSpin));
        var toTt = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(toTt));
        Assert.Equal(true, SwitchListPinFacing.AlternateAfter(forwardPin));
        Assert.Null(SwitchListPinFacing.AlternateAfter(prep));
        Assert.Null(SwitchListPinFacing.AlternateAfter(ttSpin));
    }

    [Fact]
    public void Smoke_SL55_cleared_4_forward_pin_next_is_Reverse()
    {
        var leave = new SwitchListStep(
            4,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Forward · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: false);
        Assert.Equal(true, SwitchListPinFacing.AlternateAfter(leave));
        Assert.True(SwitchListPinFacing.NeedsReverseAtPin(false));
        Assert.False(
            SwitchListStepDisplay.ResolveDriveNeedsReverse(
                leave,
                RouteClearancePhase.AtSwitch,
                planPinArmed: true,
                sessionHasPin: true,
                pinLatched: true,
                pinTravelReverse: false,
                pinBehindLive: false,
                destBehindLive: false));
        Assert.True(
            SwitchListStepDisplay.ResolveDriveNeedsReverse(
                leave,
                RouteClearancePhase.Cleared,
                planPinArmed: true,
                sessionHasPin: true,
                pinLatched: true,
                pinTravelReverse: false,
                pinBehindLive: false,
                destBehindLive: false));
        var pastC4s = new SwitchListStep(
            6,
            SwitchListStepKind.Transit,
            "SW",
            "SW-C4S",
            "Set Forward · Past switch → SW-C4S until CLEARED",
            bindNeedsReverse: false);
        Assert.Equal(true, SwitchListPinFacing.AlternateAfter(pastC4s));
    }

    /// <summary>
    /// Cab 2.13.2.5.19: Forward into a pin then Prep stayed Forward. The next
    /// drive at that pin is the opposite of the approach.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_20_at_pin_facing_is_opposite_of_approach()
    {
        var forwardPin = new SwitchListStep(
            6,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Forward · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: false);
        Assert.Equal(true, SwitchListPinFacing.AlternateAfter(forwardPin));
        Assert.True(SwitchListPinFacing.NeedsReverseAtPin(false));

        var reversePin = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Reverse · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: true);
        Assert.Equal(false, SwitchListPinFacing.AlternateAfter(reversePin));
        Assert.False(SwitchListPinFacing.NeedsReverseAtPin(true));
    }

    /// <summary>
    /// HTP SL-55 / engineer bible: frog 1, 4, and 6 reverse the next drive.
    /// Leave-TT frog 4 Forward Past → Reverse Prep B1S. Same-track Past C4S
    /// still Reverse Prep C4S.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_22_10_SL55_cleared_frog_means_opposite_next()
    {
        var steps = SwitchListPlanner.Build(new JobSummary
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
            LoadTrackId = "SW-B4L",
            LoadCargoLabel = "Wood Chips",
        });
        Assert.NotNull(steps);

        var frogIndexes = new System.Collections.Generic.List<int>();
        for (var i = 0; i < steps!.Count; i++)
        {
            if (!SwitchListPinFacing.IsClearedFrogPin(steps[i]))
            {
                continue;
            }

            frogIndexes.Add(steps[i].Index);
            var next = SwitchListPinFacing.NextDriveAfterClearedFrog(steps, i);
            Assert.NotNull(next);
            var expected = SwitchListPinFacing.NextDriveNeedsReverseAfterCleared(steps[i]);
            Assert.True(expected.HasValue);
            Assert.Equal(expected.Value, SwitchListPinFacing.StepNeedsReverse(next));
        }

        Assert.Equal(new[] { 1, 4, 6, 8, 11 }, frogIndexes.ToArray());
        Assert.True(steps[4].BindNeedsReverse);
        Assert.Contains(SwitchListDriveFacing.Reverse, SwitchListStepDisplay.LiveLabel(steps[4], true));
        Assert.True(steps[6].BindNeedsReverse);
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[1]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[2]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[4]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[6]));
        Assert.True(SwitchListPinFacing.IsClearedFrogPin(steps[7]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[8]));
        Assert.True(steps[8].BindNeedsReverse);
    }
}
