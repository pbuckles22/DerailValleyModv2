using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: look around updates the compass on 16-point change (story 3.1).
/// </summary>
public class HeadingTelemetryTests
{
    [Fact]
    public void Look_around_emits_T2_heading_on_point_change()
    {
        var cache = default(HeadingCache);
        var lastLogAt = -999f;

        Assert.True(HeadingTelemetry.Observe(0, ref cache));
        var init = HeadingTelemetry.NextLog(
            0, HeadingLogKind.Init, nowSeconds: 10f, ref lastLogAt);
        Assert.Equal("T2 heading init: N", init);

        Assert.True(HeadingTelemetry.Observe(2, ref cache));
        var change = HeadingTelemetry.NextLog(
            2, HeadingLogKind.Change, nowSeconds: 13f, ref lastLogAt);
        Assert.Equal("T2 heading change: NE", change);
        Assert.Equal(2, cache.PointIndex);
    }

    [Fact]
    public void Same_compass_point_is_silent()
    {
        var cache = default(HeadingCache);
        HeadingTelemetry.Observe(0, ref cache);

        Assert.False(HeadingTelemetry.Observe(0, ref cache));
    }

    [Fact]
    public void Rapid_look_throttles_T2_heading_change()
    {
        var cache = default(HeadingCache);
        var lastLogAt = -999f;
        HeadingTelemetry.Observe(0, ref cache);
        HeadingTelemetry.NextLog(0, HeadingLogKind.Init, 10f, ref lastLogAt);
        HeadingTelemetry.Observe(1, ref cache);

        var tooSoon = HeadingTelemetry.NextLog(
            1, HeadingLogKind.Change, nowSeconds: 11f, ref lastLogAt);

        Assert.Null(tooSoon);
        Assert.Equal(10f, lastLogAt);
    }
}

[Collection("YmsEventBus")]
public class HeadingBusTests : IDisposable
{
    public HeadingBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void Subscribe_then_raise_delivers_compass_index()
    {
        CompassHeading received = default;
        var calls = 0;
        void Handler(CompassHeading heading)
        {
            received = heading;
            calls++;
        }

        YmsEventBus.OnHeadingChanged += Handler;
        YmsEventBus.RaiseHeadingChanged(new CompassHeading(2));

        Assert.Equal(1, calls);
        Assert.Equal(2, received.PointIndex);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_heading_handler()
    {
        var calls = 0;
        void Handler(CompassHeading _) => calls++;

        YmsEventBus.OnHeadingChanged += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseHeadingChanged(new CompassHeading(0));

        Assert.Equal(0, calls);
    }
}
