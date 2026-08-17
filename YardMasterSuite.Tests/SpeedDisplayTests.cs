using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedDisplayTests
{
    [Fact]
    public void FormatFromMetersPerSecond_rounds_kmh()
    {
        Assert.Equal("Speed 36 km/h", SpeedDisplay.FormatFromMetersPerSecond(10f));
        Assert.Equal("— Speed", SpeedDisplay.FormatFromMetersPerSecond(null));
    }

    [Fact]
    public void FormatFromKmh_uses_whole_numbers()
    {
        Assert.Equal("Speed 60 km/h", SpeedDisplay.FormatFromKmh(60));
        Assert.Equal("— Speed", SpeedDisplay.FormatFromKmh(null));
    }
}

public class SpeedTelemetryTests
{
    [Fact]
    public void Observe_publishes_on_rounded_kmh_change_only()
    {
        var cache = default(SpeedCache);
        Assert.True(SpeedTelemetry.Observe(10f, ref cache, out var first));
        Assert.Equal(36, first.Kmh);
        Assert.False(SpeedTelemetry.Observe(10.05f, ref cache, out _));
        Assert.True(SpeedTelemetry.Observe(11.2f, ref cache, out var second));
        Assert.Equal(40, second.Kmh);
    }

    [Fact]
    public void Reset_clears_seeded_state()
    {
        var cache = default(SpeedCache);
        SpeedTelemetry.Observe(5f, ref cache, out _);
        SpeedTelemetry.Reset(ref cache);
        Assert.False(cache.Seeded);
    }
}
