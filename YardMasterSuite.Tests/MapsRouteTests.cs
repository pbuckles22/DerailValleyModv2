using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RoutePlanDisplayTests
{
    [Fact]
    public void FormatPathChip_matches_path_check_display()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "A", "B" },
            Array.Empty<PathJunctionEval>(),
            misalignedCount: 2,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 120f);

        Assert.Equal("Path 2 switch", RoutePlanDisplay.FormatPathChip(plan));
    }

    [Fact]
    public void Smoke_route_prefers_through_lane_over_spur()
    {
        var edges = new[]
        {
            new PathEdge("A", "TH", cost: PathTrackCosts.Through),
            new PathEdge("TH", "A", cost: PathTrackCosts.Through),
            new PathEdge("TH", "B", cost: PathTrackCosts.Through),
            new PathEdge("B", "TH", cost: PathTrackCosts.Through),
            new PathEdge("A", "PK", cost: PathTrackCosts.SpurPocket),
            new PathEdge("PK", "A", cost: PathTrackCosts.SpurPocket),
            new PathEdge("PK", "B", cost: PathTrackCosts.SpurPocket),
            new PathEdge("B", "PK", cost: PathTrackCosts.SpurPocket),
        };

        var plan = PathPlan.Find(edges, new Dictionary<string, int>(), "A", "B");
        Assert.Equal(new[] { "A", "TH", "B" }, plan.TrackIds);
        Assert.Equal(
            "Path OK | ETA 20m34s",
            RouteEtaDisplay.HudPathChip(RoutePlanDisplay.FormatPathChip(plan)!, 20f * 60f + 34f));
    }
}

public class RouteTelemetryTests
{
    [Fact]
    public void Observe_emits_on_plan_change_only()
    {
        var cache = default(RouteTelemetryCache);
        Assert.True(RouteTelemetry.Observe(true, PathCheckStatus.Aligned, 0, 100f, ref cache));
        Assert.False(RouteTelemetry.Observe(true, PathCheckStatus.Aligned, 0, 100f, ref cache));
        Assert.True(RouteTelemetry.Observe(true, PathCheckStatus.Misaligned, 1, 100f, ref cache));
        Assert.True(RouteTelemetry.Observe(false, PathCheckStatus.NoDestination, 0, 0f, ref cache));
    }

    [Fact]
    public void NextLog_init_change_cleared()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            60f);
        Assert.Equal("T2 route cleared", RouteTelemetry.NextLog(RouteTelemetryLogKind.Cleared, plan, 0f));
        Assert.Contains("T2 route init:", RouteTelemetry.NextLog(RouteTelemetryLogKind.Init, plan, 60f, "Set Forward"));
        Assert.Contains("Path OK", RouteTelemetry.NextLog(RouteTelemetryLogKind.Change, plan, 60f));
    }
}
