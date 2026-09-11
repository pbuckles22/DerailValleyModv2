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
}
