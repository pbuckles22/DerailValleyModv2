using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.16.17: a per-meter away penalty rewrote throat→SW-C4S into a
/// 7-switch, 4964s path and the backup missed the spur. Spatial coordinates
/// must not change the time-cost path.
/// </summary>
public class HtpSpatialHaulTests
{
    public const string Origin = "SW-C4S";
    public const string Dest = "SW-B4L";

    [Fact]
    public void Smoke_spatial_graph_does_not_rewrite_c4s_to_b4l_time_path()
    {
        var snap = HtpFixtures.LoadGraph();
        var spatial = SpatialGraph.FromHarvestJunctions(snap.Junctions);
        Assert.True(spatial.HasCoordinates);

        var occupiedKeys = new[] { "SW-1", "SW-3", "SW-4", "SW-C3I", "SW-B1S" };
        var occ = PathRouteConstraints.OccupiedForAlign(
            occupiedKeys,
            snap.Edges,
            Origin,
            Dest,
            destYardOverride: "SW");
        var filtered = PathRouteConstraints.FilterEdges(
            snap.Edges,
            id => PathRouteConstraints.IsAnonymousTrack(id)
                ? PathTrackClass.Unknown
                : PathTrackClass.Through,
            occ,
            Origin,
            Dest,
            PathRouteConstraints.YardIdOf,
            destYardOverride: "SW");

        PathPlanResult Find(SpatialGraph graph) => PathPlan.Find(
            filtered,
            snap.Selected,
            Origin,
            Dest,
            id => PathRouteConstraints.IsAnonymousTrack(id)
                ? PathTrackClass.Unknown
                : PathTrackClass.Through,
            destYardId: "SW",
            yardFor: PathRouteConstraints.YardIdOf,
            mode: PathPlanMode.Yard,
            spatial: graph);

        var timed = Find(default);
        var withXz = Find(spatial);
        Assert.NotEqual(PathCheckStatus.NoPath, timed.Status);
        Assert.Equal(timed.TotalCost, withXz.TotalCost, 1);
        Assert.Equal(timed.TrackIds.Count, withXz.TrackIds.Count);
        Assert.Equal(0f, withXz.SpatialPenaltySeconds);
        Assert.True(withXz.TotalCost < 1000f, "cost=" + withXz.TotalCost.ToString("0"));
    }
}
