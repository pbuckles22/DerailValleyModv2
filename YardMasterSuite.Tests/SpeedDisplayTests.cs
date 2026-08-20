using System;
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

    [Fact]
    public void Smoke_cab_omits_dash_speed_when_unknown()
    {
        Assert.Equal(string.Empty, SpeedDisplay.FormatOrEmpty(null));
        Assert.Equal("Speed 0 km/h", SpeedDisplay.FormatOrEmpty(0));
        Assert.Equal("Speed 36 km/h", SpeedDisplay.FormatOrEmpty(36));
        Assert.DoesNotContain("— Speed", SpeedDisplay.FormatOrEmpty(0));
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

    [Fact]
    public void Smoke_cab_roll_publishes_speed_0_then_5()
    {
        var cache = default(SpeedCache);
        Assert.True(SpeedTelemetry.Observe(0f, ref cache, out var idle));
        Assert.Equal(0, idle.Kmh);
        Assert.Equal("T2 speed init: 0", SpeedTelemetry.FormatLog(idle.Kmh, wasSeeded: false));
        Assert.True(SpeedTelemetry.Observe(1.4f, ref cache, out var rolling));
        Assert.Equal(5, rolling.Kmh);
        Assert.Equal("T2 speed change: 5", SpeedTelemetry.FormatLog(rolling.Kmh, wasSeeded: true));
    }

    [Fact]
    public void Observe_does_not_allocate_when_kmh_holds()
    {
        var cache = default(SpeedCache);
        SpeedTelemetry.Observe(10f, ref cache, out _);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            SpeedTelemetry.Observe(10.05f, ref cache, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
