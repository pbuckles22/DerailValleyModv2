using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteReverseHitchGateTests
{
    [Fact]
    public void Smoke_8_7_quiet_cab_while_reversing_to_pin_not_cleared()
    {
        Assert.True(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.Approaching,
            consistMoving: true));
        Assert.True(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.AtSwitch,
            consistMoving: true));
    }

    [Fact]
    public void Smoke_8_7_set_dest_at_switch_keeps_desk_until_moving()
    {
        Assert.False(RouteReverseHitchGate.ConsistIsMoving(0f));
        Assert.True(RouteReverseHitchGate.ConsistIsMoving(1f));
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.AtSwitch,
            consistMoving: false));
    }

    /// <summary>
    /// CLEARED does not auto-reopen the desk. Ctrl+Insert restores Align / Next.
    /// </summary>
    [Fact]
    public void Smoke_8_7_cleared_does_not_quiet_so_ctrl_insert_can_restore()
    {
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.Cleared,
            consistMoving: true));
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.Idle,
            consistMoving: true));
    }

    [Fact]
    public void Smoke_8_7_on_foot_desk_still_draws()
    {
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: false,
            travelUsesReverse: true,
            RouteClearancePhase.AtSwitch,
            consistMoving: true));
    }

    [Fact]
    public void Smoke_8_7_forward_pin_keeps_desk()
    {
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: false,
            RouteClearancePhase.AtSwitch,
            consistMoving: true));
    }

    /// <summary>
    /// Cab 2.13.2.5.1: auto hitch-hide is OK; Ctrl+Insert must reopen and
    /// stay open (override). Hitch-hold swallowing Insert left GO unreachable.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_1_ctrl_insert_overrides_hitch_hold_reverse()
    {
        Assert.True(RouteReverseHitchGate.ShouldAutoHideDesk(
            quietCab: true, insertOverride: false));
        Assert.False(RouteReverseHitchGate.ShouldAutoHideDesk(
            quietCab: true, insertOverride: true));
        Assert.False(RouteReverseHitchGate.ShouldAutoHideDesk(
            quietCab: false, insertOverride: false));

        Assert.False(RouteReverseHitchGate.BlocksInsertReopen(quietCab: true));
        Assert.False(RouteReverseHitchGate.BlocksInsertReopen(quietCab: false));

        Assert.True(RouteReverseHitchGate.LatchInsertOverride(quietCab: true));
        Assert.False(RouteReverseHitchGate.LatchInsertOverride(quietCab: false));
    }
}
