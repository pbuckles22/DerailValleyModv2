using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Unity Three-Gate write intent for <b>9.1</b>. 2.9.1.5 cab: after
/// <c>thr-off</c> independent stayed 0 through <c>pid: gear</c> at 32 and
/// coasted to 55 — gear-pending skipped the indy raise.
/// </summary>
public class PidSpeedWriteTests
{
    [Fact]
    public void Smoke_9_1_quantize_overspeed_indy_is_exact_notch()
    {
        Assert.Equal(
            PidSpeedHold.OverspeedIndependent,
            PidSpeedWrite.Quantize(PidSpeedHold.OverspeedIndependent, 0f),
            4);
        Assert.Equal(0f, PidSpeedWrite.Quantize(0f, PidSpeedNotch.Step));
        Assert.Equal(
            PidSpeedNotch.Step * 2f,
            PidSpeedWrite.Quantize(0.18f, PidSpeedNotch.Step),
            4);
    }

    [Fact]
    public void Smoke_9_1_off_grid_022_snaps_to_second_notch_not_027()
    {
        Assert.False(PidSpeedNotch.IsExact(0.22f));
        Assert.Equal(PidSpeedNotch.Step * 2f, PidSpeedWrite.Quantize(0.22f, 0f), 3);
        Assert.NotEqual(PidSpeedHold.OverspeedIndependent, PidSpeedWrite.Quantize(0.22f, 0f), 3);
    }

    [Fact]
    public void Smoke_9_1_overspeed_write_raises_independent()
    {
        Assert.True(
            PidSpeedWrite.Independent(
                current: 0f,
                desired: PidSpeedHold.OverspeedIndependent,
                gearPending: false,
                brakePending: false));
    }

    [Fact]
    public void Smoke_9_1_gear_pending_overspeed_still_writes_independent()
    {
        Assert.True(
            PidSpeedWrite.Independent(
                current: 0f,
                desired: PidSpeedHold.OverspeedIndependent,
                gearPending: true,
                brakePending: false));
    }

    [Fact]
    public void Smoke_9_1_gear_pending_holds_full_air_without_dump()
    {
        Assert.False(
            PidSpeedWrite.Independent(
                current: 1f,
                desired: 1f,
                gearPending: true,
                brakePending: false));
        Assert.False(
            PidSpeedWrite.Independent(
                current: 1f,
                desired: 0f,
                gearPending: true,
                brakePending: false));
    }

    [Fact]
    public void Smoke_9_1_gear_pending_overspeed_idles_throttle()
    {
        Assert.True(
            PidSpeedWrite.Throttle(
                current: 0.09f,
                desired: 0f,
                gearPending: true,
                brakePending: false,
                wantThrottle: false));
    }

    [Fact]
    public void Smoke_9_1_11_brake_bleed_must_not_hud_round_back_to_full()
    {
        // Listener must MUOverride(raw ApproachBrake), not Hud — else 0.996→1.0.
        var bled = PidSpeedHold.ApproachBrake(1f, 0f, 0.02f);
        Assert.True(bled < 1f);
        Assert.True(bled > 0.99f);
        Assert.Equal(1f, PidSpeedNotch.Hud(bled), 3);
        Assert.NotEqual(bled, PidSpeedNotch.Hud(bled), 3);
    }

    [Fact]
    public void Smoke_9_1_10_cab_overspeed_leaves_thr_9_raises_indy_27()
    {
        var throttle = 0.09f;
        var independent = 0f;
        var state = default(PidSpeedState);
        var cmd = PidSpeedHold.Tick(
            new PidSpeedInput(
                0.02f,
                speedKmh: 26f,
                requestKmh: 25f,
                postedKmh: null,
                throttle,
                independent,
                armed: true,
                derailIntervening: false,
                thermalCeiling: 1f,
                reverser: PidSpeedGear.ReverseValue,
                legNeedsReverse: true),
            ref state);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.Equal(PidSpeedHold.OverspeedIndependent, cmd.DesiredIndependent);
        Assert.False(PidSpeedTelemetry.WantsThrottle(
            armed: true,
            cmd.GearPending,
            cmd.BrakePending,
            cmd.DesiredThrottle));
        PidSpeedCab.Apply(cmd, wantThrottle: false, ref throttle, ref independent);
        Assert.Equal(0f, throttle);
        Assert.Equal(0.27f, independent, 3);
        Assert.True(System.Math.Abs(throttle - 0.09f) > PidSpeedNotch.ExactEpsilon);
        Assert.True(System.Math.Abs(independent - 0f) > PidSpeedNotch.ExactEpsilon);
    }
}
