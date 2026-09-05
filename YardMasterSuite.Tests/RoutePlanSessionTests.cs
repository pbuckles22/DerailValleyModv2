using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class RoutePlanSessionTests : IDisposable
{
    public RoutePlanSessionTests()
    {
        RoutePlanSession.Clear();
        RouteMemo.Clear();
    }

    public void Dispose()
    {
        RoutePlanSession.Clear();
        RouteMemo.Clear();
    }

    private static PathPlanResult SamplePlan(float totalCost = 42f) =>
        new(
            PathCheckStatus.Aligned,
            new[] { "SW-B3I", "SW-B4L" },
            Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: totalCost);

    [Fact]
    public void SetPlan_arms_eta_and_trip_zero()
    {
        RoutePlanSession.SetPlan(SamplePlan(30f), "SW-B3I", exitCue: "Exit N", travelEtaSeconds: 25f);
        Assert.True(RoutePlanSession.HasPlan);
        Assert.False(RoutePlanSession.IsStale);
        Assert.Equal("SW-B3I", RoutePlanSession.PlannedOriginTrackId);
        Assert.Equal("Exit N", RoutePlanSession.ExitCue);
        Assert.Equal(25f, RoutePlanSession.PlannedTravelSeconds);
        Assert.Equal(25f, RoutePlanSession.RemainingCostSeconds);
        Assert.Equal(25f, RoutePlanSession.EtaCostSeconds);
        Assert.Equal(0f, RoutePlanSession.TripProgress01);
        Assert.Equal("plan", RoutePlanSession.EtaMode);
        Assert.Null(RoutePlanSession.StatusMessage);
    }

    [Fact]
    public void SetPlan_without_travel_eta_uses_plan_total_cost()
    {
        RoutePlanSession.SetPlan(SamplePlan(99f), "  SW-B3I  ");
        Assert.Equal(99f, RoutePlanSession.PlannedTravelSeconds);
        Assert.Equal(99f, RoutePlanSession.EtaCostSeconds);
        Assert.Equal("SW-B3I", RoutePlanSession.PlannedOriginTrackId);
    }

    [Fact]
    public void MarkStale_clears_live_chips_keeps_status()
    {
        RoutePlanSession.SetPlan(SamplePlan(), "SW-B3I", "Exit E");
        RoutePlanSession.SetJunctionSnapshot(new Dictionary<string, int> { ["990152"] = 1 });
        RoutePlanSession.SetDriveBaseline(10f);
        RoutePlanSession.MarkStale("left path");
        Assert.True(RoutePlanSession.IsStale);
        Assert.False(RoutePlanSession.HasPlan);
        Assert.Null(RoutePlanSession.Plan);
        Assert.Equal("left path", RoutePlanSession.StatusMessage);
        Assert.Null(RoutePlanSession.ExitCue);
        Assert.Null(RoutePlanSession.RemainingCostSeconds);
        Assert.Null(RoutePlanSession.EtaCostSeconds);
        Assert.Null(RoutePlanSession.JunctionSnapshot);
        Assert.Null(RoutePlanSession.DriveMetersAtPlan);
    }

    [Fact]
    public void SetExitCue_and_SetRemainingEta_noop_when_stale()
    {
        RoutePlanSession.SetPlan(SamplePlan(), "SW-B3I", "Exit N");
        RoutePlanSession.MarkStale("stale");
        RoutePlanSession.SetExitCue("Exit S");
        RoutePlanSession.SetRemainingEta(1f, 2f, 3f, 0.5f, 0.25f, "lag");
        Assert.Null(RoutePlanSession.ExitCue);
        Assert.Null(RoutePlanSession.RemainingMeters);
        Assert.Null(RoutePlanSession.EtaMode);
    }

    [Fact]
    public void SetRemainingEta_clamps_progress_and_prefers_remaining_for_eta()
    {
        RoutePlanSession.SetPlan(SamplePlan(50f), "SW-B3I", travelEtaSeconds: 50f);
        RoutePlanSession.SetRemainingEta(-1f, -5f, 100f, 1.5f, -0.2f, "  lag  ");
        Assert.Equal(0f, RoutePlanSession.RemainingCostSeconds);
        Assert.Null(RoutePlanSession.RemainingMeters);
        Assert.Equal(100f, RoutePlanSession.PlannedMeters);
        Assert.Equal(1f, RoutePlanSession.TripProgress01);
        Assert.Equal(0f, RoutePlanSession.HopProgress01);
        Assert.Equal("lag", RoutePlanSession.EtaMode);
        Assert.Equal(0f, RoutePlanSession.EtaCostSeconds);
    }

    [Fact]
    public void SetJunctionSnapshot_and_drive_baseline_roundtrip()
    {
        RoutePlanSession.SetPlan(SamplePlan(), "SW-B3I");
        RoutePlanSession.SetJunctionSnapshot(new Dictionary<string, int> { ["a"] = 0, ["b"] = 1 });
        RoutePlanSession.SetDriveBaseline(-3f);
        Assert.Equal(0, RoutePlanSession.JunctionSnapshot!["a"]);
        Assert.Equal(1, RoutePlanSession.JunctionSnapshot["b"]);
        Assert.Equal(0f, RoutePlanSession.DriveMetersAtPlan);
        RoutePlanSession.SetJunctionSnapshot(null);
        Assert.Empty(RoutePlanSession.JunctionSnapshot!);
        RoutePlanSession.SetExitCue("  Exit NE  ");
        Assert.Equal("Exit NE", RoutePlanSession.ExitCue);
        RoutePlanSession.SetExitCue("   ");
        Assert.Null(RoutePlanSession.ExitCue);
    }

    [Fact]
    public void Clear_wipes_plan()
    {
        RoutePlanSession.SetPlan(SamplePlan(), "SW-B3I");
        RoutePlanSession.Clear();
        Assert.False(RoutePlanSession.HasPlan);
        Assert.False(RoutePlanSession.IsStale);
        Assert.Null(RoutePlanSession.Plan);
    }

    [Fact]
    public void RouteMemo_put_get_trim_and_clear()
    {
        var plan = SamplePlan(12f);
        RouteMemo.Put(" SW-A ", " SW-B ", plan);
        Assert.True(RouteMemo.TryGet("SW-A", "SW-B", out var hit));
        Assert.Same(plan, hit);
        Assert.False(RouteMemo.TryGet(null, "SW-B", out _));
        Assert.False(RouteMemo.TryGet("SW-A", "  ", out _));
        RouteMemo.Put(null, "SW-B", plan);
        Assert.False(RouteMemo.TryGet("x", "y", out _));
        RouteMemo.Clear();
        Assert.False(RouteMemo.TryGet("SW-A", "SW-B", out _));
    }

    [Fact]
    public void Smoke_9_1_world_leave_clears_route_plan_and_memo()
    {
        RoutePlanSession.SetPlan(SamplePlan(), "SW-B3I");
        RouteMemo.Put("SW-B3I", "SW-B4L", SamplePlan());
        YmsRouteSessions.ClearAll();
        Assert.False(RoutePlanSession.HasPlan);
        Assert.False(RouteMemo.TryGet("SW-B3I", "SW-B4L", out _));
    }
}
