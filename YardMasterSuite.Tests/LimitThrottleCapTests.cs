using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.5 consist safety: idle + air when Derail ≥65 %. Speed is not a gate.</summary>
public class LimitThrottleCapTests
{
    [Fact]
    public void Smoke_60kmh_derail_40_does_not_trip()
    {
        Assert.False(LimitThrottleCap.ShouldIntervene(40f));
        Assert.Equal(0f, LimitThrottleCap.IndependentTarget(40f));
        Assert.Equal(
            1f,
            LimitThrottleCap.ComputeDesiredThrottle(1f, intervening: false, deltaTime: 1f));
    }

    [Fact]
    public void Smoke_hud_120_next_40_derail_44_does_not_cap()
    {
        Assert.False(LimitThrottleCap.ShouldIntervene(44f));
        Assert.False(LimitThrottleCap.ShouldIntervene(64f));
        Assert.False(LimitThrottleCap.ShouldIntervene(DerailRiskDisplay.WarningThresholdPercent));
    }

    [Fact]
    public void Smoke_no_derail_passthrough_even_when_fast()
    {
        Assert.False(LimitThrottleCap.ShouldIntervene(0f));
        Assert.Equal(
            1f,
            LimitThrottleCap.ComputeDesiredThrottle(1f, intervening: false, deltaTime: 1f));
    }

    [Fact]
    public void Smoke_under_intervene_green_derail_passthrough()
    {
        Assert.False(LimitThrottleCap.ShouldIntervene(5f));
        Assert.Equal(
            0.95f,
            LimitThrottleCap.ComputeDesiredThrottle(0.95f, intervening: false, deltaTime: 1f));
        Assert.Equal(
            0.10f,
            LimitThrottleCap.ComputeDesiredBrake(0.10f, target: 0.50f, intervening: false, deltaTime: 1f));
    }

    [Fact]
    public void Smoke_derail_65_idles_throttle_and_raises_air()
    {
        Assert.True(LimitThrottleCap.ShouldIntervene(65f));
        Assert.Equal(
            LimitThrottleCap.DerailWarnIndependent,
            LimitThrottleCap.IndependentTarget(65f));
        Assert.Equal(
            LimitThrottleCap.DerailWarnTrain,
            LimitThrottleCap.TrainTarget(65f));

        var throttle = LimitThrottleCap.ComputeDesiredThrottle(
            0.64f, intervening: true, deltaTime: 1f, applyPerSecond: 0.20f);
        Assert.Equal(0.44f, throttle, precision: 3);

        var indy = LimitThrottleCap.ComputeDesiredBrake(
            0f,
            LimitThrottleCap.IndependentTarget(65f),
            intervening: true,
            deltaTime: 1f,
            applyPerSecond: 0.20f);
        Assert.InRange(indy, 0.19f, 0.21f);
        Assert.True(indy > 0f);
    }

    [Fact]
    public void Smoke_throttle_still_moving_keeps_intervening()
    {
        Assert.True(LimitThrottleCap.ShouldIntervene(65f));
        Assert.True(
            LimitThrottleCap.NeedsWork(
                throttle: 0.33f,
                independent: 0f,
                train: 0f,
                indyTarget: LimitThrottleCap.DerailWarnIndependent,
                trainTarget: LimitThrottleCap.DerailWarnTrain));
        Assert.Equal(
            0f,
            LimitThrottleCap.ComputeDesiredThrottle(0.33f, intervening: true, deltaTime: 0f));
    }

    [Fact]
    public void Smoke_derail_65_under_limit_applies_air()
    {
        Assert.True(LimitThrottleCap.ShouldIntervene(65f));
        Assert.Equal(
            LimitThrottleCap.DerailWarnIndependent,
            LimitThrottleCap.IndependentTarget(65f));
        Assert.False(LimitThrottleCap.ShouldIntervene(14f));
    }

    [Fact]
    public void Smoke_derail_red_is_stricter_than_intervene_yellow()
    {
        Assert.True(
            LimitThrottleCap.IndependentTarget(DerailRiskDisplay.CriticalThresholdPercent)
            > LimitThrottleCap.IndependentTarget(LimitThrottleCap.DerailIntervenePercent));
        Assert.True(
            LimitThrottleCap.TrainTarget(DerailRiskDisplay.CriticalThresholdPercent)
            > LimitThrottleCap.TrainTarget(LimitThrottleCap.DerailIntervenePercent));
        Assert.Equal(
            LimitThrottleCap.CritApplyPerSecond,
            LimitThrottleCap.ApplyPerSecond(95f));
        Assert.Equal(
            LimitThrottleCap.DefaultApplyPerSecond,
            LimitThrottleCap.ApplyPerSecond(65f));
    }

    [Fact]
    public void ComputeDesiredBrake_never_dumps_air()
    {
        Assert.Equal(
            0.90f,
            LimitThrottleCap.ComputeDesiredBrake(
                0.90f, target: 0.50f, intervening: true, deltaTime: 1f));
    }

    [Fact]
    public void ComputeDesiredThrottle_never_raises()
    {
        Assert.Equal(
            0f,
            LimitThrottleCap.ComputeDesiredThrottle(0.10f, intervening: true, deltaTime: 1f));
        Assert.Equal(
            0.10f,
            LimitThrottleCap.ComputeDesiredThrottle(0.10f, intervening: false, deltaTime: 1f));
    }

    [Fact]
    public void ComputeDesired_passthrough_when_not_intervening()
    {
        Assert.Equal(0.85f, LimitThrottleCap.ComputeDesiredThrottle(0.85f, intervening: false));
        Assert.Equal(0.20f, LimitThrottleCap.ComputeDesiredBrake(0.20f, 1f, intervening: false, deltaTime: 1f));
    }

    [Fact]
    public void NeedsWork_false_when_idle_and_air_at_target()
    {
        Assert.False(
            LimitThrottleCap.NeedsWork(
                throttle: 0f,
                independent: 0.80f,
                train: 0.60f,
                indyTarget: 0.80f,
                trainTarget: 0.60f));
        Assert.True(
            LimitThrottleCap.ShouldHold(
                intervening: true,
                needsWork: false));
    }

    [Fact]
    public void Smoke_hold_while_over_does_not_count_as_release()
    {
        Assert.False(LimitThrottleCap.IsSafeToWrite(
            hasUsableLoco: true,
            controlsPresent: true,
            controlNotBlocked: true,
            intervening: true,
            needsWork: false));
        Assert.True(LimitThrottleCap.ShouldHold(intervening: true, needsWork: false));
        Assert.False(LimitThrottleCap.ShouldHold(intervening: false, needsWork: false));
    }

    [Fact]
    public void IsSafeToWrite_requires_all_predicates()
    {
        Assert.True(LimitThrottleCap.IsSafeToWrite(true, true, true, intervening: true, needsWork: true));
        Assert.False(LimitThrottleCap.IsSafeToWrite(false, true, true, true, true));
        Assert.False(LimitThrottleCap.IsSafeToWrite(true, false, true, true, true));
        Assert.False(LimitThrottleCap.IsSafeToWrite(true, true, false, true, true));
        Assert.False(LimitThrottleCap.IsSafeToWrite(true, true, true, false, true));
        Assert.False(LimitThrottleCap.IsSafeToWrite(true, true, true, true, false));
    }

    [Fact]
    public void Smoke_derail_65_three_gate_applies_soft_write()
    {
        Assert.True(LimitThrottleCap.ShouldIntervene(65f));
        var desired = LimitThrottleCap.ComputeDesiredThrottle(0.90f, intervening: true, deltaTime: 1f);
        Assert.True(LimitThrottleCap.ShouldLower(0.90f, desired));

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
        Assert.InRange(desired, 0.69f, 0.71f);
    }

    [Fact]
    public void ComputeDesired_does_not_allocate_on_hot_path()
    {
        LimitThrottleCap.IndependentTarget(65f);
        LimitThrottleCap.ComputeDesiredThrottle(0.9f, intervening: true, deltaTime: 0.02f);
        LimitThrottleCap.ComputeDesiredBrake(0.1f, 0.8f, intervening: true, deltaTime: 0.02f);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            LimitThrottleCap.ShouldIntervene(65f);
            LimitThrottleCap.IndependentTarget(65f);
            LimitThrottleCap.TrainTarget(65f);
            LimitThrottleCap.ComputeDesiredThrottle(0.9f, intervening: true, deltaTime: 0.02f);
            LimitThrottleCap.ComputeDesiredBrake(0.1f, 0.8f, intervening: true, deltaTime: 0.02f);
            LimitThrottleCap.CueForLevers(true, 0.9f, 0.1f, 0.1f, 0.8f, 0.6f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Smoke_limit_gov_pause_overlay_aborts_without_write()
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
    public void Smoke_sw_exit_flash_throttle_and_both_brakes_while_moving()
    {
        var cue = LimitThrottleCap.CueForLevers(
            intervening: true,
            throttle: 0.54f,
            independent: 0f,
            train: 0f,
            indyTarget: LimitThrottleCap.DerailWarnIndependent,
            trainTarget: LimitThrottleCap.DerailWarnTrain);
        Assert.True(cue.Throttle);
        Assert.True(cue.Independent);
        Assert.True(cue.TrainBrake);
        Assert.True(cue.Any);
    }

    [Fact]
    public void Smoke_idle_throttle_still_flashes_brakes_until_air_target()
    {
        var cue = LimitThrottleCap.CueForLevers(
            intervening: true,
            throttle: 0f,
            independent: 0.50f,
            train: 0.45f,
            indyTarget: LimitThrottleCap.DerailWarnIndependent,
            trainTarget: LimitThrottleCap.DerailWarnTrain);
        Assert.False(cue.Throttle);
        Assert.True(cue.Independent);
        Assert.False(cue.TrainBrake);
    }

    [Fact]
    public void CueForLevers_none_when_not_intervening_or_at_hold()
    {
        Assert.False(
            LimitThrottleCap.CueForLevers(
                intervening: false,
                throttle: 0.90f,
                independent: 0f,
                train: 0f,
                indyTarget: 0.80f,
                trainTarget: 0.60f).Any);
        Assert.False(
            LimitThrottleCap.CueForLevers(
                intervening: true,
                throttle: 0f,
                independent: 0.80f,
                train: 0.60f,
                indyTarget: 0.80f,
                trainTarget: 0.60f).Any);
    }
}
