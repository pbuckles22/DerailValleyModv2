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
}
