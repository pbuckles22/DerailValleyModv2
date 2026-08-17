using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (planned 3.2 Smoke A): house marker hides on the office apron.
/// </summary>
public class ArOfficeGateTests
{
    [Fact]
    public void Walk_to_office_door_hides_house_marker()
    {
        Assert.True(ArOfficeGate.IsAtOffice(
            officeX: 100f, officeZ: 200f, playerX: 105f, playerZ: 204f));
        Assert.False(ArOfficeGate.ShouldShow(hasInZoneStation: true, atOffice: true));
    }

    [Fact]
    public void Yard_walk_outside_apron_shows_office_marker()
    {
        Assert.False(ArOfficeGate.IsAtOffice(
            officeX: 0f, officeZ: 0f, playerX: 50f, playerZ: 0f));
        Assert.True(ArOfficeGate.ShouldShow(hasInZoneStation: true, atOffice: false));
    }

    [Fact]
    public void No_in_zone_station_hides_office()
    {
        Assert.False(ArOfficeGate.ShouldShow(hasInZoneStation: false, atOffice: false));
    }

    [Fact]
    public void Walk_to_office_door_emits_T2_ar_office_dash()
    {
        var shown = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.OnObject, ArMarkerPlace.Hidden);
        var hidden = default(ArOverlaySnapshot);

        Assert.Equal(
            "T2 ar change: loco=— office=— pin=—",
            ArTelemetry.NextLog(shown, in hidden));
    }
}
