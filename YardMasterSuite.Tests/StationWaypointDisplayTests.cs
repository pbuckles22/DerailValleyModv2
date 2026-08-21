using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: always-on in-zone Station chip (**6.12** / v1 4.6).
/// </summary>
public class StationWaypointDisplayTests
{
    [Fact]
    public void Smoke_outside_zone_omits_station_chip()
    {
        Assert.Null(StationWaypointDisplay.Format(
            inZone: false,
            yardId: "SM",
            stationX: 100f,
            stationZ: 200f,
            playerX: 0f,
            playerZ: 0f,
            atOffice: false));
    }

    [Fact]
    public void In_zone_without_player_is_placeholder()
    {
        Assert.Equal(
            "— Station",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "SM",
                stationX: 100f,
                stationZ: 200f,
                playerX: null,
                playerZ: null,
                atOffice: false));
    }

    [Fact]
    public void Smoke_in_zone_shows_bearing_and_meters_without_coords()
    {
        // Player east of office → walk west back to paperwork.
        Assert.Equal(
            "Station SM W 100m",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "SM",
                stationX: 10f,
                stationZ: 20f,
                playerX: 110f,
                playerZ: 20f,
                atOffice: false));
    }

    [Fact]
    public void Smoke_cab_drive_shows_station_cp_ssw_640m()
    {
        Offset(640f, 202.5, out var stationX, out var stationZ);
        Assert.Equal(
            "Station CP SSW 640m",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "CP",
                stationX: stationX,
                stationZ: stationZ,
                playerX: 0f,
                playerZ: 0f,
                atOffice: false));
    }

    [Fact]
    public void Smoke_office_apron_shows_station_here()
    {
        Assert.Equal(
            "Station HB here",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "HB",
                stationX: 50f,
                stationZ: 60f,
                playerX: 58f,
                playerZ: 60f,
                atOffice: true));
    }

    [Fact]
    public void Smoke_office_apron_shows_station_cp_here()
    {
        Assert.Equal(
            "Station CP here",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "CP",
                stationX: 50f,
                stationZ: 60f,
                playerX: 58f,
                playerZ: 60f,
                atOffice: true));
    }

    [Fact]
    public void Near_office_but_not_at_apron_still_shows_bearing()
    {
        Assert.Equal(
            "Station SM E 1m",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: "SM",
                stationX: 10f,
                stationZ: 20f,
                playerX: 9f,
                playerZ: 20f,
                atOffice: false));
    }

    [Fact]
    public void Missing_yard_uses_placeholder_id()
    {
        Assert.Equal(
            "Station — here",
            StationWaypointDisplay.Format(
                inZone: true,
                yardId: null,
                stationX: 1f,
                stationZ: 2f,
                playerX: 1f,
                playerZ: 2f,
                atOffice: true));
    }

    [Fact]
    public void Walk_point_at_office_is_here()
    {
        Assert.Equal("here", StationWaypointDisplay.TryGetWalkPoint(0f, 0f, 10f, 0f, atOffice: true));
    }

    [Fact]
    public void Walk_point_not_at_office_is_bearing()
    {
        Assert.Equal("W", StationWaypointDisplay.TryGetWalkPoint(0f, 0f, 10f, 0f, atOffice: false));
    }

    private static void Offset(float meters, double degrees, out float x, out float z)
    {
        var rad = degrees * Math.PI / 180.0;
        x = (float)(meters * Math.Sin(rad));
        z = (float)(meters * Math.Cos(rad));
    }
}
