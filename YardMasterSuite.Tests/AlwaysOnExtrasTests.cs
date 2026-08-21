using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Always-on extras join: Marked · Station · Path · Clock (**6.12**).
/// </summary>
public class AlwaysOnExtrasTests
{
    [Fact]
    public void Smoke_home_mark_joins_marked_before_clock()
    {
        Assert.Equal(
            "Marked here" + MonitorHudLine.Separator + "Clock 14:30",
            AlwaysOnExtras.Join("Marked here", path: null, clock: "Clock 14:30"));
    }

    [Fact]
    public void Smoke_in_zone_joins_station_between_marked_and_clock()
    {
        Assert.Equal(
            "Marked here"
            + MonitorHudLine.Separator
            + "Station SM W 100m"
            + MonitorHudLine.Separator
            + "Clock 14:30",
            AlwaysOnExtras.Join(
                "Marked here",
                "Station SM W 100m",
                path: null,
                clock: "Clock 14:30"));
    }

    [Fact]
    public void Smoke_outside_zone_omits_station()
    {
        Assert.Equal(
            "Marked here" + MonitorHudLine.Separator + "Clock 14:30",
            AlwaysOnExtras.Join("Marked here", station: null, path: null, clock: "Clock 14:30"));
    }

    [Fact]
    public void Smoke_unmarked_omits_marked()
    {
        Assert.Equal("Clock 14:30", AlwaysOnExtras.Join(null, null, "Clock 14:30"));
    }

    [Fact]
    public void Smoke_end_dest_joins_path_ok()
    {
        Assert.Equal(
            "Marked here" + MonitorHudLine.Separator + "Path OK" + MonitorHudLine.Separator + "Clock 14:30",
            AlwaysOnExtras.Join("Marked here", "Path OK", "Clock 14:30"));
    }

    [Fact]
    public void Smoke_marked_station_path_clock_order()
    {
        Assert.Equal(
            "Marked here"
            + MonitorHudLine.Separator
            + "Station SM here"
            + MonitorHudLine.Separator
            + "Path OK"
            + MonitorHudLine.Separator
            + "Clock 14:30",
            AlwaysOnExtras.Join("Marked here", "Station SM here", "Path OK", "Clock 14:30"));
    }

    [Fact]
    public void Smoke_look_away_joins_marked_station_path_clock()
    {
        Assert.Equal(
            "Marked NNW 28m"
            + MonitorHudLine.Separator
            + "Station CP NNW 41m"
            + MonitorHudLine.Separator
            + "Path OK"
            + MonitorHudLine.Separator
            + "Clock 00:07",
            AlwaysOnExtras.Join(
                "Marked NNW 28m",
                "Station CP NNW 41m",
                "Path OK",
                "Clock 00:07"));
    }

    [Fact]
    public void Smoke_no_dest_omits_path()
    {
        Assert.Equal(
            "Marked NE 84m" + MonitorHudLine.Separator + "Clock 09:05",
            AlwaysOnExtras.Join("Marked NE 84m", path: null, clock: "Clock 09:05"));
    }
}
