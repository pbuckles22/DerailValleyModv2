using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.4 auto-coupler: fail-closed couple assist, no RCL remote.</summary>
public class AutoCoupleAssistTests
{
    [Fact]
    public void Smoke_in_scan_range_couples_when_on_consist()
    {
        Assert.Equal(
            AutoCoupleAction.Couple,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: true,
                mechanicallyCoupled: false,
                linkComplete: false,
                closeEnough: true,
                speedOk: true));
        Assert.True(AutoCoupleAssist.IsSafeToWrite(
            worldActive: true,
            actorOnConsist: true,
            tipPresent: true,
            preventCouple: false,
            overlayClear: true,
            AutoCoupleAction.Couple));
    }

    [Fact]
    public void Smoke_off_train_does_not_couple()
    {
        Assert.False(AutoCoupleAssist.ActorOnConsist(playerOnCar: false, standingInSameTrainset: false));
        Assert.False(AutoCoupleAssist.IsSafeToWrite(
            worldActive: true,
            actorOnConsist: false,
            tipPresent: true,
            preventCouple: false,
            overlayClear: true,
            AutoCoupleAction.Couple));

        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: false),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });
        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Integrity, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_look_at_other_train_is_not_on_consist()
    {
        Assert.False(AutoCoupleAssist.ActorOnConsist(playerOnCar: true, standingInSameTrainset: false));
    }

    [Fact]
    public void Smoke_neutral_reverser_does_not_couple()
    {
        Assert.False(AutoCoupleAssist.HasTravelAim(ProximityTravelDirection.Neutral));
        Assert.False(AutoCoupleAssist.HasTravelAim(ProximityTravelDirection.Unknown));
        Assert.True(AutoCoupleAssist.HasTravelAim(ProximityTravelDirection.Reverse));
        Assert.True(AutoCoupleAssist.HasTravelAim(ProximityTravelDirection.Forward));
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: false,
                hasTip: true,
                partnerInRange: true,
                mechanicallyCoupled: false,
                linkComplete: false,
                closeEnough: true,
                speedOk: true));
    }

    [Fact]
    public void Smoke_out_of_scan_does_not_couple()
    {
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: false,
                mechanicallyCoupled: false,
                linkComplete: false,
                closeEnough: true,
                speedOk: true));
    }

    [Fact]
    public void Smoke_rear_four_meters_does_not_couple()
    {
        Assert.False(AutoCoupleAssist.ClearanceAllowsCouple(3.9f));
        Assert.False(AutoCoupleAssist.ClearanceAllowsCouple(4f));
        Assert.True(AutoCoupleAssist.ClearanceAllowsCouple(0.5f));
        Assert.False(AutoCoupleAssist.ClearanceAllowsCouple(0.51f));
        Assert.False(AutoCoupleAssist.ClearanceAllowsCouple(null));
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: true,
                mechanicallyCoupled: false,
                linkComplete: false,
                closeEnough: false,
                speedOk: true));
    }

    [Fact]
    public void Smoke_does_not_couple_at_high_speed_after_snap()
    {
        Assert.True(AutoCoupleAssist.SpeedAllowsCouple(2f));
        Assert.True(AutoCoupleAssist.SpeedAllowsCouple(8f));
        Assert.False(AutoCoupleAssist.SpeedAllowsCouple(14f));
        Assert.False(AutoCoupleAssist.SpeedAllowsCouple(22f));
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: true,
                mechanicallyCoupled: false,
                linkComplete: false,
                closeEnough: true,
                speedOk: false));
    }

    [Fact]
    public void Smoke_already_linked_does_not_write()
    {
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: true,
                mechanicallyCoupled: true,
                linkComplete: true,
                closeEnough: true,
                speedOk: true));
        Assert.False(AutoCoupleAssist.IsSafeToWrite(
            worldActive: true,
            actorOnConsist: true,
            tipPresent: true,
            preventCouple: false,
            overlayClear: true,
            AutoCoupleAction.None));
    }

    [Fact]
    public void Smoke_loose_coupled_finishes_link()
    {
        Assert.Equal(
            AutoCoupleAction.Finish,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: false,
                mechanicallyCoupled: true,
                linkComplete: false,
                closeEnough: false,
                speedOk: true));
    }

    [Fact]
    public void Smoke_wagon_usable_link_does_not_need_mu()
    {
        Assert.True(AutoCoupleAssist.LinkComplete(
            mechanicallyCoupled: true,
            tightened: true,
            airHoseConnected: true,
            cocksOpenBothSides: true));
    }

    [Fact]
    public void Smoke_loco_loco_mu_is_best_effort_not_a_hold()
    {
        Assert.True(AutoCoupleAssist.LinkComplete(
            mechanicallyCoupled: true,
            tightened: true,
            airHoseConnected: true,
            cocksOpenBothSides: true));
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: true,
                partnerInRange: true,
                mechanicallyCoupled: true,
                linkComplete: true,
                closeEnough: true,
                speedOk: true));
    }

    [Fact]
    public void Smoke_prevent_couple_safety_aborts_without_write()
    {
        Assert.False(AutoCoupleAssist.IsSafeToWrite(
            worldActive: true,
            actorOnConsist: true,
            tipPresent: true,
            preventCouple: true,
            overlayClear: true,
            AutoCoupleAction.Couple));

        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: false),
            () =>
            {
                calls++;
                return true;
            });
        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Safety, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_pause_overlay_aborts_without_write()
    {
        Assert.False(AutoCoupleAssist.IsSafeToWrite(
            worldActive: true,
            actorOnConsist: true,
            tipPresent: true,
            preventCouple: false,
            overlayClear: false,
            AutoCoupleAction.Couple));

        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: false, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });
        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Safety, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_in_range_three_gate_applies_soft_write()
    {
        var action = AutoCoupleAssist.Decide(
            hasTravelAim: true,
            hasTip: true,
            partnerInRange: true,
            mechanicallyCoupled: false,
            linkComplete: false,
            closeEnough: true,
            speedOk: true);
        Assert.Equal(AutoCoupleAction.Couple, action);
        Assert.True(AutoCoupleAssist.IsSafeToWrite(
            true, true, true, false, true, action));

        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });
        Assert.True(result.Applied);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Missing_tip_is_not_safe()
    {
        Assert.False(AutoCoupleAssist.IsSafeToWrite(
            worldActive: true,
            actorOnConsist: true,
            tipPresent: false,
            preventCouple: false,
            overlayClear: true,
            AutoCoupleAction.Couple));
        Assert.Equal(
            AutoCoupleAction.None,
            AutoCoupleAssist.Decide(
                hasTravelAim: true,
                hasTip: false,
                partnerInRange: true,
                mechanicallyCoupled: false,
                linkComplete: false,
                closeEnough: true,
                speedOk: true));
    }

    [Fact]
    public void Decide_does_not_allocate()
    {
        AutoCoupleAssist.Decide(true, true, true, false, false, true, true);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            AutoCoupleAssist.Decide(true, true, true, false, false, true, true);
            AutoCoupleAssist.IsSafeToWrite(true, true, true, false, true, AutoCoupleAction.Couple);
            AutoCoupleAssist.LinkComplete(true, true, true, true);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
