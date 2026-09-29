using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// <b>16.3</b> yard bounding boxes. A walk that has left the origin ladder for a
/// different ladder must not dip back through the origin to save seconds. A walk
/// whose destination is still inside the origin ladder may leave and come back,
/// because that is how a long consist clears its own frog.
/// </summary>
public class PathPlanYardZoneTests
{
    private const string COut = "J-C-OUT";
    private const string BIn = "J-B-IN";
    private const string CBack = "J-C-BACK";
    private const string CToB4 = "J-C-B4";
    private const string B23 = "J-B-23";
    private const string B34 = "J-B-34";
    private const string FrogA = "J-C-FROG-A";

    /// <summary>
    /// Cheapest C→B walk dips back into the C ladder for one hop. It is 29 s
    /// cheaper, so time alone picks it. 16.3 must hard-block that re-entry and
    /// take the all-B corridor instead.
    /// </summary>
    [Fact]
    public void Smoke_16_3_c_to_b_walk_does_not_dip_back_through_the_c_ladder()
    {
        var edges = new[]
        {
            new PathEdge("SW-C1", "SW-MAIN", COut, 0, 1f),
            new PathEdge("SW-MAIN", "SW-B2", BIn, 0, 1f),
            new PathEdge("SW-B2", "SW-C9", CBack, 0, 1f),
            new PathEdge("SW-C9", "SW-B4L", CToB4, 0, 1f),
            new PathEdge("SW-B2", "SW-B3", B23, 0, 30f),
            new PathEdge("SW-B3", "SW-B4L", B34, 0, 1f),
        };

        var plan = PathPlan.Find(
            edges,
            NoneThrown,
            "SW-C1",
            "SW-B4L",
            spatial: LadderXz);

        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.DoesNotContain("SW-C9", plan.TrackIds);
        Assert.Contains("SW-B3", plan.TrackIds);
        Assert.Contains("SW-B4L", plan.TrackIds);
        Assert.Equal(
            PathGraphTelemetry.FormatZoneBlock("J-C-BACK", "J-C-BACK"),
            plan.ZoneBlockLog);
    }

    /// <summary>
    /// Trap case. SW-C1 to SW-C2 is one frog apart, but the consist is longer
    /// than the lead, so the only legal move is to pull right out of the C box
    /// onto the main, stop, and reverse back in through the same frog. A blanket
    /// "never re-enter the origin" rule turns this into NoPath.
    /// </summary>
    [Fact]
    public void Smoke_16_3_long_consist_may_leave_the_c_ladder_to_clear_its_own_frog()
    {
        var edges = new[]
        {
            new PathEdge("SW-C1", "SW-MAIN3", FrogA, 0, 1f, false, 300f),
            new PathEdge("SW-MAIN3", "SW-C2", FrogA, 1, 1f, true, 300f),
        };

        // Yard mode: the sawtooth crosses the same frog twice, which the World
        // profile hard-skips as junction re-use.
        var plan = PathPlan.Find(
            edges,
            NoneThrown,
            "SW-C1",
            "SW-C2",
            mode: PathPlanMode.Yard,
            spatial: LadderXz,
            consistLengthMeters: 200f);

        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Contains("SW-MAIN3", plan.TrackIds);
        Assert.Contains("SW-C2", plan.TrackIds);
    }

    private static readonly Dictionary<string, int> NoneThrown = new();

    /// <summary>
    /// C ladder sits near the origin, B ladder 500 m east. The main runs between
    /// them. Boxes are drawn from these junctions, not from the city prefix:
    /// <c>YardIdOf</c> answers "SW" for every track here.
    /// </summary>
    private static SpatialGraph LadderXz => SpatialGraph.FromHarvestJunctions(new[]
    {
        new RouteHarvestJunction(COut, 90f, 50f, 0),
        new RouteHarvestJunction(FrogA, 95f, 55f, 0),
        new RouteHarvestJunction(CBack, 80f, 60f, 0),
        new RouteHarvestJunction(BIn, 510f, 50f, 0),
        new RouteHarvestJunction(CToB4, 520f, 50f, 0),
        new RouteHarvestJunction(B23, 530f, 50f, 0),
        new RouteHarvestJunction(B34, 540f, 50f, 0),
    });
}
