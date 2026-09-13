using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Job-ticket hops + harvest frogs → Past-switch. Not a hardcoded three-hop
/// list. Bald Transit to dest must fail without cab.
/// </summary>
[Collection("StaticSessions")]
public class SwitchListHopCoverageTests
{
    public SwitchListHopCoverageTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_SL55_job_hops_include_C4S_to_C1O()
    {
        var hops = SwitchListHopCoverage.JobFrogHops(Sl55LiveMultiPickupJob());
        Assert.Contains(hops, h => h.Item1 == "SW-C4S" && h.Item2 == "SW-C1O");
        Assert.Contains(hops, h => h.Item1 == "SW-B1S" && h.Item2 == "SW-C4S");
        Assert.Contains(hops, h => h.Item1 == "SW-B4L" && h.Item2 == "#Y-#S1774#T");
    }

    [Fact]
    public void Smoke_SL55_bald_Transit_C1O_fails_job_coverage()
    {
        var snap = HtpFixtures.LoadCorridor();
        var bald = new[]
        {
            new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
            new SwitchListStep(8, SwitchListStepKind.Transit, "SW", "SW-C1O", "Transit → SW-C1O"),
            new SwitchListStep(9, SwitchListStepKind.Delivery, "SW", "SW-C1O", "Delivery → SW-C1O"),
        };

        Assert.False(SwitchListHopCoverage.StepIsPastSwitchClearance(bald[1]));
        var missing = SwitchListHopCoverage.FirstUncoveredFrogHop(
            Sl55LiveMultiPickupJob(),
            bald,
            snap.Edges,
            snap.Selected);
        Assert.False(string.IsNullOrEmpty(missing), "bald Transit to dest must fail hop coverage");
        Assert.Equal(
            "SW-C4S → SW-C1O",
            SwitchListHopCoverage.FirstUncoveredFrogHop(
                bald,
                snap.Edges,
                snap.Selected,
                "SW",
                new[] { ("SW-C4S", "SW-C1O") }));
    }

    [Fact]
    public void Smoke_FH82_job_hops_include_inter_yard_haul()
    {
        var hops = SwitchListHopCoverage.JobFrogHops(Fh82LiveJob());
        Assert.Contains(hops, h => h.Item1 == "SW-C1O" && h.Item2 == "GF-D5I");
        Assert.Contains(hops, h => h.Item1 == "SW-B4L" && h.Item2 == "#Y-#S1774#T");
        var steps = SwitchListPlanner.Build(Fh82LiveJob());
        Assert.NotNull(steps);
        Assert.Contains(
            steps,
            s => s.DestTrackId == "GF-D5I"
                && s.Kind == SwitchListStepKind.Transit
                && SwitchListHopCoverage.StepIsPastSwitchClearance(s));
    }

    [Fact]
    public void Smoke_inter_yard_dest_without_Past_switch_fails_coverage()
    {
        var edges = new[]
        {
            new PathEdge("SW-C4S", "SW-X", "J-THROAT", 0, 5f),
            new PathEdge("SW-X", "SW-C4S", "J-THROAT", 0, 5f),
            new PathEdge("SW-X", "GF-D5I", cost: 20f),
            new PathEdge("GF-D5I", "SW-X", cost: 20f),
        };
        var selected = new System.Collections.Generic.Dictionary<string, int>
        {
            ["J-THROAT"] = 0,
        };
        var job = new JobSummary
        {
            JobId = "SW-FH-bald",
            JobTypeLabel = "FH",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C4S",
            DestTrackId = "GF-D5I",
        };
        var bald = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
            new SwitchListStep(2, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
            new SwitchListStep(3, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery → GF-D5I"),
        };

        Assert.False(SwitchListHopCoverage.StepIsPastSwitchClearance(bald[1]));
        Assert.Equal(
            "SW-C4S → GF-D5I",
            SwitchListHopCoverage.FirstUncoveredFrogHop(
                job,
                bald,
                edges,
                selected));
    }

    [Fact]
    public void Smoke_same_yard_dest_Transit_is_Past_switch_even_without_reverseInto()
    {
        var snap = HtpFixtures.LoadCorridor();
        var job = new JobSummary
        {
            JobId = "SW-SL-bald",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-C4S",
            DestTrackId = "SW-C1O",
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Contains(
            steps,
            s => s.DestTrackId == "SW-C1O"
                && s.Kind == SwitchListStepKind.Transit
                && SwitchListHopCoverage.StepIsPastSwitchClearance(s));
        Assert.True(
            string.IsNullOrEmpty(
                SwitchListHopCoverage.FirstUncoveredFrogHop(
                    job,
                    steps,
                    snap.Edges,
                    snap.Selected)));
    }

    [Fact]
    public void Smoke_job_tickets_on_SW_harvest_cover_frog_hops()
    {
        var snap = HtpFixtures.LoadCorridor();
        var jobs = new[] { Sl55LiveMultiPickupJob(), Fh82LiveJob() };
        for (var i = 0; i < jobs.Length; i++)
        {
            var job = jobs[i];
            var steps = SwitchListPlanner.Build(job);
            Assert.NotNull(steps);
            var missing = SwitchListHopCoverage.FirstUncoveredFrogHop(
                job,
                steps,
                snap.Edges,
                snap.Selected);
            Assert.True(
                string.IsNullOrEmpty(missing),
                job.JobId + " omitted Past-switch for harvest frog hop " + missing);
        }
    }

    private static JobSummary Sl55LiveMultiPickupJob() =>
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

    private static JobSummary Fh82LiveJob() =>
        new()
        {
            JobId = "SW-FH-82",
            JobTypeLabel = "FH",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C1O",
            DestTrackId = "GF-D5I",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
        };
}
