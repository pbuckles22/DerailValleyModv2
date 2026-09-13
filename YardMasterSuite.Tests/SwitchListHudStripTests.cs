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
    public void Smoke_on_step_4_of_10_remaining_is_4_through_10()
    {
        var steps = SwitchListPlanner.Build(Sl55());
        Assert.NotNull(steps);
        Assert.Equal(10, steps!.Count);
        var buf = new string[SwitchListHudStrip.Capacity];
        var n = SwitchListHudStrip.FillRemaining(steps, currentIndex: 3, buf);
        Assert.Equal(7, n);
        Assert.StartsWith("▶ 4/10", buf[0]);
        Assert.Contains("5/10", buf[1]);
        Assert.Contains("10/10", buf[6]);
        Assert.DoesNotContain("1/10", buf[0]);
        Assert.DoesNotContain("3/10", buf[0]);
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
