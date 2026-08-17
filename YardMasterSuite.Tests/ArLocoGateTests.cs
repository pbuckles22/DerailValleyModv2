using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (3.2 Smoke B): own-loco marker hides while boarded.
/// </summary>
public class ArLocoGateTests
{
    [Fact]
    public void Boarded_hides_own_loco_marker()
    {
        Assert.False(ArLocoGate.ShouldShow(hasLoco: true, playerIsOnThatLoco: true));
    }

    [Fact]
    public void On_foot_shows_last_loco_marker()
    {
        Assert.True(ArLocoGate.ShouldShow(hasLoco: true, playerIsOnThatLoco: false));
    }

    [Fact]
    public void No_last_loco_hides_marker()
    {
        Assert.False(ArLocoGate.ShouldShow(hasLoco: false, playerIsOnThatLoco: false));
    }

    [Fact]
    public void Board_loco_emits_T2_ar_loco_dash()
    {
        var onFoot = new ArOverlaySnapshot(
            ArMarkerPlace.OnObject, ArMarkerPlace.Edge, ArMarkerPlace.Hidden);
        var boarded = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.Edge, ArMarkerPlace.Hidden);

        Assert.Equal(
            "T2 ar change: loco=— office=edge pin=—",
            ArTelemetry.NextLog(onFoot, in boarded));
    }
}
