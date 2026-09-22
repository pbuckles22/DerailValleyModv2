using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cost band: a frog about 7 seconds dearer than the cheapest stays eligible
/// when it sits closer to the destination. A 4964-second tour does not, even
/// when that frog is closer still. Hop seconds stay the search cost.
/// </summary>
public class PathPlanCostBandTests
{
    private const string FarFrog = "FAR";
    private const string NearFrog = "NEAR";
    private const string TourFrog = "TOUR";

    [Fact]
    public void Smoke_cost_band_picks_closer_frog_inside_seven_seconds_and_rejects_4964_tour()
    {
        var edges = new[]
        {
            new PathEdge("D", "PAD", "DEST", 0, 1f),
            new PathEdge("O", "FarMid", FarFrog, 0, 1f),
            new PathEdge("FarMid", "D", cost: 1f),
            new PathEdge("O", "NearMid", NearFrog, 0, 8f),
            new PathEdge("NearMid", "D", cost: 1f),
            new PathEdge("O", "TourMid", TourFrog, 0, 4965f),
            new PathEdge("TourMid", "D", cost: 1f),
        };
        var selected = new Dictionary<string, int>();
        var spatial = SpatialGraph.FromHarvestJunctions(new[]
        {
            new RouteHarvestJunction("DEST", 0f, 0f, 0),
            new RouteHarvestJunction(FarFrog, 3f, 0f, 0),
            new RouteHarvestJunction(NearFrog, 1f, 0f, 0),
            new RouteHarvestJunction(TourFrog, 0f, 0f, 0),
        });

        var timed = PathPlan.Find(edges, selected, "O", "D");
        Assert.Equal(FarFrog, FirstFrog(timed));

        var withXz = PathPlan.Find(edges, selected, "O", "D", spatial: spatial);
        Assert.Equal(NearFrog, FirstFrog(withXz));
        Assert.DoesNotContain(withXz.Junctions, j => j.JunctionId == TourFrog);
        Assert.Equal(0f, withXz.SpatialPenaltySeconds);
        Assert.True(withXz.TotalCost < 1000f, "cost=" + withXz.TotalCost.ToString("0"));
    }

    private static string FirstFrog(PathPlanResult plan)
    {
        Assert.NotEmpty(plan.Junctions);
        return plan.Junctions[0].JunctionId;
    }
}
