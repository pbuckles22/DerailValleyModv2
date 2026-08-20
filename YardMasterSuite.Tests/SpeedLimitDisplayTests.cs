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
    public void FromGeometry_maps_scan_to_limit_snapshot()
    {
        var scan = new GeometryScanResult(42, hasLimit: true, limitKmh: 40f, 0f, 100f);
        var snap = SpeedLimitState.FromGeometry(in scan);
        Assert.Equal(LimitAuthority.Geometry, snap.Authority);
        Assert.Equal(40f, snap.LimitKmh);
    }

    [Fact]
    public void Smoke_straight_track_geometry_is_limit_120()
    {
        var scan = new GeometryScanResult(7, hasLimit: false, 0f, 0f, 0f);
        var snap = SpeedLimitState.FromGeometry(in scan);
        Assert.Equal(SpeedLimitState.UnrestrictedKmh, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Geometry, snap.Authority);
        Assert.Equal("Limit 120", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
    }

    [Fact]
    public void Smoke_curve_geometry_is_limit_60()
    {
        var scan = new GeometryScanResult(8, hasLimit: true, limitKmh: 60f, 0f, 80f);
        var snap = SpeedLimitState.FromGeometry(in scan);
        Assert.Equal(60f, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Geometry, snap.Authority);
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.DoesNotContain("Next", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
    }

    [Fact]
    public void Smoke_unboarded_geometry_omits_limit_chip()
    {
        var none = GeometryScanResult.None;
        var snap = SpeedLimitState.FromGeometry(in none);
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
        var snap = new SpeedLimitSnapshot(60f, LimitAuthority.Geometry);
        Assert.True(SpeedLimitTelemetry.Observe(snap, ref cache, out _));
        Assert.False(SpeedLimitTelemetry.Observe(snap, ref cache, out _));
    }

    [Fact]
    public void Observe_does_not_allocate_when_limit_holds()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(120f, LimitAuthority.Geometry);
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
