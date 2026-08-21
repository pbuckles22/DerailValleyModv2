using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// T2 station logs on zone / yard / bearing — not every meter (**6.12**).
/// </summary>
public class StationWaypointTelemetryTests
{
    [Fact]
    public void Smoke_enter_zone_emits_T2_station_init()
    {
        var cache = default(StationWaypointCache);
        Assert.True(StationWaypointTelemetry.Observe(
            inZone: true,
            yardId: "SM",
            stationX: 10f,
            stationZ: 20f,
            playerX: 110f,
            playerZ: 20f,
            atOffice: false,
            ref cache));
        Assert.Equal(
            "T2 station init: Station SM W",
            StationWaypointTelemetry.NextLog(null, StationWaypointTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_enter_cp_zone_emits_T2_station_init_ssw()
    {
        Offset(640f, 202.5, out var stationX, out var stationZ);
        var cache = default(StationWaypointCache);
        StationWaypointTelemetry.Observe(
            true, "CP", stationX, stationZ, 0f, 0f, false, ref cache);
        Assert.Equal(
            "T2 station init: Station CP SSW",
            StationWaypointTelemetry.NextLog(null, StationWaypointTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_leave_zone_emits_T2_station_change_cleared()
    {
        var cache = default(StationWaypointCache);
        StationWaypointTelemetry.Observe(
            true, "SM", 10f, 20f, 110f, 20f, false, ref cache);
        var prior = StationWaypointTelemetry.Snapshot(ref cache);

        Assert.True(StationWaypointTelemetry.Observe(
            false, null, null, null, 0f, 0f, false, ref cache));
        Assert.Equal(
            "T2 station change: — Station",
            StationWaypointTelemetry.NextLog(prior, StationWaypointTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_office_apron_emits_here()
    {
        var cache = default(StationWaypointCache);
        StationWaypointTelemetry.Observe(
            true, "SM", 50f, 60f, 58f, 60f, true, ref cache);
        Assert.Equal(
            "T2 station init: Station SM here",
            StationWaypointTelemetry.NextLog(null, StationWaypointTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_office_apron_emits_T2_station_change_here()
    {
        Offset(43f, 315.0, out var stationX, out var stationZ);
        var cache = default(StationWaypointCache);
        StationWaypointTelemetry.Observe(
            true, "CP", stationX, stationZ, 0f, 0f, false, ref cache);
        var prior = StationWaypointTelemetry.Snapshot(ref cache);

        Assert.True(StationWaypointTelemetry.Observe(
            true, "CP", stationX, stationZ, 0f, 0f, true, ref cache));
        Assert.Equal(
            "T2 station change: Station CP here",
            StationWaypointTelemetry.NextLog(prior, StationWaypointTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Same_bearing_and_meters_is_silent()
    {
        var cache = default(StationWaypointCache);
        StationWaypointTelemetry.Observe(
            true, "SM", 10f, 20f, 110f, 20f, false, ref cache);
        Assert.False(StationWaypointTelemetry.Observe(
            true, "SM", 10f, 20f, 110f, 20f, false, ref cache));
    }

    [Fact]
    public void Meter_step_publishes_hud_but_not_T2()
    {
        var cache = default(StationWaypointCache);
        StationWaypointTelemetry.Observe(
            true, "SM", 0f, 0f, 100f, 0f, false, ref cache);
        var prior = StationWaypointTelemetry.Snapshot(ref cache);

        Assert.True(StationWaypointTelemetry.Observe(
            true, "SM", 0f, 0f, 101f, 0f, false, ref cache));
        Assert.Null(StationWaypointTelemetry.NextLog(prior, StationWaypointTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Observe_does_not_allocate_when_buckets_hold()
    {
        var cache = default(StationWaypointCache);
        const string Yard = "SM";
        StationWaypointTelemetry.Observe(
            true, Yard, 10f, 20f, 110f, 20f, false, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            StationWaypointTelemetry.Observe(
                true, Yard, 10f, 20f, 110f, 20f, false, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static void Offset(float meters, double degrees, out float x, out float z)
    {
        var rad = degrees * Math.PI / 180.0;
        x = (float)(meters * Math.Sin(rad));
        z = (float)(meters * Math.Cos(rad));
    }
}
