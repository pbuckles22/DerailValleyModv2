using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 8.7 smoke: misaligned throws and Path OK sawtooth (JunctionFirstStop) show 1/2 then 2/2;
/// straight Path OK omits coach.
/// </summary>
public class RouteSwitchCoachTests
{
    [Fact]
    public void Path_OK_omits_coach()
    {
        var lines = RouteSwitchCoach.Format(
            pinArmed: false,
            RouteClearancePhase.Idle,
            pinIsBehind: false,
            destIsBehind: true);
        Assert.False(lines.Show);
        Assert.Null(RouteSwitchCoach.ActiveLine(in lines));
        Assert.Null(lines.ArCaption);
    }

    [Fact]
    public void Path_N_switch_step1_drive_past_until_CLEARED()
    {
        var lines = RouteSwitchCoach.Format(
            pinArmed: true,
            RouteClearancePhase.AtSwitch,
            pinIsBehind: false,
            destIsBehind: true);
        Assert.True(lines.Show);
        Assert.Equal(1, lines.ActiveStep);
        Assert.Equal("1/2 Drive past switch — Set Forward until CLEARED", lines.Step1);
        Assert.Equal("2/2 Align Route, then Set Reverse to dest", lines.Step2);
        Assert.Equal("At switch", lines.ArCaption);
        Assert.Equal(lines.Step1, RouteSwitchCoach.ActiveLine(in lines));
    }

    [Fact]
    public void Path_N_switch_step1_Set_Reverse_when_pin_behind()
    {
        var lines = RouteSwitchCoach.Format(
            pinArmed: true,
            RouteClearancePhase.Approaching,
            pinIsBehind: true,
            destIsBehind: false);
        Assert.Equal(1, lines.ActiveStep);
        Assert.Equal("1/2 Drive past switch — Set Reverse until CLEARED", lines.Step1);
        Assert.Equal("2/2 Align Route, then Set Forward to dest", lines.Step2);
    }

    [Fact]
    public void CLEARED_advances_to_step2_press_Align()
    {
        var lines = RouteSwitchCoach.Format(
            pinArmed: true,
            RouteClearancePhase.Cleared,
            pinIsBehind: false,
            destIsBehind: true);
        Assert.True(lines.Show);
        Assert.Equal(2, lines.ActiveStep);
        Assert.Equal("1/2 CLEARED — press Align", lines.Step1);
        Assert.Equal("2/2 Align Route, then Set Reverse to dest", lines.Step2);
        Assert.Equal("CLEARED", lines.ArCaption);
        Assert.Equal(lines.Step2, RouteSwitchCoach.ActiveLine(in lines));
    }

    [Fact]
    public void Smoke_8_7_CLEARED_dest_ahead_coach_is_Set_Forward()
    {
        var lines = RouteSwitchCoach.Format(
            pinArmed: true,
            RouteClearancePhase.Cleared,
            pinIsBehind: false,
            destIsBehind: false);
        Assert.Equal(2, lines.ActiveStep);
        Assert.Equal("2/2 Align Route, then Set Forward to dest", lines.Step2);
    }
}
