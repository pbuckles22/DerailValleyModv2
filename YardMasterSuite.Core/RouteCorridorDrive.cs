using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Frozen yard corridor for Tier 1: Dijkstra → pin pick → Switch List → CLEARED walk.
/// No Unity. Graph is a list of <see cref="PathEdge"/> plus live branch selection.
/// </summary>
public readonly struct RouteCorridorSpec
{
    public RouteCorridorSpec(
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> selectedBranches,
        string originTrackId,
        string destTrackId,
        string yardId,
        PathPlanMode mode,
        string expectedPinJunctionId,
        string expectedPastSwitchTrackId,
        string expectedReverseIntoTrackId)
    {
        Edges = edges;
        SelectedBranches = selectedBranches;
        OriginTrackId = originTrackId;
        DestTrackId = destTrackId;
        YardId = yardId;
        Mode = mode;
        ExpectedPinJunctionId = expectedPinJunctionId;
        ExpectedPastSwitchTrackId = expectedPastSwitchTrackId;
        ExpectedReverseIntoTrackId = expectedReverseIntoTrackId;
    }

    public IReadOnlyList<PathEdge> Edges { get; }
    public IReadOnlyDictionary<string, int> SelectedBranches { get; }
    public string OriginTrackId { get; }
    public string DestTrackId { get; }
    public string YardId { get; }
    public PathPlanMode Mode { get; }
    public string ExpectedPinJunctionId { get; }
    public string ExpectedPastSwitchTrackId { get; }
    public string ExpectedReverseIntoTrackId { get; }
}

public readonly struct RouteCorridorPose
{
    public RouteCorridorPose(
        float noseX,
        float noseZ,
        float pinX,
        float pinZ,
        float locoForwardX,
        float locoForwardZ,
        float consistLengthM)
    {
        NoseX = noseX;
        NoseZ = noseZ;
        PinX = pinX;
        PinZ = pinZ;
        LocoForwardX = locoForwardX;
        LocoForwardZ = locoForwardZ;
        ConsistLengthM = consistLengthM;
    }

    public float NoseX { get; }
    public float NoseZ { get; }
    public float PinX { get; }
    public float PinZ { get; }
    public float LocoForwardX { get; }
    public float LocoForwardZ { get; }
    public float ConsistLengthM { get; }

    public bool PinIsBehind =>
        DriveSetFacing.IsTargetBehind(
            LocoForwardX,
            LocoForwardZ,
            PinX - NoseX,
            PinZ - NoseZ);
}

public readonly struct RouteCorridorWalk
{
    public RouteCorridorWalk(
        PathPlanResult plan,
        string? pinJunctionId,
        IReadOnlyList<SwitchListStep>? steps,
        RouteClearanceDecision[] phases)
    {
        Plan = plan;
        PinJunctionId = pinJunctionId;
        Steps = steps;
        Phases = phases;
    }

    public PathPlanResult Plan { get; }
    public string? PinJunctionId { get; }
    public IReadOnlyList<SwitchListStep>? Steps { get; }
    public RouteClearanceDecision[] Phases { get; }
}

public static class RouteCorridorDrive
{
    public static PathPlanResult Plan(in RouteCorridorSpec spec) =>
        PathPlan.Find(
            spec.Edges,
            spec.SelectedBranches,
            spec.OriginTrackId,
            spec.DestTrackId,
            destYardId: spec.YardId,
            mode: spec.Mode);

    public static string? PickPin(PathPlanResult plan) =>
        SwitchListRouteLeg.PickPinJunctionId(plan);

    public static IReadOnlyList<SwitchListStep>? BindSteps(
        in RouteCorridorSpec spec,
        PathPlanResult plan,
        bool pinNeedsReverse,
        bool destNeedsReverse) =>
        SwitchListPlanner.BuildFromRoute(
            spec.YardId,
            spec.DestTrackId,
            plan,
            pinNeedsReverse,
            destNeedsReverse);

    public static RouteClearanceDecision EvaluatePose(
        RouteClearancePhase prior,
        in RouteCorridorPose pose,
        bool travelUsesReverse)
    {
        var travelPast = RouteClearanceTravel.TravelPastJunctionM(
            pose.NoseX,
            pose.NoseZ,
            pose.PinX,
            pose.PinZ,
            pose.LocoForwardX,
            pose.LocoForwardZ,
            pose.ConsistLengthM,
            travelUsesReverse);
        var sample = new RouteClearanceSample(
            hasPin: true,
            nosePastJunctionM: travelPast,
            consistLengthM: pose.ConsistLengthM,
            frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
            approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
        return RouteClearanceEval.Evaluate(prior, in sample);
    }

    public static RouteCorridorWalk Walk(
        in RouteCorridorSpec spec,
        IReadOnlyList<RouteCorridorPose> poses,
        bool pinNeedsReverse,
        bool destNeedsReverse)
    {
        var plan = Plan(in spec);
        var pin = PickPin(plan);
        var steps = BindSteps(in spec, plan, pinNeedsReverse, destNeedsReverse);
        var phases = new RouteClearanceDecision[poses.Count];
        var prior = RouteClearancePhase.Idle;
        var latchedReverse = poses.Count > 0 && poses[0].PinIsBehind;
        for (var i = 0; i < poses.Count; i++)
        {
            phases[i] = EvaluatePose(prior, poses[i], latchedReverse);
            prior = phases[i].Phase;
        }

        return new RouteCorridorWalk(plan, pin, steps, phases);
    }
}
