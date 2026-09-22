using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.16.15 step 7: Prep SW-C4S matched the Transit dest, so the live
/// throat <c>#Y-#S113#T</c> was replaced with SW-C4S and PathPlan returned
/// cost 0. Pin 1002848 was still ahead.
/// </summary>
public class HtpPrepSameDestOriginTests
{
    public const string Throat = "#Y-#S113#T";
    public const string Departed = "SW-B1S";
    public const string PrepDest = "SW-C4S";
    public const string Pin8 = "1002848";

    private static SwitchListStep[] Step7() =>
        new[]
        {
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", Departed, "Prep → SW-B1S"),
            new SwitchListStep(6, SwitchListStepKind.Transit, "SW", PrepDest, "Past switch → SW-C4S until CLEARED"),
            new SwitchListStep(7, SwitchListStepKind.Prep, "SW", PrepDest, "Prep → SW-C4S"),
        };

    [Fact]
    public void Smoke_step7_throat_S113_to_C4S_is_nonzero_through_pin_1002848()
    {
        var choice = PrepSameDestOrigin.Resolve(Throat, PrepDest, Step7(), 2);
        Assert.Equal(PrepSameDestOrigin.Kind.UseProbe, choice.Kind);
        Assert.True(choice.SameDestPrep);

        var plan = Plan(Throat, PrepDest);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.NotEqual(PathCheckStatus.NoOrigin, plan.Status);
        Assert.True(plan.TotalCost > 0f, "cost=" + plan.TotalCost.ToString("0"));
        Assert.Equal(Throat, plan.TrackIds[0]);
        Assert.Equal(PrepDest, plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.True(plan.TrackIds.Count > 1);
        Assert.True(
            HasJunction(plan, Pin8),
            "cost=" + plan.TotalCost.ToString("0")
            + " tracks=" + string.Join(">", plan.TrackIds)
            + " junc=" + string.Join(",", JunctionIds(plan)));
    }

    [Fact]
    public void Smoke_step7_stale_B1S_probe_does_not_route_back_to_B1S()
    {
        var choice = PrepSameDestOrigin.Resolve(Departed, PrepDest, Step7(), 2);
        Assert.True(choice.SameDestPrep);
        Assert.Equal(PrepSameDestOrigin.Kind.HoldDeparted, choice.Kind);

        var fakeArrival = Plan(PrepDest, PrepDest);
        Assert.Equal(0f, fakeArrival.TotalCost);
        Assert.NotEqual(PrepSameDestOrigin.Kind.AlreadyAtDest, choice.Kind);
    }

    [Fact]
    public void Smoke_step5_prep_dest_differs_from_prior_transit_keeps_probe()
    {
        var steps = new[]
        {
            new SwitchListStep(4, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Past switch"),
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", Departed, "Prep → SW-B1S"),
        };
        var choice = PrepSameDestOrigin.Resolve(Throat, Departed, steps, 1);
        Assert.False(choice.SameDestPrep);
        Assert.Equal(PrepSameDestOrigin.Kind.UseProbe, choice.Kind);
    }

    [Fact]
    public void Smoke_step7_probe_already_on_dest_allows_zero_path()
    {
        var choice = PrepSameDestOrigin.Resolve(PrepDest, PrepDest, Step7(), 2);
        Assert.True(choice.SameDestPrep);
        Assert.Equal(PrepSameDestOrigin.Kind.AlreadyAtDest, choice.Kind);
    }

    private static bool HasJunction(PathPlanResult plan, string id)
    {
        for (var i = 0; i < plan.Junctions.Count; i++)
        {
            if (plan.Junctions[i].JunctionId == id)
            {
                return true;
            }
        }

        return false;
    }

    private static string JunctionIds(PathPlanResult plan)
    {
        var ids = new string[plan.Junctions.Count];
        for (var i = 0; i < plan.Junctions.Count; i++)
        {
            ids[i] = plan.Junctions[i].JunctionId;
        }

        return string.Join(",", ids);
    }

    private static PathPlanResult Plan(string origin, string dest)
    {
        var snap = HtpFixtures.LoadGraph();
        var spatial = SpatialGraph.FromHarvestJunctions(snap.Junctions);
        var mode = PathPlanModeSelect.ForTrip(origin, dest, "SW");
        return PathPlan.Find(
            snap.Edges,
            snap.Selected,
            origin,
            dest,
            destYardId: "SW",
            mode: mode,
            spatial: spatial);
    }
}
