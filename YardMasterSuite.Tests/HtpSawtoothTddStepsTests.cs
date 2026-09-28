using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Gemini TDD Mini Win Steps 1–7 as a strict checklist against existing
/// <see cref="PathPlan"/> — not a second toy pathfinder. Steps land one at
/// a time; do not skip ahead.
/// <para>
/// Mock yard = 1-to-1 sawtooth: Start A → Frog B → Frog C → Dest D.
/// Track ids are PathPlan nodes; edges are hops (Gemini Node/Edge).
/// </para>
/// </summary>
public class HtpSawtoothTddStepsTests
{
    // Gemini Step 1 mock: A(start) → B(frog) → C(frog) → D(dest).
    public const string NodeA = "Node_A";
    public const string NodeB = "Node_B";
    public const string NodeC = "Node_C";
    public const string NodeD = "Node_D";
    public const string FrogB = "frog-B";
    public const string FrogC = "frog-C";

    /// <summary>
    /// Step 1 green: static text-yard as PathEdges (CI, no Unity).
    /// Cost = travel weight; LengthMeters = physical m (Gemini mock 500+200+400).
    /// </summary>
    public static IReadOnlyList<PathEdge> MockSawtoothYardEdges() =>
        new[]
        {
            new PathEdge(NodeA, NodeB, junctionId: null, requiredBranch: -1, cost: 500f, lengthMeters: 500f),
            new PathEdge(NodeB, NodeA, junctionId: null, requiredBranch: -1, cost: 500f, lengthMeters: 500f),
            new PathEdge(NodeB, NodeC, FrogB, requiredBranch: 0, cost: 200f, lengthMeters: 200f),
            new PathEdge(NodeC, NodeB, FrogB, requiredBranch: 0, cost: 200f, lengthMeters: 200f),
            new PathEdge(NodeC, NodeD, FrogC, requiredBranch: 0, cost: 400f, lengthMeters: 400f),
            new PathEdge(NodeD, NodeC, FrogC, requiredBranch: 0, cost: 400f, lengthMeters: 400f),
        };

    public static IReadOnlyDictionary<string, int> MockSelected() =>
        new Dictionary<string, int>
        {
            [FrogB] = 0,
            [FrogC] = 0,
        };

    /// <summary>
    /// Gemini Step 1 — The Headless Yard.
    /// Red was: no mock. Green: edges load with A/B/C/D and three hop lengths.
    /// </summary>
    [Fact]
    public void Step1_CanLoadMockSawtoothYard_ReturnsValidGraph()
    {
        var edges = MockSawtoothYardEdges();
        Assert.Equal(6, edges.Count);

        Assert.Contains(
            edges,
            e => e.FromTrackId == NodeA && e.ToTrackId == NodeB && e.Cost == 500f && e.LengthMeters == 500f);
        Assert.Contains(edges, e => e.FromTrackId == NodeB && e.ToTrackId == NodeC && e.JunctionId == FrogB);
        Assert.Contains(
            edges,
            e => e.FromTrackId == NodeC && e.ToTrackId == NodeD && e.Cost == 400f && e.LengthMeters == 400f);

        var nodes = new HashSet<string>();
        for (var i = 0; i < edges.Count; i++)
        {
            nodes.Add(edges[i].FromTrackId);
            nodes.Add(edges[i].ToTrackId);
        }

        Assert.Equal(4, nodes.Count);
        Assert.Contains(NodeA, nodes);
        Assert.Contains(NodeB, nodes);
        Assert.Contains(NodeC, nodes);
        Assert.Contains(NodeD, nodes);
    }

    /// <summary>
    /// Gemini Step 2 — Unrestricted shortest path (distance only, no reverse physics).
    /// Assert PathPlan walks A → B → C → D.
    /// </summary>
    [Fact]
    public void Step2_Pathfinder_FindsShortestPath_NoPhysics()
    {
        var plan = PathPlan.Find(
            MockSawtoothYardEdges(),
            MockSelected(),
            NodeA,
            NodeD,
            mode: PathPlanMode.Yard);

        Assert.True(
            plan.Status == PathCheckStatus.Aligned || plan.Status == PathCheckStatus.Misaligned,
            "status=" + plan.Status);
        Assert.Equal(4, plan.TrackIds.Count);
        Assert.Equal(NodeA, plan.TrackIds[0]);
        Assert.Equal(NodeB, plan.TrackIds[1]);
        Assert.Equal(NodeC, plan.TrackIds[2]);
        Assert.Equal(NodeD, plan.TrackIds[3]);
        Assert.Equal(1100f, plan.TotalCost);
        Assert.Equal(0, plan.ReverseCount);
    }

    // Gemini Step 3: 3-node V — A → pivot B → C; B→C is a reverse hop.
    public const string VNodeA = "V_A";
    public const string VNodeB = "V_B";
    public const string VNodeC = "V_C";

    /// <summary>
    /// Step 3 green: V-shape with harvest-flagged <see cref="PathEdge.RequiresReverse"/>
    /// on the exit leg (Option A — production PathPlan, not a heading-dot A*).
    /// </summary>
    public static IReadOnlyList<PathEdge> MockVShapeReverseEdges() =>
        new[]
        {
            new PathEdge(VNodeA, VNodeB, cost: 100f, lengthMeters: 100f),
            new PathEdge(VNodeB, VNodeA, cost: 100f, lengthMeters: 100f),
            new PathEdge(VNodeB, VNodeC, cost: 100f, requiresReverse: true, lengthMeters: 100f),
            new PathEdge(VNodeC, VNodeB, cost: 100f, requiresReverse: true, lengthMeters: 100f),
        };

    /// <summary>
    /// Gemini Step 3 — Train Orientation (Facing).
    /// A → B → C on a V: the B→C hop must be marked reverse; plan.ReverseCount ≥ 1.
    /// </summary>
    [Fact]
    public void Step3_Pathfinder_DetectsReversal()
    {
        var plan = PathPlan.Find(
            MockVShapeReverseEdges(),
            new Dictionary<string, int>(),
            VNodeA,
            VNodeC,
            mode: PathPlanMode.Yard);

        Assert.True(
            plan.Status == PathCheckStatus.Aligned || plan.Status == PathCheckStatus.Misaligned,
            "status=" + plan.Status);
        Assert.Equal(3, plan.TrackIds.Count);
        Assert.Equal(VNodeA, plan.TrackIds[0]);
        Assert.Equal(VNodeB, plan.TrackIds[1]);
        Assert.Equal(VNodeC, plan.TrackIds[2]);

        // Production lock: PathPlan counts RequiresReverse hops (Gemini "RequiresReversal").
        Assert.Equal(1, plan.ReverseCount);
        Assert.True(plan.LastHopRequiresReverse);
    }

    // Gemini Step 4: two routes Start→Dest — short V-reverse vs longer forward.
    // Costs chosen so short < long < short + PathTrackCosts.ReversePenalty (120):
    // without the penalty Dijkstra would pick short; with it, forward wins.
    public const string PrefS = "Pref_S";
    public const string PrefP = "Pref_P";
    public const string PrefD = "Pref_D";

    /// <summary>
    /// Step 4 green: short reverse into dest (200) vs longer forward (250).
    /// Production <see cref="PathTrackCosts.ReversePenalty"/> is the Gemini "massive" add.
    /// </summary>
    public static IReadOnlyList<PathEdge> MockPreferForwardEdges() =>
        new[]
        {
            // Longer forward: S → D (250)
            new PathEdge(PrefS, PrefD, cost: 250f, lengthMeters: 250f),
            new PathEdge(PrefD, PrefS, cost: 250f, lengthMeters: 250f),
            // Shorter V: S → P → D (100+100), last hop reverse
            new PathEdge(PrefS, PrefP, cost: 100f, lengthMeters: 100f),
            new PathEdge(PrefP, PrefS, cost: 100f, lengthMeters: 100f),
            new PathEdge(PrefP, PrefD, cost: 100f, requiresReverse: true, lengthMeters: 100f),
            new PathEdge(PrefD, PrefP, cost: 100f, requiresReverse: true, lengthMeters: 100f),
        };

    /// <summary>
    /// Gemini Step 4 — Reversal Penalty.
    /// Prefer the longer forward route over a shorter V that requires reverse.
    /// </summary>
    [Fact]
    public void Step4_Pathfinder_PrefersLongerForwardRoute()
    {
        var plan = PathPlan.Find(
            MockPreferForwardEdges(),
            new Dictionary<string, int>(),
            PrefS,
            PrefD,
            mode: PathPlanMode.Yard);

        Assert.True(
            plan.Status == PathCheckStatus.Aligned || plan.Status == PathCheckStatus.Misaligned,
            "status=" + plan.Status);
        Assert.Equal(2, plan.TrackIds.Count);
        Assert.Equal(PrefS, plan.TrackIds[0]);
        Assert.Equal(PrefD, plan.TrackIds[1]);
        Assert.DoesNotContain(PrefP, plan.TrackIds);
        Assert.Equal(0, plan.ReverseCount);
        Assert.False(plan.LastHopRequiresReverse);
        Assert.Equal(250f, plan.TotalCost);
    }

    /// <summary>
    /// Gemini Step 5 — Consist Length Tracking (The Tail).
    /// 100 m train, nose 50 m past Node_B → tail sits 50 m before Node_B on Node_A.
    /// </summary>
    [Fact]
    public void Step5_Consist_CalculatesTailPosition()
    {
        // Corridor A → B → C; lengths match Step 1 hop meters.
        var hopIds = new[] { NodeA, NodeB, NodeC };
        var lengths = new[] { 500f, 200f, 400f };
        const float consistM = 100f;
        const int approachA = 0; // pin = exit of A = Node_B
        const int leadOnB = 1;
        const float noseIntoB = 50f; // 50 m past Node_B

        Assert.True(
            ConsistBounds.TryCompute(
                hopIds,
                lengths,
                leadOnB,
                noseIntoB,
                consistM,
                out var bounds));

        Assert.Equal(NodeA, bounds.TailTrackId);
        Assert.Equal(0, bounds.TailHopIndex);
        Assert.Equal(450f, bounds.TailIntoHopMeters); // 500 − 50 before pin
        Assert.Equal(NodeB, bounds.HeadTrackId);
        Assert.Equal(leadOnB, bounds.HeadHopIndex);
        Assert.Equal(noseIntoB, bounds.HeadIntoHopMeters);
        Assert.Equal(consistM, bounds.ConsistLengthMeters);

        Assert.True(
            RouteTailAlongTrack.TryTailPastPin(
                hopIds,
                lengths,
                leadOnB,
                noseIntoB,
                consistM,
                approachA,
                out var tailPastPin));
        Assert.Equal(-50f, tailPastPin);
    }

    // Gemini Step 6: short reverse run-up (50 m) vs longer forward detour (300 m).
    public const string ShortA = "Short_A";
    public const string ShortB = "Short_B";
    public const string ShortC = "Short_C";
    public const string ShortDetour = "Short_Detour";

    /// <summary>
    /// Step 6 mock: A→B (50 m) → C reverse; A→Detour→C forward (300 m total).
    /// </summary>
    public static IReadOnlyList<PathEdge> MockShortTailYardEdges() =>
        new[]
        {
            new PathEdge(ShortA, ShortB, cost: 50f, lengthMeters: 50f),
            new PathEdge(ShortB, ShortA, cost: 50f, lengthMeters: 50f),
            new PathEdge(ShortB, ShortC, cost: 50f, requiresReverse: true, lengthMeters: 50f),
            new PathEdge(ShortC, ShortB, cost: 50f, requiresReverse: true, lengthMeters: 50f),
            new PathEdge(ShortA, ShortDetour, cost: 150f, lengthMeters: 150f),
            new PathEdge(ShortDetour, ShortA, cost: 150f, lengthMeters: 150f),
            new PathEdge(ShortDetour, ShortC, cost: 150f, lengthMeters: 150f),
            new PathEdge(ShortC, ShortDetour, cost: 150f, lengthMeters: 150f),
        };

    /// <summary>
    /// Gemini Step 6 — Pull-past frog clearance.
    /// 20 m train fits the 50 m run-up (takes short reverse). 100 m does not (detour).
    /// </summary>
    [Fact]
    public void Step6_Pathfinder_RejectsShortTailTrack()
    {
        var edges = MockShortTailYardEdges();
        var selected = new Dictionary<string, int>();

        var shortConsist = PathPlan.Find(
            edges,
            selected,
            ShortA,
            ShortC,
            mode: PathPlanMode.Yard,
            consistLengthMeters: 20f);

        Assert.True(
            shortConsist.Status == PathCheckStatus.Aligned
            || shortConsist.Status == PathCheckStatus.Misaligned,
            "20m status=" + shortConsist.Status);
        Assert.Equal(new[] { ShortA, ShortB, ShortC }, shortConsist.TrackIds);
        Assert.Equal(1, shortConsist.ReverseCount);

        var longConsist = PathPlan.Find(
            edges,
            selected,
            ShortA,
            ShortC,
            mode: PathPlanMode.Yard,
            consistLengthMeters: 100f);

        Assert.True(
            longConsist.Status == PathCheckStatus.Aligned
            || longConsist.Status == PathCheckStatus.Misaligned,
            "100m status=" + longConsist.Status);
        Assert.Equal(new[] { ShortA, ShortDetour, ShortC }, longConsist.TrackIds);
        Assert.Equal(0, longConsist.ReverseCount);
        Assert.DoesNotContain(ShortB, longConsist.TrackIds);
    }

    /// <summary>
    /// Gemini Step 7 — Sawtooth POC command queue.
    /// 20 m consist takes A→B→C reverse; parser emits Drive / Stop / Throw / Reverse / Drive.
    /// </summary>
    [Fact]
    public void Step7_Integration_GeneratesValidSawtoothCommands()
    {
        var edges = MockShortTailYardEdges();
        var plan = PathPlan.Find(
            edges,
            new Dictionary<string, int>(),
            ShortA,
            ShortC,
            mode: PathPlanMode.Yard,
            consistLengthMeters: 20f);

        Assert.Equal(new[] { ShortA, ShortB, ShortC }, plan.TrackIds);

        var cmds = RouteCommandParser.Generate(plan, edges);
        Assert.Equal(5, cmds.Count);

        Assert.Equal(LocoCommandAction.Drive, cmds[0].Action);
        Assert.Equal(ShortB, cmds[0].TargetId);
        Assert.False(cmds[0].TravelReverse);

        Assert.Equal(LocoCommandAction.Stop, cmds[1].Action);

        Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[2].Action);
        Assert.Equal(ShortB, cmds[2].TargetId); // no JunctionId — pivot track
        Assert.False(cmds[2].TargetIsJunction);

        Assert.Equal(LocoCommandAction.ChangeDirection, cmds[3].Action);
        Assert.True(cmds[3].TravelReverse);

        Assert.Equal(LocoCommandAction.Drive, cmds[4].Action);
        Assert.Equal(ShortC, cmds[4].TargetId);
        Assert.True(cmds[4].TravelReverse);
    }
}
