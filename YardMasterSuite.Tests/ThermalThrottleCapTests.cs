using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.2 thermal governor: Warning 75% / Critical 55% soft-roll.</summary>
public class ThermalThrottleCapTests
{
    private const float MaxCritical = ThermalThrottleCap.DefaultMaxWhenCritical;
    private const float MaxWarning = ThermalThrottleCap.DefaultMaxWhenWarning;

    [Fact]
    public void CeilingForBand_warning_milder_than_critical()
    {
        Assert.Equal(MaxWarning, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.Warning));
        Assert.Equal(MaxCritical, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.Critical));
        Assert.Equal(MaxCritical, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.WarningAndCritical));
        Assert.Equal(1f, ThermalThrottleCap.CeilingForBand(MotorCabTempBand.Nominal));
        Assert.Equal(1f, ThermalThrottleCap.CeilingForBand(null));
    }

    [Fact]
    public void Smoke_warning_hot_soft_rolls_throttle_toward_75()
    {
        var stepped = ThermalThrottleCap.ComputeDesiredThrottle(
            0.90f,
            motorsHot: true,
            MaxWarning,
            deltaTime: 1f,
            rollbackPerSecond: 0.05f);
        Assert.Equal(0.85f, stepped, precision: 3);
        Assert.Equal(
            MaxWarning,
            ThermalThrottleCap.ComputeDesiredThrottle(MaxWarning, motorsHot: true, MaxWarning, deltaTime: 1f));
    }

    [Fact]
    public void Smoke_critical_hot_soft_rolls_throttle_toward_55()
    {
        var stepped = ThermalThrottleCap.ComputeDesiredThrottle(
            0.70f,
            motorsHot: true,
            MaxCritical,
            deltaTime: 1f,
            rollbackPerSecond: 0.05f);
        Assert.Equal(0.65f, stepped, precision: 3);
        Assert.Equal(
            MaxCritical,
            ThermalThrottleCap.ComputeDesiredThrottle(0.56f, motorsHot: true, MaxCritical, deltaTime: 1f));
    }

    [Fact]
    public void Smoke_hot_with_null_band_uses_critical_ceiling()
    {
        Assert.Equal(
            MaxCritical,
            ThermalThrottleCap.CeilingWhenHot(motorsHot: true, band: null));
        Assert.Equal(
            MaxCritical,
            ThermalThrottleCap.CeilingWhenHot(motorsHot: true, MotorCabTempBand.Nominal));
        Assert.Equal(
            MaxWarning,
            ThermalThrottleCap.CeilingWhenHot(motorsHot: true, MotorCabTempBand.Warning));
        Assert.Equal(1f, ThermalThrottleCap.CeilingWhenHot(motorsHot: false, MotorCabTempBand.Warning));
    }

    [Fact]
    public void Smoke_tm_reset_dead_ceiling_is_zero()
    {
        Assert.Equal(0f, ThermalThrottleCap.CeilingForMotors(MotorStatus.Dead, band: null));
        Assert.Equal(1f, ThermalThrottleCap.CeilingForMotors(MotorStatus.Ok, band: null));
        Assert.Equal(
            MaxWarning,
            ThermalThrottleCap.CeilingForMotors(MotorStatus.Hot, MotorCabTempBand.Warning));
    }

    [Fact]
    public void ComputeDesired_passthrough_when_not_hot()
    {
        Assert.Equal(0.85f, ThermalThrottleCap.ComputeDesiredThrottle(0.85f, motorsHot: false, MaxCritical));
    }

    [Fact]
    public void ComputeDesired_hard_snaps_when_delta_zero()
    {
        Assert.Equal(MaxCritical, ThermalThrottleCap.ComputeDesiredThrottle(0.9f, motorsHot: true, MaxCritical));
    }

    [Fact]
    public void ComputeDesired_soft_roll_does_not_pass_ceiling()
    {
        Assert.Equal(
            MaxWarning,
            ThermalThrottleCap.ComputeDesiredThrottle(0.78f, motorsHot: true, MaxWarning, deltaTime: 1f, 0.05f));
    }

    [Fact]
    public void ComputeDesired_leaves_alone_when_hot_but_already_at_or_below_max()
    {
        Assert.Equal(0.25f, ThermalThrottleCap.ComputeDesiredThrottle(0.25f, motorsHot: true, MaxCritical));
        Assert.Equal(MaxCritical, ThermalThrottleCap.ComputeDesiredThrottle(MaxCritical, motorsHot: true, MaxCritical));
    }

    [Fact]
    public void ComputeDesired_never_raises_throttle()
    {
        Assert.Equal(0.1f, ThermalThrottleCap.ComputeDesiredThrottle(0.1f, motorsHot: true, maxWhenHot: 0.9f));
    }

    [Fact]
    public void ComputeDesired_clamps_inputs_to_unit_interval()
    {
        Assert.Equal(0f, ThermalThrottleCap.ComputeDesiredThrottle(-1f, motorsHot: false, MaxCritical));
        Assert.Equal(1f, ThermalThrottleCap.ComputeDesiredThrottle(2f, motorsHot: false, MaxCritical));
        Assert.Equal(1f, ThermalThrottleCap.ComputeDesiredThrottle(2f, motorsHot: true, maxWhenHot: 1.5f));
        Assert.Equal(0.4f, ThermalThrottleCap.ComputeDesiredThrottle(2f, motorsHot: true, maxWhenHot: 0.4f));
    }

    [Fact]
    public void ShouldSoftWrite_only_when_desired_is_lower()
    {
        Assert.True(ThermalThrottleCap.ShouldSoftWrite(0.8f, 0.4f));
        Assert.False(ThermalThrottleCap.ShouldSoftWrite(0.4f, 0.4f));
        Assert.False(ThermalThrottleCap.ShouldSoftWrite(0.3f, 0.4f));
    }

    [Fact]
    public void Smoke_cool_motors_are_not_safe_to_cap()
    {
        Assert.False(ThermalThrottleCap.IsSafeToCap(
            hasUsableLoco: true,
            controlsPresent: true,
            controlNotBlocked: true,
            motorsHot: false,
            currentAboveCap: true));
    }

    [Fact]
    public void IsSafeToCap_requires_all_predicates()
    {
        Assert.True(ThermalThrottleCap.IsSafeToCap(
            hasUsableLoco: true,
            controlsPresent: true,
            controlNotBlocked: true,
            motorsHot: true,
            currentAboveCap: true));

        Assert.False(ThermalThrottleCap.IsSafeToCap(false, true, true, true, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, false, true, true, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, true, false, true, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, true, true, false, true));
        Assert.False(ThermalThrottleCap.IsSafeToCap(true, true, true, true, false));
    }

    [Fact]
    public void Smoke_thermal_hot_above_cap_three_gate_applies_soft_write()
    {
        var current = 0.90f;
        var ceiling = ThermalThrottleCap.CeilingWhenHot(motorsHot: true, MotorCabTempBand.Warning);
        var desired = ThermalThrottleCap.ComputeDesiredThrottle(
            current, motorsHot: true, ceiling, deltaTime: 1f);
        Assert.True(ThermalThrottleCap.ShouldSoftWrite(current, desired));
        Assert.True(ThermalThrottleCap.IsSafeToCap(true, true, true, motorsHot: true, currentAboveCap: true));

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
        Assert.InRange(desired, 0.84f, 0.86f);
    }

    [Fact]
    public void ComputeDesired_does_not_allocate_on_hot_path()
    {
        ThermalThrottleCap.ComputeDesiredThrottle(0.9f, motorsHot: true, MaxWarning, deltaTime: 0.02f);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ThermalThrottleCap.ComputeDesiredThrottle(0.9f, motorsHot: true, MaxWarning, deltaTime: 0.02f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Smoke_thermal_pause_overlay_aborts_without_write()
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
}
