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
    public void Smoke_numpad_enter_cycles_reverser_on_loco_and_wagon()
    {
        // Dedicated KeypadEnter — not Rewired Incremental. Cab + wagon both OK.
        Assert.True(OnConsistControl.ShouldCycleReverserFromOnConsist(
            playerOnCar: true,
            standingIsLoco: true));
        Assert.True(OnConsistControl.ShouldCycleReverserFromOnConsist(
            playerOnCar: true,
            standingIsLoco: false));
        Assert.False(OnConsistControl.ShouldCycleReverserFromOnConsist(
            playerOnCar: false,
            standingIsLoco: false));
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
    public void Smoke_numpad_enter_key_repeat_does_not_cycle_until_keyup()
    {
        var lastAcceptedAt = 1.00f;
        Assert.False(ReverserCyclePressGate.ShouldAcceptPress(
            1.40f,
            lastAcceptedAt,
            sawKeyUpSinceLastAccept: false));
        Assert.True(ReverserCyclePressGate.ShouldAcceptPress(
            1.40f,
            lastAcceptedAt,
            sawKeyUpSinceLastAccept: true));
    }

    [Fact]
    public void HudLegend_points_at_cab_bindings()
    {
        Assert.DoesNotContain("Throttle", OnConsistControl.HudLegend);
        Assert.Contains("Numpad Enter", OnConsistControl.HudLegend);
        Assert.Contains("TM fuse", OnConsistControl.HudLegend);
        Assert.DoesNotContain("/ Reverser →", OnConsistControl.HudLegend);
    }

    [Fact]
    public void Smoke_on_consist_does_not_write_throttle_indy_train()
    {
        // Player.log 2.6.21.3: thr/indy/train walked together (GetButtonDown chatter).
        Assert.False(OnConsistControl.ShouldWriteCabLevers);
    }

    [Fact]
    public void Smoke_loading_screen_does_not_poll_on_consist_keys()
    {
        // Premature input poll before Rewired → ControlMapperSaver NRE / bad bindings.
        Assert.False(OnConsistControl.ShouldPollInput(worldActive: false));
        Assert.True(OnConsistControl.ShouldPollInput(worldActive: true));
    }

    [Fact]
    public void Smoke_cab_incremental_chatter_does_not_reclimb()
    {
        // Same session, in the seat (no T2 on-consist armed): Down every frame while held.
        var wasHeld = false;
        Assert.True(IncrementalChatterGate.ShouldApplyNotch(buttonDown: true, wasHeld));
        wasHeld = true;
        Assert.False(IncrementalChatterGate.ShouldApplyNotch(buttonDown: true, wasHeld));
        Assert.False(IncrementalChatterGate.ShouldApplyNotch(buttonDown: true, wasHeld));
        wasHeld = false;
        Assert.True(IncrementalChatterGate.ShouldApplyNotch(buttonDown: true, wasHeld));
        Assert.False(IncrementalChatterGate.ShouldApplyNotch(buttonDown: false, wasHeld: false));
    }
}
