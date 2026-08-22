using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.15): Home mark shows a PIN AR slot; standing on it hides the icon.
/// </summary>
public class ArPinGateTests
{
    [Fact]
    public void Smoke_unmarked_hides_pin()
    {
        Assert.False(ArPinGate.ShouldShow(hasMark: false, atPin: false));
    }

    [Fact]
    public void Smoke_home_mark_away_shows_pin()
    {
        Assert.False(ArPinGate.IsAtPin(0f, 0f, 40f, 0f));
        Assert.True(ArPinGate.ShouldShow(hasMark: true, atPin: false));
    }

    [Fact]
    public void Smoke_standing_on_mark_hides_pin()
    {
        Assert.True(ArPinGate.IsAtPin(100f, 200f, 102f, 201f));
        Assert.False(ArPinGate.ShouldShow(hasMark: true, atPin: true));
    }

    [Fact]
    public void Smoke_standing_within_8m_hides_pin()
    {
        Assert.Equal(8f, ArPinGate.HideRadiusMeters);
        Assert.True(ArPinGate.IsAtPin(0f, 0f, 8f, 0f));
        Assert.False(ArPinGate.IsAtPin(0f, 0f, 8.01f, 0f));
    }

    [Fact]
    public void Smoke_home_pin_emits_T2_ar_pin_place()
    {
        var hidden = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.Hidden, ArMarkerPlace.Hidden);
        var shown = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.Hidden, ArMarkerPlace.OnObject);

        Assert.Equal(
            "T2 ar change: loco=— office=— pin=object",
            ArTelemetry.NextLog(hidden, in shown));
    }
}
