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
            RouteClearancePhase.Approaching));
        Assert.True(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.AtSwitch));
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
            RouteClearancePhase.Cleared));
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: true,
            RouteClearancePhase.Idle));
    }

    [Fact]
    public void Smoke_8_7_on_foot_desk_still_draws()
    {
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: false,
            travelUsesReverse: true,
            RouteClearancePhase.AtSwitch));
    }

    [Fact]
    public void Smoke_8_7_forward_pin_keeps_desk()
    {
        Assert.False(RouteReverseHitchGate.QuietCabDuringPinReverse(
            boardedLoco: true,
            travelUsesReverse: false,
            RouteClearancePhase.AtSwitch));
    }
}
