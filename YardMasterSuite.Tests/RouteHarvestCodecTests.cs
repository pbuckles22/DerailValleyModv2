using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteHarvestCodecTests
{
    [Fact]
    public void Roundtrip_SW_Turntable_graph_replays_same_pin()
    {
        var spec = SwTurntableCorridorTests.SwToTurntable();
        var text = RouteHarvestCodec.Format(
            spec.Edges,
            spec.SelectedBranches,
            yardId: spec.YardId,
            originTrackId: spec.OriginTrackId,
            destTrackId: spec.DestTrackId,
            mode: spec.Mode,
            pinJunctionId: spec.ExpectedPinJunctionId);
        Assert.StartsWith(RouteHarvestCodec.Header, text);

        Assert.True(RouteHarvestCodec.TryParse(text, out var snap));
        Assert.Equal(spec.OriginTrackId, snap.OriginTrackId);
        Assert.Equal(spec.DestTrackId, snap.DestTrackId);
        Assert.Equal(spec.ExpectedPinJunctionId, snap.PinJunctionId);
        Assert.Equal(spec.Edges.Count, snap.Edges.Count);

        var replay = new RouteCorridorSpec(
            snap.Edges,
            snap.Selected,
            snap.OriginTrackId!,
            snap.DestTrackId!,
            snap.YardId!,
            snap.Mode,
            snap.PinJunctionId!,
            SwTurntableCorridorTests.PastSwitch,
            spec.ExpectedReverseIntoTrackId);
        var pin = RouteCorridorDrive.PickPin(RouteCorridorDrive.Plan(in replay));
        Assert.Equal(SwTurntableCorridorTests.PinJunction, pin);
        Assert.NotEqual(SwTurntableCorridorTests.FirstFlipDistractor, pin);
    }

    [Fact]
    public void Pose_lines_roundtrip_for_CLEARED_walk()
    {
        var text = RouteHarvestCodec.Format(
            System.Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            pinJunctionId: "J-pin",
            pinX: 10f,
            pinZ: 2f,
            noseX: 80f,
            noseZ: 0f,
            fwdX: 1f,
            fwdZ: 0f,
            consistLengthM: 38f,
            pinIsBehind: true);
        Assert.True(RouteHarvestCodec.TryParse(text, out var snap));
        Assert.Equal(10f, snap.PinX);
        Assert.Equal(80f, snap.NoseX);
        Assert.True(snap.PinIsBehind);
        var pose = new RouteCorridorPose(
            snap.NoseX!.Value,
            snap.NoseZ!.Value,
            snap.PinX!.Value,
            snap.PinZ!.Value,
            snap.FwdX!.Value,
            snap.FwdZ!.Value,
            snap.ConsistLengthM!.Value);
        Assert.True(pose.PinIsBehind);
        var d = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in pose, travelUsesReverse: true);
        Assert.Equal(RouteClearancePhase.AtSwitch, d.Phase);
    }

    [Fact]
    public void Smoke_harvest_length_line_is_trainset_sum_not_loco_only()
    {
        var cars = new[] { 7.49484158f, 12.2f, 18.4f };
        var locoOnly = cars[0];
        var sum = ConsistLengthMeters.Sum(cars);
        Assert.True(sum > locoOnly + 20f);
        var text = RouteHarvestCodec.Format(
            System.Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            consistLengthM: sum);
        Assert.True(RouteHarvestCodec.TryParse(text, out var snap));
        Assert.True(snap.ConsistLengthM.HasValue);
        Assert.Equal(sum, snap.ConsistLengthM.Value, 3);
        Assert.NotEqual(locoOnly, snap.ConsistLengthM.Value);
    }
}
