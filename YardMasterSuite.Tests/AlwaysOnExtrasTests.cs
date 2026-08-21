using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Always-on extras join: Marked then Path then Clock (**6.11**). Station stays **6.12**.
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
    public void Smoke_no_dest_omits_path()
    {
        Assert.Equal(
            "Marked NE 84m" + MonitorHudLine.Separator + "Clock 09:05",
            AlwaysOnExtras.Join("Marked NE 84m", path: null, clock: "Clock 09:05"));
    }
}
