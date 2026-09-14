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
    public void Smoke_strip_is_current_step_plus_rest_not_fixed_4_10()
    {
        var steps = SwitchListPlanner.Build(Sl55());
        Assert.NotNull(steps);
        Assert.Equal(10, steps!.Count);
        Assert.Equal("Now", SwitchListHudStrip.NowHeader);
        Assert.Equal("Rest", SwitchListHudStrip.RestHeader);
        Assert.True(SwitchListHudStrip.ShowsRestSection(7));
        Assert.False(SwitchListHudStrip.ShowsRestSection(1));

        var buf = new string[SwitchListHudStrip.Capacity];
        var n = SwitchListHudStrip.FillRemaining(steps, currentIndex: 0, buf);
        Assert.Equal(10, n);
        Assert.StartsWith("▶ 1/10", buf[0]);
        Assert.Contains("2/10", buf[1]);

        n = SwitchListHudStrip.FillRemaining(steps, currentIndex: 3, buf);
        Assert.Equal(7, n);
        Assert.StartsWith("▶ 4/10", buf[0]);
        Assert.Contains("5/10", buf[1]);
        Assert.Contains("10/10", buf[6]);
        Assert.DoesNotContain("1/10", buf[0]);
        Assert.DoesNotContain("3/10", buf[0]);
    }

    [Fact]
    public void Smoke_strip_sits_under_ticker_not_on_it()
    {
        Assert.Equal(
            128f,
            SwitchListHudStrip.OverlayTopGuiY(hudStackBottomGuiY: 120f));
        Assert.Equal(
            80f,
            SwitchListHudStrip.OverlayTopGuiY(hudStackBottomGuiY: 0f));
        Assert.Equal(
            80f,
            SwitchListHudStrip.OverlayTopGuiY(hudStackBottomGuiY: 12f));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            SwitchListHudStrip.OverlayTopGuiY(120f);
            SwitchListHudStrip.OverlayTopGuiY(0f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static JobSummary Sl55() =>
        new()
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
        };
}
