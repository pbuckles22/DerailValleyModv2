using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.3 auto-brake: engine off soft-rolls air on + throttle idle; never auto-release on start.</summary>
public class AutoBrakeParkTests
{
    private const float Rate = AutoBrakePark.DefaultApplyPerSecond;

    [Fact]
    public void DetectEngineOffFallingEdge_only_on_to_off()
    {
        Assert.True(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: true, isEngineOn: false));
        Assert.False(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: false, isEngineOn: false));
        Assert.False(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: true, isEngineOn: true));
        Assert.False(AutoBrakePark.DetectEngineOffFallingEdge(wasEngineOn: false, isEngineOn: true));
    }

    [Fact]
    public void Smoke_already_off_does_not_start_apply()
    {
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(
                AutoBrakePhase.Idle,
                engineOffFallingEdge: false,
                engineOff: true,
                safe: true,
                sessionStillNeedsWork: true));
    }

    [Fact]
    public void Smoke_engine_start_does_not_auto_release()
    {
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(
                AutoBrakePhase.Idle,
                engineOffFallingEdge: false,
                engineOff: false,
                safe: true,
                sessionStillNeedsWork: true));
        Assert.Equal(1f, AutoBrakePark.ComputeDesiredBrake(1f, applying: false, deltaTime: 1f));
        Assert.Equal(0.4f, AutoBrakePark.ComputeDesiredBrake(0.4f, applying: false, deltaTime: 1f));
        Assert.False(AutoBrakePark.ShouldLower(1f, AutoBrakePark.ComputeDesiredBrake(1f, applying: false, 1f)));
    }

    [Fact]
    public void Smoke_engine_off_falling_edge_starts_apply()
    {
        Assert.Equal(
            AutoBrakePhase.Applying,
            AutoBrakePark.NextPhase(
                AutoBrakePhase.Idle,
                engineOffFallingEdge: true,
                engineOff: true,
                safe: true,
                sessionStillNeedsWork: true));
    }

    [Fact]
    public void Smoke_shutdown_at_speed_still_applies()
    {
        Assert.True(AutoBrakePark.IsSafeToApply(
            hasUsableLoco: true,
            controlsPresent: true,
            controlNotBlocked: true,
            engineOff: true,
            sessionNeedsWork: true));
        Assert.Equal(
            AutoBrakePhase.Applying,
            AutoBrakePark.NextPhase(
                AutoBrakePhase.Idle,
                engineOffFallingEdge: true,
                engineOff: true,
                safe: true,
                sessionStillNeedsWork: true));
        Assert.InRange(
            AutoBrakePark.ComputeDesiredBrake(0f, applying: true, deltaTime: 1f),
            0.19f,
            0.21f);
    }

    [Fact]
    public void Smoke_shutdown_soft_rolls_brakes_and_throttle()
    {
        Assert.Equal(
            0.5f + Rate,
            AutoBrakePark.ComputeDesiredBrake(0.5f, applying: true, deltaTime: 1f, Rate),
            precision: 3);
        Assert.Equal(
            0.6f - Rate,
            AutoBrakePark.ComputeDesiredThrottle(0.6f, applying: true, deltaTime: 1f, Rate),
            precision: 3);
        Assert.True(AutoBrakePark.ShouldRaise(0.5f, 0.5f + Rate));
        Assert.True(AutoBrakePark.ShouldLower(0.6f, 0.6f - Rate));
    }

    [Fact]
    public void BrakesNeedApply_when_either_below_full()
    {
        Assert.False(AutoBrakePark.BrakesNeedApply(1f, 1f));
        Assert.True(AutoBrakePark.BrakesNeedApply(0.9f, 1f));
        Assert.True(AutoBrakePark.BrakesNeedApply(1f, 0f));
    }

    [Fact]
    public void ThrottleNeedsIdle_when_above_zero()
    {
        Assert.False(AutoBrakePark.ThrottleNeedsIdle(0f));
        Assert.True(AutoBrakePark.ThrottleNeedsIdle(0.1f));
    }

    [Fact]
    public void SessionNeedsWork_brakes_or_throttle()
    {
        Assert.False(AutoBrakePark.SessionNeedsWork(1f, 1f, 0f));
        Assert.True(AutoBrakePark.SessionNeedsWork(0.5f, 1f, 0f));
        Assert.True(AutoBrakePark.SessionNeedsWork(1f, 1f, 0.2f));
    }

    [Fact]
    public void ComputeDesiredBrake_passthrough_when_not_applying()
    {
        Assert.Equal(0.4f, AutoBrakePark.ComputeDesiredBrake(0.4f, applying: false, deltaTime: 1f));
    }

    [Fact]
    public void ComputeDesiredBrake_hard_snaps_when_delta_zero()
    {
        Assert.Equal(1f, AutoBrakePark.ComputeDesiredBrake(0.2f, applying: true, deltaTime: 0f));
    }

    [Fact]
    public void ComputeDesiredBrake_does_not_pass_full()
    {
        Assert.Equal(1f, AutoBrakePark.ComputeDesiredBrake(0.95f, applying: true, deltaTime: 1f, Rate));
    }

    [Fact]
    public void ComputeDesiredThrottle_passthrough_when_not_applying()
    {
        Assert.Equal(0.6f, AutoBrakePark.ComputeDesiredThrottle(0.6f, applying: false, deltaTime: 1f));
    }

    [Fact]
    public void ComputeDesiredThrottle_hard_snaps_when_delta_zero()
    {
        Assert.Equal(0f, AutoBrakePark.ComputeDesiredThrottle(0.4f, applying: true, deltaTime: 0f));
    }

    [Fact]
    public void ComputeDesired_clamps_inputs_to_unit_interval()
    {
        Assert.Equal(0f, AutoBrakePark.ComputeDesiredBrake(-1f, applying: false, deltaTime: 1f));
        Assert.Equal(1f, AutoBrakePark.ComputeDesiredBrake(2f, applying: false, deltaTime: 1f));
        Assert.Equal(0f, AutoBrakePark.ComputeDesiredThrottle(-1f, applying: true, deltaTime: 1f));
        Assert.Equal(0.8f, AutoBrakePark.ComputeDesiredThrottle(2f, applying: true, deltaTime: 1f, 0.2f));
    }

    [Fact]
    public void ShouldRaise_and_ShouldLower()
    {
        Assert.True(AutoBrakePark.ShouldRaise(0.3f, 0.5f));
        Assert.False(AutoBrakePark.ShouldRaise(0.5f, 0.5f));
        Assert.True(AutoBrakePark.ShouldLower(0.5f, 0.3f));
        Assert.False(AutoBrakePark.ShouldLower(0.3f, 0.5f));
    }

    [Fact]
    public void IsSafeToApply_requires_all_predicates()
    {
        Assert.True(AutoBrakePark.IsSafeToApply(true, true, true, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(false, true, true, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, false, true, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, true, false, true, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, true, true, false, true));
        Assert.False(AutoBrakePark.IsSafeToApply(true, true, true, true, false));
    }

    [Fact]
    public void NextPhase_idle_starts_on_falling_edge_when_safe()
    {
        Assert.Equal(
            AutoBrakePhase.Applying,
            AutoBrakePark.NextPhase(AutoBrakePhase.Idle, true, true, true, true));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Idle, true, true, false, true));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Idle, false, true, true, true));
    }

    [Fact]
    public void NextPhase_applying_ends_when_done_or_engine_on()
    {
        Assert.Equal(
            AutoBrakePhase.Applying,
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, true, true, true));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, true, true, false));
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, false, true, true));
    }

    [Fact]
    public void Smoke_engine_on_while_applying_stops_without_releasing()
    {
        Assert.Equal(
            AutoBrakePhase.Idle,
            AutoBrakePark.NextPhase(
                AutoBrakePhase.Applying,
                engineOffFallingEdge: false,
                engineOff: false,
                safe: true,
                sessionStillNeedsWork: true));
        var held = AutoBrakePark.ComputeDesiredBrake(0.7f, applying: false, deltaTime: 1f);
        Assert.Equal(0.7f, held);
        Assert.False(AutoBrakePark.ShouldLower(0.7f, held));
    }

    [Fact]
    public void Smoke_shutdown_three_gate_applies_soft_write()
    {
        var currentTrain = 0.40f;
        var desired = AutoBrakePark.ComputeDesiredBrake(currentTrain, applying: true, deltaTime: 1f);
        Assert.True(AutoBrakePark.ShouldRaise(currentTrain, desired));
        Assert.True(AutoBrakePark.IsSafeToApply(true, true, true, engineOff: true, sessionNeedsWork: true));

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
        Assert.InRange(desired, 0.59f, 0.61f);
    }

    [Fact]
    public void Smoke_pause_overlay_aborts_without_write()
    {
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
    public void ComputeDesired_does_not_allocate_on_hot_path()
    {
        AutoBrakePark.ComputeDesiredBrake(0.5f, applying: true, deltaTime: 0.02f);
        AutoBrakePark.ComputeDesiredThrottle(0.5f, applying: true, deltaTime: 0.02f);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            AutoBrakePark.ComputeDesiredBrake(0.5f, applying: true, deltaTime: 0.02f);
            AutoBrakePark.ComputeDesiredThrottle(0.5f, applying: true, deltaTime: 0.02f);
            AutoBrakePark.NextPhase(AutoBrakePhase.Applying, false, true, true, true);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
