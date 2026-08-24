using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class OnConsistControlTests
{
    [Fact]
    public void ResolveFrontLocoIndex_null_when_player_off_consist()
    {
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: false, new[] { 0 }));
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: false, Array.Empty<int>()));
    }

    [Fact]
    public void ResolveFrontLocoIndex_null_when_no_loco_on_trainset()
    {
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, Array.Empty<int>()));
        Assert.Null(OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, null!));
    }

    [Fact]
    public void Smoke_player_on_last_car_still_picks_front_loco()
    {
        Assert.Equal(0, OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, new[] { 0 }));
        Assert.Equal(0, OnConsistControl.ResolveFrontLocoIndex(playerOnCar: true, new[] { 3, 0, 2 }));
    }

    [Fact]
    public void Smoke_cab_keys_do_not_double_step_when_standing_on_mu_mate()
    {
        // Wagon: native cab keys do not reach a loco — redirect.
        Assert.True(OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar: true, standingIsLoco: false));
        // Any loco (front cab or MU mate): native + MU already step. Redirect would 9%→18%.
        Assert.False(OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar: true, standingIsLoco: true));
        Assert.False(OnConsistControl.ShouldRedirectToFrontLoco(playerOnCar: false, standingIsLoco: false));
    }

    [Fact]
    public void IsSafeToWrite_requires_on_consist_and_front_loco()
    {
        Assert.True(OnConsistControl.IsSafeToWrite(true, true, true, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(false, true, true, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, false, true, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, true, false, true, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, true, true, false, true));
        Assert.False(OnConsistControl.IsSafeToWrite(true, true, true, true, false));
    }

    [Fact]
    public void CanWriteLever_ignores_cab_reach_blocker_when_present()
    {
        Assert.True(OnConsistControl.CanWriteLever(controlPresent: true, controlBlocked: true));
        Assert.True(OnConsistControl.CanWriteLever(controlPresent: true, controlBlocked: false));
        Assert.False(OnConsistControl.CanWriteLever(controlPresent: false, controlBlocked: false));
    }

    [Fact]
    public void StepReverser_notches_R_N_F()
    {
        Assert.Equal(0.5f, OnConsistControl.StepReverser(0f, direction: +1), 3);
        Assert.Equal(1f, OnConsistControl.StepReverser(0.5f, direction: +1), 3);
        Assert.Equal(1f, OnConsistControl.StepReverser(1f, direction: +1), 3);
        Assert.Equal(0.5f, OnConsistControl.StepReverser(1f, direction: -1), 3);
        Assert.Equal(0f, OnConsistControl.StepReverser(0.5f, direction: -1), 3);
    }

    [Fact]
    public void CycleReverser_n_then_r_then_f()
    {
        Assert.Equal(0f, OnConsistControl.CycleReverser(0.5f), 3);
        Assert.Equal(1f, OnConsistControl.CycleReverser(0f), 3);
        Assert.Equal(0.5f, OnConsistControl.CycleReverser(1f), 3);
    }

    [Fact]
    public void Smoke_reverser_cycle_reverse_stays_forward()
    {
        Assert.Equal(1f, OnConsistControl.CycleReverser(0f), 3);
        Assert.True(ReverserCyclePressGate.ShouldPassThroughNeutral(0f, 1f));
        Assert.False(ReverserCyclePressGate.ShouldPassThroughNeutral(0.5f, 0f));

        var lastAcceptedAt = -1f;
        Assert.True(ReverserCyclePressGate.ShouldAcceptPress(1.00f, lastAcceptedAt));
        lastAcceptedAt = 1.00f;
        Assert.False(ReverserCyclePressGate.ShouldAcceptPress(1.05f, lastAcceptedAt));
        Assert.True(ReverserCyclePressGate.ShouldAcceptPress(1.40f, lastAcceptedAt));

        Assert.True(ReverserCyclePressGate.ShouldHoldWrittenValue(1.10f, writtenAt: 1.00f));
        Assert.False(ReverserCyclePressGate.ShouldHoldWrittenValue(1.40f, writtenAt: 1.00f));
    }

    [Fact]
    public void StepLever_matches_cab_notch()
    {
        Assert.Equal(1f / 9f, OnConsistControl.StepLever(0f, +1, isNotched: true, notchCount: 10f), 3);
        Assert.Equal(0.1f, OnConsistControl.StepLever(0f, +1, isNotched: false, notchCount: 1f), 3);
    }

    [Fact]
    public void HudLegend_points_at_cab_bindings()
    {
        Assert.Contains("Throttle", OnConsistControl.HudLegend);
        Assert.Contains("front loco", OnConsistControl.HudLegend);
        Assert.Contains("Numpad Enter", OnConsistControl.HudLegend);
        Assert.Contains("TM fuse", OnConsistControl.HudLegend);
        Assert.DoesNotContain("/ Reverser →", OnConsistControl.HudLegend);
    }
}

public class HoldRepeatTests
{
    [Fact]
    public void ShouldFire_press_then_delay_then_repeat()
    {
        var next = 0f;
        Assert.True(HoldRepeat.ShouldFire(pressedThisFrame: true, isHeld: true, timeHeld: 0f, ref next));
        Assert.Equal(HoldRepeat.DefaultInitialDelaySeconds, next, 3);
        Assert.False(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: true, timeHeld: 0.20f, ref next));
        Assert.True(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: true, timeHeld: 0.35f, ref next));
        Assert.False(HoldRepeat.ShouldFire(pressedThisFrame: false, isHeld: false, timeHeld: 1f, ref next));
        Assert.Equal(0f, next);
    }
}
