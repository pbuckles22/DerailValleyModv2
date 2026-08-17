using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// A116 zone finder: sustained curve → governing limit + span. Micro-kinks ignored.
/// Story 4.3 ports this so 4.4 can feed a longer arc list without rewriting the math.
/// </summary>
public class SpeedLimitGeometryZonesTests
{
    [Fact]
    public void No_zone_when_arcs_empty()
    {
        var found = SpeedLimitGeometryZones.TryGoverningZone(
            new List<SpeedLimitGeometryZones.ArcSample>(),
            out _, out _, out _);
        Assert.False(found);
        Assert.Null(SpeedLimitGeometryZones.GoverningLimitKmh(
            new List<SpeedLimitGeometryZones.ArcSample>()));
    }

    [Fact]
    public void Single_sustained_zone_reports_its_own_span()
    {
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 110f, lengthMeters: 200f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(40f, limitKmh);
        Assert.Equal(0f, start);
        Assert.Equal(200f, end);
    }

    [Fact]
    public void Tightest_zone_wins_over_a_looser_one_and_reports_its_own_offset()
    {
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 500f, lengthMeters: 500f),
            new(radiusMeters: 80f, lengthMeters: 100f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(30f, limitKmh);
        Assert.Equal(500f, start);
        Assert.Equal(600f, end);
    }

    [Fact]
    public void Micro_kink_shorter_than_min_zone_length_is_ignored()
    {
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 500f, lengthMeters: 100f),
            new(radiusMeters: 40f, lengthMeters: 5f),
            new(radiusMeters: 500f, lengthMeters: 100f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(80f, limitKmh);
        Assert.Equal(0f, start);
        Assert.Equal(100f, end);
    }

    [Fact]
    public void Two_equally_tight_zones_report_the_earliest_one()
    {
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 80f, lengthMeters: 50f),
            new(radiusMeters: 500f, lengthMeters: 300f),
            new(radiusMeters: 80f, lengthMeters: 50f),
        };

        var found = SpeedLimitGeometryZones.TryGoverningZone(
            arcs, out var limitKmh, out var start, out var end);

        Assert.True(found);
        Assert.Equal(30f, limitKmh);
        Assert.Equal(0f, start);
        Assert.Equal(50f, end);
    }

    [Fact]
    public void Result_matches_governing_limit_kmh_for_the_same_arcs()
    {
        var arcs = new List<SpeedLimitGeometryZones.ArcSample>
        {
            new(radiusMeters: 300f, lengthMeters: 400f),
            new(radiusMeters: 60f, lengthMeters: 80f),
            new(radiusMeters: 900f, lengthMeters: 200f),
        };

        var expected = SpeedLimitGeometryZones.GoverningLimitKmh(arcs);
        var found = SpeedLimitGeometryZones.TryGoverningZone(arcs, out var limitKmh, out _, out _);

        Assert.True(found);
        Assert.Equal(expected, limitKmh);
    }
}
