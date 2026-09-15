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
    /// HTP engineer walk (idle 9-row SL-55): every CLEARED frog is a reversal.
    /// Reverse Past B4L → Forward to TT; Forward Past leave → Reverse Prep;
    /// Forward Past B4L → Reverse Prep C4S. Same-direction through-frogs are
    /// not pins (to-TT, TT spin, Prep, Transit).
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
            var expected = SwitchListPinFacing.NextDriveNeedsReverseAfterCleared(steps[i]);
            Assert.True(expected.HasValue);
            var next = SwitchListPinFacing.NextDriveAfterClearedFrog(steps, i);
            Assert.NotNull(next);
            Assert.Equal(expected.Value, SwitchListPinFacing.StepNeedsReverse(next));
        }

        Assert.Equal(new[] { 1, 4, 6, 8 }, frogIndexes.ToArray());
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[1]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[2]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[4]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[6]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[8]));
        Assert.False(SwitchListPinFacing.IsClearedFrogPin(steps[9]));
    }
}
