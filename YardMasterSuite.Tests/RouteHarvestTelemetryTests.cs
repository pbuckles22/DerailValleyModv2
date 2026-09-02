using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 13.1.15 harvest: km-bucket dest remaining and dest-yard-behind (desk open or closed).
/// </summary>
public class RouteHarvestTelemetryTests
{
    [Fact]
    public void Smoke_13_1_15_remain_emits_on_dest_set()
    {
        var cache = default(RouteRemainLogCache);
        Assert.Equal(
            "T2 route: rem=4214m dest=SW-C1O",
            RouteHarvestTelemetry.NextRemain(4214f, "SW-C1O", ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_remain_same_km_bucket_is_silent()
    {
        var cache = default(RouteRemainLogCache);
        Assert.NotNull(RouteHarvestTelemetry.NextRemain(4214f, "SW-C1O", ref cache));
        Assert.Null(RouteHarvestTelemetry.NextRemain(4000f, "SW-C1O", ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_remain_km_bucket_change_emits()
    {
        var cache = default(RouteRemainLogCache);
        Assert.Equal(
            "T2 route: rem=4214m dest=SW-C1O",
            RouteHarvestTelemetry.NextRemain(4214f, "SW-C1O", ref cache));
        Assert.Equal(
            "T2 route: rem=2900m dest=SW-C1O",
            RouteHarvestTelemetry.NextRemain(2900f, "SW-C1O", ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_remain_clears_when_dest_drops()
    {
        var cache = default(RouteRemainLogCache);
        Assert.NotNull(RouteHarvestTelemetry.NextRemain(1500f, "GF-O5I", ref cache));
        Assert.Null(RouteHarvestTelemetry.NextRemain(null, null, ref cache));
    }

    [Fact]
    public void KmBucket_floors_to_whole_km()
    {
        Assert.Equal(4, RouteHarvestTelemetry.KmBucket(4214f));
        Assert.Equal(3, RouteHarvestTelemetry.KmBucket(3999f));
        Assert.Equal(0, RouteHarvestTelemetry.KmBucket(12f));
        Assert.Equal(-1, RouteHarvestTelemetry.KmBucket(-1f));
    }

    [Fact]
    public void Smoke_13_1_15_dest_yard_behind_when_other_yard_is_behind()
    {
        Assert.True(RouteHarvestTelemetry.IsDestYardBehind("GF", "SW", destTrackBehind: true));
        var cache = default(RouteDestYardBehindCache);
        Assert.Same(
            RouteHarvestTelemetry.DestYardBehind,
            RouteHarvestTelemetry.NextDestYardBehind(true, ref cache));
        Assert.Null(RouteHarvestTelemetry.NextDestYardBehind(true, ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_same_yard_dest_behind_is_not_dest_yard_behind()
    {
        Assert.False(RouteHarvestTelemetry.IsDestYardBehind("SW", "SW", destTrackBehind: true));
        Assert.False(RouteHarvestTelemetry.IsDestYardBehind("GF", "SW", destTrackBehind: false));
        Assert.False(RouteHarvestTelemetry.IsDestYardBehind(null, "SW", destTrackBehind: true));
    }

    [Fact]
    public void NextRemain_does_not_allocate_inside_km_bucket()
    {
        var cache = default(RouteRemainLogCache);
        RouteHarvestTelemetry.NextRemain(4214f, "SW-C1O", ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            RouteHarvestTelemetry.NextRemain(4100f, "SW-C1O", ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void NextDestYardBehind_interned_and_alloc_free_while_true()
    {
        var cache = default(RouteDestYardBehindCache);
        var first = RouteHarvestTelemetry.NextDestYardBehind(true, ref cache);
        Assert.Same(RouteHarvestTelemetry.DestYardBehind, first);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            RouteHarvestTelemetry.NextDestYardBehind(true, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
