using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListStepDisplayTests
{
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

        var live = SwitchListStepDisplay.LiveLabel(frozen, destNeedsReverse: false);
        Assert.Contains("Set Forward", live);
        Assert.DoesNotContain("Set Reverse", live);
        Assert.Contains("#Y-#S1774#T", live);
        Assert.Contains("into", live);

        var stillBehind = SwitchListStepDisplay.LiveLabel(frozen, destNeedsReverse: true);
        Assert.StartsWith("Set Reverse", stillBehind);

        var pastSwitch = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "#Y-#S989#T",
            "Set Reverse · Past switch → #Y-#S989#T until CLEARED");
        Assert.False(SwitchListStepDisplay.UsesLiveDestFacing(pastSwitch.Kind));
        Assert.Equal(pastSwitch.Label, SwitchListStepDisplay.LiveLabel(pastSwitch, destNeedsReverse: false));
    }
}
