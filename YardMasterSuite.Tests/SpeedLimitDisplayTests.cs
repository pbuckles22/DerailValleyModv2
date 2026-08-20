using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_and_whole_kmh()
    {
        Assert.Equal("— Limit", SpeedLimitDisplay.Format(null));
        Assert.Equal("Limit 60", SpeedLimitDisplay.Format(60f));
        Assert.Equal("Limit 10", SpeedLimitDisplay.Format(9.6f));
    }

    [Fact]
    public void FormatHud_yellow_from_ten_below_through_five_over()
    {
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHud(49f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.WarningColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(50f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.CriticalColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(66f, 60f));
    }

    [Fact]
    public void FormatHud_placeholder_when_limit_unknown()
    {
        Assert.Equal("— Limit", SpeedLimitDisplay.FormatHud(40f, null));
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHud(null, 60f));
    }

    [Fact]
    public void Smoke_usable_loco_without_posted_is_limit_120_auth_default()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: true, postedKmh: null);
        Assert.Equal(SpeedLimitState.UnrestrictedKmh, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Default, snap.Authority);
        Assert.Equal("Limit 120", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.Equal("T2 limit init: 120 auth=default", SpeedLimitTelemetry.FormatInit(in snap));
    }

    [Fact]
    public void Smoke_curve_cannot_move_hud_limit_without_posted_board()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: true, postedKmh: null);
        Assert.Equal(120f, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Default, snap.Authority);
        Assert.DoesNotContain("Next", SpeedLimitDisplay.FormatHudOrEmpty(10f, snap.LimitKmh));
    }

    [Fact]
    public void Smoke_posted_board_behind_is_limit_60_auth_posted()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: true, postedKmh: 60f);
        Assert.Equal(60f, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, snap.Authority);
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.DoesNotContain("Next", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.Equal("T2 limit init: 60 auth=posted", SpeedLimitTelemetry.FormatInit(in snap));
    }

    [Fact]
    public void Smoke_unboarded_omits_limit_even_if_posted_sticky()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: false, postedKmh: 60f);
        Assert.Null(snap.LimitKmh);
        Assert.Equal(LimitAuthority.None, snap.Authority);
        Assert.Equal(string.Empty, SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
    }

    [Fact]
    public void Smoke_cab_limit_omits_next_distance()
    {
        var label = SpeedLimitDisplay.FormatHudOrEmpty(20f, 40f);
        Assert.Equal("Limit 40", label);
        Assert.DoesNotContain("Next", label);
        Assert.DoesNotContain("— Limit", label);
    }

    [Fact]
    public void Observe_does_not_allocate_when_posted_limit_holds()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(60f, LimitAuthority.Posted);
        SpeedLimitTelemetry.Observe(snap, ref cache, out _);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            SpeedLimitTelemetry.Observe(snap, ref cache, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void FormatHudOrEmpty_yellow_and_red_bands()
    {
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHudOrEmpty(49f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.WarningColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHudOrEmpty(50f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.CriticalColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHudOrEmpty(66f, 60f));
    }
}

public class SpeedLimitTelemetryTests
{
    [Fact]
    public void Observe_dedupes_same_limit()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(60f, LimitAuthority.Posted);
        Assert.True(SpeedLimitTelemetry.Observe(snap, ref cache, out _));
        Assert.False(SpeedLimitTelemetry.Observe(snap, ref cache, out _));
    }

    [Fact]
    public void Observe_does_not_allocate_when_limit_holds()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(120f, LimitAuthority.Default);
        SpeedLimitTelemetry.Observe(snap, ref cache, out _);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            SpeedLimitTelemetry.Observe(snap, ref cache, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
