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
}
