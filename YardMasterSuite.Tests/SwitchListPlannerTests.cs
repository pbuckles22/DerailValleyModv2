using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListPlannerTests
{
    private static JobSummary Freight(
        string originTrack,
        string destTrack,
        bool turnAround = false,
        string? turntable = null) =>
        new()
        {
            JobId = "SM-FH-12",
            JobTypeLabel = "FH",
            OriginYardId = "CS",
            DestYardId = "SM",
            OriginTrackId = originTrack,
            DestTrackId = destTrack,
            NeedsTurnAround = turnAround,
            TurntableTrackId = turntable,
        };

    [Fact]
    public void Build_freight_prep_transit_delivery()
    {
        var steps = SwitchListPlanner.Build(Freight("CS-A1L", "SM-C5O"));
        Assert.NotNull(steps);
        Assert.Equal(3, steps!.Count);
        Assert.Equal(SwitchListStepKind.Prep, steps[0].Kind);
        Assert.Equal("CS-A1L", steps[0].DestTrackId);
        Assert.Equal("Prep → CS-A1L", steps[0].Label);
        Assert.Equal(SwitchListStepKind.Transit, steps[1].Kind);
        Assert.Equal("SM-C5O", steps[1].DestTrackId);
        Assert.Equal(SwitchListStepKind.Delivery, steps[2].Kind);
        Assert.Equal("SM-C5O", steps[2].DestTrackId);
        Assert.Equal(1, steps[0].Index);
        Assert.Equal(3, steps[2].Index);
    }

    [Fact]
    public void Build_inserts_turnaround_before_prep_when_flagged()
    {
        var steps = SwitchListPlanner.Build(
            Freight("CS-A1L", "SM-C5O", turnAround: true, turntable: "CS-TT"));
        Assert.NotNull(steps);
        Assert.Equal(4, steps!.Count);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[0].Kind);
        Assert.Equal("CS-TT", steps[0].DestTrackId);
        Assert.Equal("Turn around → CS-TT", steps[0].Label);
        Assert.Equal(SwitchListStepKind.Prep, steps[1].Kind);
        Assert.Equal(SwitchListStepKind.Transit, steps[2].Kind);
        Assert.Equal(SwitchListStepKind.Delivery, steps[3].Kind);
    }

    [Fact]
    public void Build_inserts_reverse_into_before_transit()
    {
        var job = Freight("MF-C3I", "SM-B3I");
        job.NeedsReverseInto = true;
        job.ReverseIntoTrackId = "MF-B4O";
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(4, steps!.Count);
        Assert.Equal(SwitchListStepKind.Prep, steps[0].Kind);
        Assert.Equal(SwitchListStepKind.ReverseInto, steps[1].Kind);
        Assert.Equal("MF-B4O", steps[1].DestTrackId);
        Assert.Equal("Reverse into → MF-B4O", steps[1].Label);
        Assert.Equal(SwitchListStepKind.Transit, steps[2].Kind);
        Assert.Equal(SwitchListStepKind.Delivery, steps[3].Kind);
    }

    [Fact]
    public void Build_turnaround_then_reverse_into_order()
    {
        var job = Freight("MF-C3I", "SM-B3I", turnAround: true, turntable: "MF-TT");
        job.NeedsReverseInto = true;
        job.ReverseIntoTrackId = "MF-B4O";
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(5, steps!.Count);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[0].Kind);
        Assert.Equal(SwitchListStepKind.Prep, steps[1].Kind);
        Assert.Equal(SwitchListStepKind.ReverseInto, steps[2].Kind);
        Assert.Equal(SwitchListStepKind.Transit, steps[3].Kind);
        Assert.Equal(SwitchListStepKind.Delivery, steps[4].Kind);
    }

    [Fact]
    public void Build_fail_closed_without_tracks()
    {
        Assert.Null(SwitchListPlanner.Build(new JobSummary { JobId = "X" }));
        Assert.Null(SwitchListPlanner.Build(Freight("", "SM-C5O")));
        Assert.Null(SwitchListPlanner.Build(Freight("CS-A1L", "  ")));
        Assert.Null(SwitchListPlanner.Build(
            Freight("CS-A1L", "SM-C5O", turnAround: true, turntable: null)));
        var missingRi = Freight("CS-A1L", "SM-C5O");
        missingRi.NeedsReverseInto = true;
        Assert.Null(SwitchListPlanner.Build(missingRi));
    }

    [Fact]
    public void Build_uses_arrival_track_for_transit_when_set()
    {
        var job = Freight("CS-A1L", "SM-C5O");
        job = new JobSummary
        {
            JobId = job.JobId,
            OriginTrackId = job.OriginTrackId,
            DestTrackId = job.DestTrackId,
            DestYardId = "SM",
            DestArrivalTrackId = "SM-I1P",
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal("SM-I1P", steps![1].DestTrackId);
        Assert.Equal("SM-C5O", steps[2].DestTrackId);
    }

    [Fact]
    public void BuildTownTurntable_tt_only()
    {
        var steps = SwitchListPlanner.BuildTownTurntable("SW", "#Y-#S1774#T");
        Assert.NotNull(steps);
        Assert.Single(steps!);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[0].Kind);
        Assert.Equal("#Y-#S1774#T", steps[0].DestTrackId);
        Assert.Equal("SW", steps[0].DestYardId);
    }

    [Fact]
    public void BuildTownTurntable_pivot_then_tt()
    {
        var steps = SwitchListPlanner.BuildTownTurntable("SW", "#Y-#S1774#T", "#Y-#S23#T");
        Assert.NotNull(steps);
        Assert.Equal(2, steps!.Count);
        Assert.Equal(SwitchListStepKind.Pivot, steps[0].Kind);
        Assert.Equal("#Y-#S23#T", steps[0].DestTrackId);
        Assert.Contains("Pivot", steps[0].Label);
        Assert.Contains("until CLEARED", steps[0].Label);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[1].Kind);
        Assert.Equal("#Y-#S1774#T", steps[1].DestTrackId);
        Assert.Equal("Set Forward · Turn around → #Y-#S1774#T", steps[1].Label);
    }

    [Fact]
    public void BuildTownTurntable_fail_closed_without_tt()
    {
        Assert.Null(SwitchListPlanner.BuildTownTurntable("SW", null));
        Assert.Null(SwitchListPlanner.BuildTownTurntable("SW", "  "));
    }
}

public class RouteSwitchListPlannerTests
{
    private static PathPlanResult SawtoothPlan(
        bool aligned,
        bool lastHopReverse,
        string junctionId = "J-dual",
        string approachTo = "C")
    {
        var junctions = aligned
            ? System.Array.Empty<PathJunctionEval>()
            : new[] { new PathJunctionEval(junctionId, 1, 0) };
        return new PathPlanResult(
            aligned ? PathCheckStatus.Aligned : PathCheckStatus.Misaligned,
            new[] { "A", "B", approachTo, "TT" },
            junctions,
            misalignedCount: aligned ? 0 : 1,
            reverseCount: lastHopReverse ? 1 : 0,
            lastHopRequiresReverse: lastHopReverse,
            totalCost: 10f,
            junctionFirstStop: new PathJunctionFirstStop(junctionId, 1, approachTo, "TT"));
    }

    [Fact]
    public void NeedsRouteSwitchList_true_when_flips_and_reverse()
    {
        // Golden 2.8.7.2: pin/list arm on sawtooth JunctionFirstStop, not only
        // while RequiredFlips remain (B3I Path OK still needs the same pin).
        var plan = SawtoothPlan(aligned: false, lastHopReverse: true);
        Assert.True(SwitchListPlanner.NeedsRouteSwitchList(plan, destNeedsReverse: true));
    }

    [Fact]
    public void NeedsRouteSwitchList_true_when_Path_OK_sawtooth_no_flips()
    {
        var plan = SawtoothPlan(aligned: true, lastHopReverse: true);
        Assert.True(SwitchListPlanner.NeedsRouteSwitchList(plan, destNeedsReverse: true));
    }

    [Fact]
    public void NeedsRouteSwitchList_false_when_straight_forward()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            System.Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 1f);
        Assert.False(SwitchListPlanner.NeedsRouteSwitchList(plan, destNeedsReverse: false));
    }

    [Fact]
    public void BuildFromRoute_Path_N_switch_then_reverse_into()
    {
        var plan = SawtoothPlan(aligned: false, lastHopReverse: true);
        var steps = SwitchListPlanner.BuildFromRoute("SW", "TT", plan, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.NotNull(steps);
        Assert.Equal(2, steps!.Count);
        Assert.Equal(SwitchListStepKind.Transit, steps[0].Kind);
        Assert.Contains("Past switch", steps[0].Label);
        Assert.Contains("until CLEARED", steps[0].Label);
        Assert.Equal("C", steps[0].DestTrackId);
        Assert.Equal(SwitchListStepKind.ReverseInto, steps[1].Kind);
        Assert.Equal("TT", steps[1].DestTrackId);
        Assert.Contains("Reverse into", steps[1].Label);
    }
}

public class SwitchListSessionTests
{
    [Fact]
    public void Bind_and_align_target_advances()
    {
        var steps = SwitchListPlanner.Build(new JobSummary
        {
            JobId = "HB-FH-1",
            OriginTrackId = "HB-A1L",
            DestTrackId = "FF-C2O",
        });
        Assert.NotNull(steps);

        SwitchListSession.Clear();
        Assert.False(SwitchListSession.HasActive);

        SwitchListSession.Bind("HB-FH-1", steps!);
        Assert.True(SwitchListSession.HasActive);
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal("HB-A1L", SwitchListSession.CurrentAlignTrackId);

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal("FF-C2O", SwitchListSession.CurrentAlignTrackId);

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(2, SwitchListSession.CurrentIndex);
        Assert.False(SwitchListSession.TryAdvance());
        Assert.True(SwitchListSession.IsComplete);

        SwitchListSession.Clear();
        Assert.False(SwitchListSession.HasActive);
    }
}
