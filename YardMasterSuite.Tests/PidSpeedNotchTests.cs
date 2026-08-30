using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PidSpeedNotchTests
{
    [Fact]
    public void Smoke_9_1_8_two_elevenths_is_not_hud_18()
    {
        Assert.False(PidSpeedNotch.IsExact(2f / 11f));
        Assert.False(PidSpeedNotch.IsExact(3f / 11f));
        Assert.True(PidSpeedNotch.IsExact(0.09f));
        Assert.True(PidSpeedNotch.IsExact(0.18f));
        Assert.True(PidSpeedNotch.IsExact(0.27f));
        Assert.Equal(0.18f, PidSpeedNotch.Snap(2f / 11f), 3);
        Assert.Equal(0.27f, PidSpeedNotch.Snap(3f / 11f), 3);
        Assert.Equal(0.27f, PidSpeedNotch.Hud(3f / 11f), 3);
        Assert.Equal(0.18f, PidSpeedNotch.Hud(2f / 11f), 3);
        Assert.Equal(0.09f, PidSpeedNotch.FromNotch(1), 3);
        Assert.Equal(0.18f, PidSpeedNotch.FromNotch(2), 3);
        Assert.Equal(0.27f, PidSpeedNotch.FromNotch(3), 3);
    }

    [Fact]
    public void Smoke_9_1_8_cab_raw_09_writes_hud_18_not_two_elevenths()
    {
        var throttle = 0.09f;
        var independent = 0f;
        var cmd = new PidSpeedCommand(
            active: true,
            targetKmh: 25f,
            desiredThrottle: 2f / 11f,
            desiredIndependent: 0f,
            desiredReverser: PidSpeedGear.ReverseValue,
            gearPending: false,
            desiredTrain: 0f);
        PidSpeedCab.Apply(cmd, wantThrottle: true, ref throttle, ref independent);
        Assert.Equal(0.18f, throttle, 3);
        Assert.True(PidSpeedNotch.IsExact(throttle));
    }

    [Fact]
    public void Smoke_9_1_8_overspeed_indy_is_hud_27_not_three_elevenths()
    {
        var throttle = 0.09f;
        var independent = 0f;
        var cmd = new PidSpeedCommand(
            active: true,
            targetKmh: 25f,
            desiredThrottle: 0f,
            desiredIndependent: 3f / 11f,
            desiredReverser: PidSpeedGear.ReverseValue,
            gearPending: false,
            desiredTrain: 0f);
        PidSpeedCab.Apply(cmd, wantThrottle: false, ref throttle, ref independent);
        Assert.Equal(0.27f, independent, 3);
        Assert.Equal(0f, throttle, 3);
    }

    [Fact]
    public void Smoke_9_1_thr_12_snaps_to_first_9_notch()
    {
        Assert.Equal(PidSpeedNotch.Step, PidSpeedNotch.ApplyExpander(0.125f, 0f, firstPunchFromZero: true), 3);
        Assert.False(PidSpeedNotch.IsExact(0.125f));
        Assert.True(PidSpeedNotch.IsExact(PidSpeedNotch.Step));
    }

    [Fact]
    public void Smoke_9_1_off_grid_two_elevenths_stays_on_9()
    {
        var first = PidSpeedNotch.ApplyExpander(0.125f, 0f, firstPunchFromZero: true);
        Assert.Equal(0.09f, first, 3);
        var stuck = PidSpeedNotch.ApplyExpander(2f / 11f, first, firstPunchFromZero: true);
        Assert.Equal(0.09f, stuck, 3);
    }

    [Fact]
    public void Smoke_9_1_hud_18_leaves_9()
    {
        Assert.Equal(0.18f, PidSpeedNotch.ApplyExpander(0.18f, 0.09f, firstPunchFromZero: true), 3);
    }

    [Fact]
    public void Smoke_9_1_exact_second_notch_leaves_9()
    {
        var onFirst = PidSpeedNotch.Step;
        var second = PidSpeedNotch.Step * 2f;
        Assert.Equal(second, PidSpeedNotch.ApplyExpander(second, onFirst, firstPunchFromZero: true), 3);
    }

    [Fact]
    public void Smoke_9_1_overspeed_022_indy_stays_zero_off_grid()
    {
        Assert.Equal(0f, PidSpeedNotch.ApplyExpander(0.22f, 0f, firstPunchFromZero: false));
    }
}
