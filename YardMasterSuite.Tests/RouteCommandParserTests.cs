using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 16.2 ship 1 — <see cref="RouteCommandParser"/> queue the desk shows before any
/// control writes: consecutive drives collapse into legs, sawtooth throws name a
/// junction, and each reverse hop flips travel direction.
/// </summary>
[Collection("StaticSessions")]
public class RouteCommandParserTests
{
    private static PathPlanResult Along(IReadOnlyList<PathEdge> edges, params string[] trackIds) =>
        PathPlan.ReevaluateAlong(trackIds, edges, new Dictionary<string, int>());

    [Fact]
    public void Smoke_16_2_forward_multi_hop_collapses_to_one_drive_leg()
    {
        var edges = HtpSawtoothTddStepsTests.MockSawtoothYardEdges();
        var plan = PathPlan.Find(
            edges,
            HtpSawtoothTddStepsTests.MockSelected(),
            HtpSawtoothTddStepsTests.NodeA,
            HtpSawtoothTddStepsTests.NodeD,
            mode: PathPlanMode.Yard);

        var cmds = RouteCommandParser.Generate(plan, edges);

        var only = Assert.Single(cmds);
        Assert.Equal(LocoCommandAction.Drive, only.Action);
        Assert.Equal(HtpSawtoothTddStepsTests.NodeD, only.TargetId);
        Assert.False(only.TravelReverse);
    }

    [Fact]
    public void Smoke_16_2_sawtooth_throw_resolves_pivot_to_pulled_past_junction()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", "J-in", requiredBranch: 1, cost: 50f, lengthMeters: 50f),
            new PathEdge("B", "C", cost: 50f, requiresReverse: true, lengthMeters: 50f),
        };

        var cmds = RouteCommandParser.Generate(Along(edges, "A", "B", "C"), edges);

        Assert.Equal(5, cmds.Count);
        Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[2].Action);
        Assert.Equal("J-in", cmds[2].TargetId);
        Assert.True(cmds[2].TargetIsJunction);
    }

    [Fact]
    public void Smoke_16_2_reverse_hop_own_junction_wins_over_pulled_past_junction()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", "J-in", requiredBranch: 1, cost: 50f, lengthMeters: 50f),
            new PathEdge("B", "C", "J-rev", requiredBranch: 0, cost: 50f, requiresReverse: true, lengthMeters: 50f),
        };

        var cmds = RouteCommandParser.Generate(Along(edges, "A", "B", "C"), edges);

        Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[2].Action);
        Assert.Equal("J-rev", cmds[2].TargetId);
        Assert.True(cmds[2].TargetIsJunction);
    }

    [Fact]
    public void Smoke_16_2_unresolved_pivot_throw_is_align_at_track_not_a_junction()
    {
        var edges = HtpSawtoothTddStepsTests.MockShortTailYardEdges();
        var plan = PathPlan.Find(
            edges,
            new Dictionary<string, int>(),
            HtpSawtoothTddStepsTests.ShortA,
            HtpSawtoothTddStepsTests.ShortC,
            mode: PathPlanMode.Yard,
            consistLengthMeters: 20f);

        var cmds = RouteCommandParser.Generate(plan, edges);

        Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[2].Action);
        Assert.Equal(HtpSawtoothTddStepsTests.ShortB, cmds[2].TargetId);
        Assert.False(cmds[2].TargetIsJunction);
    }

    [Fact]
    public void Smoke_16_2_second_reverse_flips_travel_back_to_forward()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", "J-1", requiredBranch: 0, cost: 50f, lengthMeters: 50f),
            new PathEdge("B", "C", cost: 50f, requiresReverse: true, lengthMeters: 50f),
            new PathEdge("C", "D", "J-2", requiredBranch: 0, cost: 50f, requiresReverse: true, lengthMeters: 50f),
        };

        var cmds = RouteCommandParser.Generate(Along(edges, "A", "B", "C", "D"), edges);

        Assert.Equal(9, cmds.Count);
        Assert.Equal(LocoCommandAction.ChangeDirection, cmds[3].Action);
        Assert.True(cmds[3].TravelReverse);
        Assert.Equal(LocoCommandAction.Drive, cmds[4].Action);
        Assert.Equal("C", cmds[4].TargetId);
        Assert.True(cmds[4].TravelReverse);

        Assert.Equal(LocoCommandAction.Stop, cmds[5].Action);
        Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[6].Action);
        Assert.Equal("J-2", cmds[6].TargetId);
        Assert.Equal(LocoCommandAction.ChangeDirection, cmds[7].Action);
        Assert.False(cmds[7].TravelReverse);
        Assert.Equal(LocoCommandAction.Drive, cmds[8].Action);
        Assert.Equal("D", cmds[8].TargetId);
        Assert.False(cmds[8].TravelReverse);
    }

    [Fact]
    public void Smoke_16_2_reverse_leg_collapses_hops_after_the_pivot()
    {
        var edges = new[]
        {
            new PathEdge("A", "B", "J-in", requiredBranch: 0, cost: 50f, lengthMeters: 50f),
            new PathEdge("B", "C", cost: 50f, requiresReverse: true, lengthMeters: 50f),
            new PathEdge("C", "D", cost: 50f, lengthMeters: 50f),
            new PathEdge("D", "E", cost: 50f, lengthMeters: 50f),
        };

        var cmds = RouteCommandParser.Generate(Along(edges, "A", "B", "C", "D", "E"), edges);

        Assert.Equal(5, cmds.Count);
        Assert.Equal(LocoCommandAction.Drive, cmds[4].Action);
        Assert.Equal("E", cmds[4].TargetId);
        Assert.True(cmds[4].TravelReverse);
    }

    [Fact]
    public void Smoke_16_2_off_plan_edges_do_not_change_the_queue()
    {
        var edges = new[]
        {
            new PathEdge("X", "Y", "J-far", requiredBranch: 0, cost: 10f, requiresReverse: true, lengthMeters: 10f),
            new PathEdge("A", "B", "J-in", requiredBranch: 0, cost: 50f, lengthMeters: 50f),
            new PathEdge("B", "C", cost: 50f, requiresReverse: true, lengthMeters: 50f),
            new PathEdge("Y", "Z", cost: 10f, lengthMeters: 10f),
        };

        var cmds = RouteCommandParser.Generate(Along(edges, "A", "B", "C"), edges);

        Assert.Equal(
            "Drive>B | Stop | Throw J-in | Rev | Drive<C",
            RouteCommandTelemetry.FormatQueue(cmds));
    }

    [Fact]
    public void Smoke_16_2_route_cmd_log_reads_sawtooth_order_on_set_dest()
    {
        var edges = HtpSawtoothTddStepsTests.MockShortTailYardEdges();
        var plan = PathPlan.Find(
            edges,
            new Dictionary<string, int>(),
            HtpSawtoothTddStepsTests.ShortA,
            HtpSawtoothTddStepsTests.ShortC,
            mode: PathPlanMode.Yard,
            consistLengthMeters: 20f);

        var cmds = RouteCommandParser.Generate(plan, edges);

        Assert.Equal(
            "T2 route-cmd: set-dest n=5 | Drive>Short_B | Stop | Align@Short_B | Rev | Drive<Short_C",
            RouteCommandTelemetry.FormatLog(cmds, "set-dest"));
        Assert.Equal(
            "Cmd: Drive>Short_B | Stop | Align@Short_B | Rev | Drive<Short_C",
            RouteCommandTelemetry.FormatDesk(cmds));
    }

    [Fact]
    public void Smoke_16_2_route_cmd_log_empty_queue_is_explicit()
    {
        var cmds = RouteCommandParser.Generate(null, null);

        Assert.Empty(cmds);
        Assert.Equal("T2 route-cmd: align n=0 | —", RouteCommandTelemetry.FormatLog(cmds, "align"));
        Assert.Null(RouteCommandTelemetry.FormatDesk(cmds));
    }

    [Fact]
    public void Smoke_16_2_branch_to_branch_through_one_junction_is_a_reversal_on_the_stem()
    {
        // Live mapper shape: stem↔branch hops, RequiresReverse never set.
        var edges = new[]
        {
            new PathEdge("Spur", "Stem", "J-ladder", requiredBranch: 0, cost: 50f),
            new PathEdge("Stem", "Spur", "J-ladder", requiredBranch: 0, cost: 50f),
            new PathEdge("Stem", "Target", "J-ladder", requiredBranch: 1, cost: 50f),
            new PathEdge("Target", "Stem", "J-ladder", requiredBranch: 1, cost: 50f),
        };

        var cmds = RouteCommandParser.Generate(Along(edges, "Spur", "Stem", "Target"), edges);

        Assert.Equal(
            "Drive>Stem | Stop | Throw J-ladder | Rev | Drive<Target",
            RouteCommandTelemetry.FormatQueue(cmds));
    }

    [Fact]
    public void Smoke_16_2_start_reverse_leaves_on_reverse_and_the_stem_flips_to_forward()
    {
        var edges = new[]
        {
            new PathEdge("Spur", "Stem", "J-ladder", requiredBranch: 0, cost: 50f),
            new PathEdge("Stem", "Target", "J-ladder", requiredBranch: 1, cost: 50f),
        };

        var cmds = RouteCommandParser.Generate(
            Along(edges, "Spur", "Stem", "Target"),
            edges,
            startReverse: true);

        Assert.Equal(
            "Drive<Stem | Stop | Throw J-ladder | Fwd | Drive>Target",
            RouteCommandTelemetry.FormatQueue(cmds));
    }

    [Theory]
    [InlineData(HtpSetDestAuditTests.Sl55SecondPickup)]
    [InlineData(HtpSetDestAuditTests.Sl55FirstPickup)]
    public void Smoke_16_2_cab_fail_b4l_set_dest_queue_has_the_sawtooth_on_live_graph(string dest)
    {
        // Cab 2.16.32 FAIL: parked on B4L, desk printed only "Cmd: Drive>SW-C4S".
        var snap = HtpFixtures.LoadGraph();
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55ViaSpur,
            dest,
            destYardId: "SW",
            yardFor: PathRouteConstraints.YardIdOf,
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);

        var cmds = RouteCommandParser.Generate(plan, snap.Edges, startReverse: true);
        var queue = RouteCommandTelemetry.FormatQueue(cmds);

        Assert.True(cmds.Count >= 5, queue);
        Assert.Equal(LocoCommandAction.Drive, cmds[0].Action);
        Assert.True(cmds[0].TravelReverse, queue);
        Assert.Equal(LocoCommandAction.Stop, cmds[1].Action);
        Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[2].Action);
        Assert.True(cmds[2].TargetIsJunction, queue);
        Assert.Equal(LocoCommandAction.ChangeDirection, cmds[3].Action);
        Assert.False(cmds[3].TravelReverse, queue);
        var last = cmds[cmds.Count - 1];
        Assert.Equal(LocoCommandAction.Drive, last.Action);
        Assert.Equal(dest, last.TargetId);
        Assert.Equal(plan.JunctionFirstStop?.JunctionId, cmds[2].TargetId);
    }

    [Fact]
    public void Smoke_16_2_switch_list_desk_grows_one_row_for_the_command_line()
    {
        var without = SwitchListStepDisplay.SwitchListDeskHeightPx(13, coach: true, jobDropExtraPx: 0);
        var with = SwitchListStepDisplay.SwitchListDeskHeightPx(13, coach: true, jobDropExtraPx: 0, routeCmd: true);

        Assert.Equal(SwitchListStepDisplay.SwitchListRouteCmdPx, with - without);
    }

    [Fact]
    public void Smoke_16_2_desk_command_line_follows_plan_session()
    {
        RoutePlanSession.Clear();
        try
        {
            var edges = HtpSawtoothTddStepsTests.MockShortTailYardEdges();
            var plan = PathPlan.Find(
                edges,
                new Dictionary<string, int>(),
                HtpSawtoothTddStepsTests.ShortA,
                HtpSawtoothTddStepsTests.ShortC,
                mode: PathPlanMode.Yard,
                consistLengthMeters: 20f);
            var cmds = RouteCommandParser.Generate(plan, edges);

            RoutePlanSession.SetPlan(plan, HtpSawtoothTddStepsTests.ShortA);
            RoutePlanSession.SetCommands(cmds);
            Assert.Equal(5, RoutePlanSession.Commands.Count);
            Assert.StartsWith("Cmd: Drive>Short_B", RoutePlanSession.CommandDeskLine);

            RoutePlanSession.MarkStale("left path");
            Assert.Empty(RoutePlanSession.Commands);
            Assert.Null(RoutePlanSession.CommandDeskLine);

            RoutePlanSession.SetPlan(plan, HtpSawtoothTddStepsTests.ShortA);
            Assert.Empty(RoutePlanSession.Commands);
            Assert.Null(RoutePlanSession.CommandDeskLine);
        }
        finally
        {
            RoutePlanSession.Clear();
        }
    }
}
