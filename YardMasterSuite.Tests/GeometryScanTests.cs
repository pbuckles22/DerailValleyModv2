using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Story 4.3 smoke harvest: rescan only when the boarded loco enters a new
/// RailTrack segment. Same segment is silent (no bezier, no T2).
/// </summary>
public class GeometryScanTests
{
    private static List<SpeedLimitGeometryZones.ArcSample> Tight40Kmh()
    {
        return new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 110f, lengthMeters: 200f),
        };
    }

    [Fact]
    public void Menu_unboarded_before_any_track_is_silent()
    {
        var cache = default(GeometryScanCache);
        Assert.False(GeometryScan.ShouldRescan(0, in cache));
    }

    [Fact]
    public void First_segment_rescans_then_same_segment_is_silent()
    {
        var cache = default(GeometryScanCache);
        Assert.True(GeometryScan.ShouldRescan(12, in cache));

        var result = GeometryScan.Evaluate(12, Tight40Kmh());
        GeometryScan.Remember(result, ref cache);

        Assert.False(GeometryScan.ShouldRescan(12, in cache));
        Assert.Equal(12, cache.SegmentId);
        Assert.True(result.HasLimit);
        Assert.Equal(40f, result.LimitKmh);
    }

    [Fact]
    public void New_segment_rescans()
    {
        var cache = default(GeometryScanCache);
        GeometryScan.Remember(GeometryScan.Evaluate(12, Tight40Kmh()), ref cache);

        Assert.True(GeometryScan.ShouldRescan(99, in cache));
    }

    [Fact]
    public void Unboard_segment_zero_rescans_once_then_silent()
    {
        var cache = default(GeometryScanCache);
        GeometryScan.Remember(GeometryScan.Evaluate(12, Tight40Kmh()), ref cache);

        Assert.True(GeometryScan.ShouldRescan(0, in cache));
        GeometryScan.Remember(GeometryScan.Evaluate(0, Tight40Kmh()), ref cache);
        Assert.False(GeometryScan.ShouldRescan(0, in cache));
        Assert.Equal(0, cache.SegmentId);
    }

    [Fact]
    public void Evaluate_unboard_is_none_even_when_arcs_exist()
    {
        var result = GeometryScan.Evaluate(0, Tight40Kmh());
        Assert.Equal(0, result.SegmentId);
        Assert.False(result.HasLimit);
    }

    [Fact]
    public void Evaluate_empty_arcs_is_clear_limit()
    {
        var result = GeometryScan.Evaluate(
            7,
            new List<SpeedLimitGeometryZones.ArcSample>());
        Assert.Equal(7, result.SegmentId);
        Assert.False(result.HasLimit);
    }

    [Fact]
    public void Store_remembers_per_segment_so_reentry_skips_recompute()
    {
        var store = new GeometrySegmentStore();
        var first = GeometryScan.Evaluate(12, Tight40Kmh());
        store.Remember(first);

        Assert.True(store.TryGet(12, out var cached));
        Assert.Equal(40f, cached.LimitKmh);
        Assert.False(store.TryGet(0, out _));
        Assert.False(store.TryGet(99, out _));

        store.Clear();
        Assert.False(store.TryGet(12, out _));
    }

    [Fact]
    public void Format_segment_dash_when_unboarded()
    {
        Assert.Equal("T2 geometry: segment=—", GeometryTelemetry.Format(GeometryScanResult.None));
    }

    [Fact]
    public void Format_limit_dash_when_no_sustained_zone()
    {
        var result = new GeometryScanResult(44, hasLimit: false, 0f, 0f, 0f);
        Assert.Equal("T2 geometry: segment=44 limit=—", GeometryTelemetry.Format(result));
    }

    [Fact]
    public void Format_limit_and_span_when_zone_found()
    {
        var result = new GeometryScanResult(44, hasLimit: true, 40f, 0f, 200f);
        Assert.Equal("T2 geometry: segment=44 limit=40 start=0 end=200", GeometryTelemetry.Format(result));
    }
}

[Collection("YmsEventBus")]
public class GeometryScanBusTests : IDisposable
{
    public GeometryScanBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void Subscribe_then_raise_delivers_geometry_scan()
    {
        GeometryScanResult received = default;
        var calls = 0;
        void Handler(GeometryScanResult item)
        {
            received = item;
            calls++;
        }

        YmsEventBus.OnGeometryScan += Handler;
        YmsEventBus.RaiseGeometryScan(new GeometryScanResult(8, true, 30f, 10f, 80f));

        Assert.Equal(1, calls);
        Assert.Equal(8, received.SegmentId);
        Assert.Equal(30f, received.LimitKmh);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_geometry_scan_handler()
    {
        var calls = 0;
        YmsEventBus.OnGeometryScan += _ => calls++;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseGeometryScan(new GeometryScanResult(1, false, 0f, 0f, 0f));

        Assert.Equal(0, calls);
    }
}
