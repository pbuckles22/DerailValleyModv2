using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class SwitchListHudStripTests
{
    public SwitchListHudStripTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_desk_open_hides_strip()
    {
        Assert.False(SwitchListHudStrip.ShouldDraw(deskOpen: true, hasActiveList: true, listComplete: false));
        Assert.True(SwitchListHudStrip.ShouldDraw(deskOpen: false, hasActiveList: true, listComplete: false));
        Assert.False(SwitchListHudStrip.ShouldDraw(deskOpen: false, hasActiveList: true, listComplete: true));
    }

    [Fact]
    public void Smoke_strip_is_current_step_plus_rest_not_fixed_window()
    {
        var steps = Four();
        Assert.Equal("Now", SwitchListHudStrip.NowHeader);
        Assert.Equal("Rest", SwitchListHudStrip.RestHeader);
        Assert.True(SwitchListHudStrip.ShowsRestSection(4));
        Assert.False(SwitchListHudStrip.ShowsRestSection(1));

        var buf = new string[SwitchListHudStrip.Capacity];
        var n = SwitchListHudStrip.FillRemaining(steps, currentIndex: 0, buf);
        Assert.Equal(4, n);
        Assert.StartsWith("▶ 1/4", buf[0]);
        Assert.Contains("2/4", buf[1]);

        n = SwitchListHudStrip.FillRemaining(steps, currentIndex: 2, buf);
        Assert.Equal(2, n);
        Assert.StartsWith("▶ 3/4", buf[0]);
        Assert.Contains("4/4", buf[1]);
        Assert.DoesNotContain("1/4", buf[0]);
    }

    [Fact]
    public void Smoke_strip_sits_under_ticker_not_on_it()
    {
        const float icon = ArMarkerDisplay.IconPixels;
        Assert.Equal(48f, icon);
        Assert.Equal(
            176f,
            SwitchListHudStrip.OverlayTopGuiY(hudStackBottomGuiY: 120f));
        Assert.Equal(
            128f,
            SwitchListHudStrip.OverlayTopGuiY(hudStackBottomGuiY: 0f));
        Assert.Equal(
            128f,
            SwitchListHudStrip.OverlayTopGuiY(hudStackBottomGuiY: 12f));
        Assert.True(
            SwitchListHudStrip.OverlayTopGuiY(120f)
            >= 120f + MonitorHudStackLayout.StickyRowGap + icon);
        Assert.Equal(0.94f, MapsDeskChrome.A);
        Assert.True(MapsDeskChrome.R <= 0.1f);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            SwitchListHudStrip.OverlayTopGuiY(120f);
            SwitchListHudStrip.OverlayTopGuiY(0f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static SwitchListStep[] Four() =>
        new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "A", "Past A"),
            new SwitchListStep(2, SwitchListStepKind.Transit, "SW", "B", "Past B"),
            new SwitchListStep(3, SwitchListStepKind.Prep, "SW", "C", "Prep C"),
            new SwitchListStep(4, SwitchListStepKind.Delivery, "SW", "D", "Delivery D"),
        };
}
