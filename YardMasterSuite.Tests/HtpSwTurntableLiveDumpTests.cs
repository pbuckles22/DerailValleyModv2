using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP0 fold: live SW harvest 2026-08-28. Origin SW-B3I, dest town
/// Turntable <c>#Y-#S1774#T</c>. Golden 8.7: trainset ~98 m so the tail
/// must pass the frog — a 7.5 m DE2 bobtail dump is not a valid CP0 fixture.
/// Pin is sawtooth JunctionFirstStop <c>990152</c> even when Path OK
/// (ladder <c>990218</c> already thrown). Sketch polarity stays in
/// <see cref="SwTurntableCorridorTests"/>.
/// </summary>
[Collection("StaticSessions")]
public class HtpSwTurntableLiveDumpTests
{
    public const string SawtoothPin = "990152";
    public const string LadderFlip = "990218";

    /// <summary>Harvested six-car consist. Bobtail ~7.5 m must not replace this.</summary>
    public const float GoldenTrainsetLengthM = 97.72f;

    public const float BobtailHidesBugLengthM = 7.49f;

    private static float RequireGoldenTrainset(in RouteHarvestSnapshot snap)
    {
        Assert.True(snap.ConsistLengthM.HasValue);
        var length = snap.ConsistLengthM!.Value;
        Assert.InRange(length, 90f, 110f);
        Assert.True(length > BobtailHidesBugLengthM * 8f);
        return length;
    }

    [Fact]
    public void Smoke_harvested_graph_and_corridor_parse_same_edge_count()
    {
        var corridor = HtpFixtures.LoadCorridor();
        var graph = HtpFixtures.LoadGraph();
        Assert.Equal("YMS-HARVEST 1", RouteHarvestCodec.Header);
        Assert.Equal("SW", corridor.YardId);
        Assert.Equal("SW-B3I", corridor.OriginTrackId);
        Assert.Equal("#Y-#S1774#T", corridor.DestTrackId);
        Assert.True(corridor.ConsistLengthM.HasValue);
        RequireGoldenTrainset(in corridor);
        Assert.Equal(PathPlanMode.Yard, corridor.Mode);
        Assert.True(corridor.Edges.Count > 1000);
        Assert.Equal(corridor.Edges.Count, graph.Edges.Count);
        Assert.True(HtpFixtures.TryJunctionXz(in corridor, SawtoothPin, out _, out _));
    }

    [Fact]
    public void Smoke_harvested_SW_B3I_to_TT_Path_OK_still_pins_sawtooth_not_ladder()
    {
        var snap = HtpFixtures.LoadCorridor();
        var spec = HtpFixtures.ToSpec(in snap, SawtoothPin);
        var plan = RouteCorridorDrive.Plan(in spec);
        Assert.Equal(PathCheckStatus.Aligned, plan.Status);
        Assert.Equal("SW-B3I", plan.TrackIds[0]);
        Assert.Equal("#Y-#S1774#T", plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.Empty(PathPlan.RequiredFlips(plan));
        Assert.NotNull(plan.JunctionFirstStop);
        Assert.Equal(SawtoothPin, plan.JunctionFirstStop!.Value.JunctionId);
        Assert.Equal(SawtoothPin, RouteCorridorDrive.PickPin(plan));
        Assert.NotEqual(LadderFlip, RouteCorridorDrive.PickPin(plan));
    }

    [Fact]
    public void Smoke_harvested_SW_B4L_to_TT_same_sawtooth_pin_as_B3I()
    {
        var snap = HtpFixtures.LoadCorridor();
        var spec = new RouteCorridorSpec(
            snap.Edges,
            snap.Selected,
            "SW-B4L",
            "#Y-#S1774#T",
            "SW",
            PathPlanMode.Yard,
            expectedPinJunctionId: SawtoothPin,
            expectedPastSwitchTrackId: "SW-B4L",
            expectedReverseIntoTrackId: "#Y-#S1774#T");
        var plan = RouteCorridorDrive.Plan(in spec);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal("SW-B4L", plan.TrackIds[0]);
        Assert.Equal("#Y-#S1774#T", plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.Equal(SawtoothPin, RouteCorridorDrive.PickPin(plan));
        Assert.NotNull(plan.JunctionFirstStop);
        Assert.Equal(SawtoothPin, plan.JunctionFirstStop!.Value.JunctionId);
        var flips = PathPlan.RequiredFlips(plan);
        Assert.NotEmpty(flips);
        Assert.Equal(LadderFlip, flips[0].JunctionId);
        Assert.NotEqual(flips[0].JunctionId, RouteCorridorDrive.PickPin(plan));
    }

    [Fact]
    public void Smoke_harvested_desk_binds_Past_switch_then_dest()
    {
        var snap = HtpFixtures.LoadCorridor();
        var spec = HtpFixtures.ToSpec(in snap, SawtoothPin);
        var plan = RouteCorridorDrive.Plan(in spec);
        var steps = RouteCorridorDrive.BindSteps(
            in spec, plan, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.NotNull(steps);
        Assert.True(steps!.Count >= 2);
        Assert.Equal(SwitchListStepKind.Transit, steps[0].Kind);
        Assert.Contains("until CLEARED", steps[0].Label);
        Assert.Equal(snap.DestTrackId, steps[steps.Count - 1].DestTrackId);
        Assert.Contains("Set Forward", steps[steps.Count - 1].Label);
        Assert.DoesNotContain("Set Reverse", steps[steps.Count - 1].Label);
    }

    [Fact]
    public void Smoke_harvested_pose_at_B3I_sawtooth_is_At_switch_not_CLEARED()
    {
        var snap = HtpFixtures.LoadCorridor();
        var pose = HtpFixtures.DumpedPose(in snap, SawtoothPin);
        var d = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in pose, travelUsesReverse: pose.PinIsBehind);
        Assert.NotEqual(RouteClearancePhase.Cleared, d.Phase);
        Assert.False(d.CanThrowAlign);
        Assert.Equal(RouteClearanceGateReason.NeedCleared, RouteClearanceGate.Align(true, d.Phase));
    }

    [Fact]
    public void Smoke_harvested_pin_50m_ahead_reverse_math_is_the_CLEARED_trap()
    {
        var snap = HtpFixtures.LoadCorridor();
        var pose = HtpFixtures.AlongPinForward(in snap, -50f, SawtoothPin);
        Assert.False(pose.PinIsBehind);

        var golden = RouteClearanceTravel.GoldenNosePastM(
            pose.NoseX, pose.NoseZ, pose.PinX, pose.PinZ, pose.LocoForwardX, pose.LocoForwardZ);
        Assert.True(golden < 0f);

        var trap = RouteClearanceTravel.TravelPastJunctionM(golden, pose.ConsistLengthM, travelReverse: true);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(
                true,
                trap,
                pose.ConsistLengthM,
                RouteClearanceEval.DefaultFrogEnvelopeM,
                RouteClearanceEval.DefaultApproachWindowM)));

        var d = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in pose, travelUsesReverse: false);
        Assert.Equal(RouteClearancePhase.AtSwitch, d.Phase);
        Assert.False(d.CanThrowAlign);
    }

    [Fact]
    public void Smoke_harvested_drive_reverse_CLEARED_only_after_leading_edge()
    {
        var snap = HtpFixtures.LoadCorridor();
        var spec = HtpFixtures.ToSpec(in snap, SawtoothPin);
        var trainset = RequireGoldenTrainset(in snap);
        var poses = new[]
        {
            HtpFixtures.AlongPinForward(in snap, 80f, SawtoothPin),
            HtpFixtures.AlongPinForward(in snap, 20f, SawtoothPin),
            HtpFixtures.AlongPinForward(in snap, -10f, SawtoothPin),
            HtpFixtures.AlongPinForward(in snap, -60f, SawtoothPin),
        };
        Assert.True(poses[0].PinIsBehind);
        Assert.False(poses[3].PinIsBehind);
        foreach (var pose in poses)
        {
            Assert.Equal(trainset, pose.ConsistLengthM, 2);
        }

        var walk = RouteCorridorDrive.Walk(
            in spec, poses, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.Equal(SawtoothPin, walk.PinJunctionId);
        Assert.Equal(RouteClearancePhase.AtSwitch, walk.Phases[0].Phase);
        Assert.False(walk.Phases[0].CanThrowAlign);
        Assert.Equal(RouteClearancePhase.AtSwitch, walk.Phases[1].Phase);
        Assert.Equal(RouteClearancePhase.AtSwitch, walk.Phases[2].Phase);
        Assert.Equal(RouteClearancePhase.Cleared, walk.Phases[3].Phase);
        Assert.True(walk.Phases[3].CanThrowAlign);
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Align(true, walk.Phases[3].Phase));

        RoutePinLatch.Clear();
        try
        {
            RoutePinLatch.Observe("set-dest", walk.Plan, pinIsBehind: poses[0].PinIsBehind);
            var passed = poses[3];
            var latched = RouteCorridorDrive.EvaluatePose(
                RouteClearancePhase.AtSwitch, in passed, RoutePinLatch.TravelUsesReverse);
            Assert.Equal(RouteClearancePhase.Cleared, latched.Phase);
            var liveFlip = RouteCorridorDrive.EvaluatePose(
                RouteClearancePhase.AtSwitch, in passed, travelUsesReverse: passed.PinIsBehind);
            Assert.NotEqual(RouteClearancePhase.Cleared, liveFlip.Phase);
        }
        finally
        {
            RoutePinLatch.Clear();
        }
    }

    [Fact]
    public void Smoke_harvested_B3I_and_B4L_share_S88_then_same_rails_to_TT()
    {
        var snap = HtpFixtures.LoadCorridor();
        var b3 = RouteCorridorDrive.Plan(HtpFixtures.ToSpec(in snap, SawtoothPin));
        var b4 = RouteCorridorDrive.Plan(new RouteCorridorSpec(
            snap.Edges,
            snap.Selected,
            "SW-B4L",
            "#Y-#S1774#T",
            "SW",
            PathPlanMode.Yard,
            SawtoothPin,
            "SW-B4L",
            "#Y-#S1774#T"));
        Assert.Equal("#Y-#S88#T", b3.TrackIds[2]);
        Assert.Equal("#Y-#S88#T", b4.TrackIds[2]);
        Assert.Equal(b3.TrackIds.Count, b4.TrackIds.Count);
        for (var i = 2; i < b3.TrackIds.Count; i++)
        {
            Assert.Equal(b3.TrackIds[i], b4.TrackIds[i]);
        }

        Assert.Equal(SawtoothPin, RouteCorridorDrive.PickPin(b3));
        Assert.Equal(SawtoothPin, RouteCorridorDrive.PickPin(b4));
    }

    [Fact]
    public void Smoke_harvested_SW_B1S_to_TT_pins_same_sawtooth()
    {
        var snap = HtpFixtures.LoadCorridor();
        var spec = new RouteCorridorSpec(
            snap.Edges,
            snap.Selected,
            "SW-B1S",
            "#Y-#S1774#T",
            "SW",
            PathPlanMode.Yard,
            SawtoothPin,
            "SW-B1S",
            "#Y-#S1774#T");
        var plan = RouteCorridorDrive.Plan(in spec);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal("SW-B1S", plan.TrackIds[0]);
        Assert.Equal("#Y-#S1774#T", plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.Equal(SawtoothPin, RouteCorridorDrive.PickPin(plan));
    }

    /// <summary>
    /// 8.7 golden: CLEARED means the tail is past the frog. Nose 50 m past
    /// with a 7.5 m DE2 is already clear; the harvested ~98 m trainset is
    /// still fouling. Replacing this fixture with a bobtail dump fails CI.
    /// </summary>
    [Fact]
    public void Smoke_8_7_trainset_tail_must_clear_frog_bobtail_hides_bug()
    {
        var snap = HtpFixtures.LoadCorridor();
        var trainset = RequireGoldenTrainset(in snap);
        Assert.InRange(trainset, GoldenTrainsetLengthM - 1f, GoldenTrainsetLengthM + 1f);

        const float nosePastFrogM = 50f;
        var frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        Assert.True((nosePastFrogM - BobtailHidesBugLengthM) >= frog);
        Assert.True((nosePastFrogM - trainset) < frog);

        var stillFouling = HtpFixtures.AlongPinForward(in snap, nosePastFrogM, SawtoothPin);
        Assert.Equal(trainset, stillFouling.ConsistLengthM, 2);
        var trainsetFoul = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in stillFouling, travelUsesReverse: false);
        Assert.Equal(RouteClearancePhase.AtSwitch, trainsetFoul.Phase);
        Assert.False(trainsetFoul.CanThrowAlign);
        Assert.Equal(RouteClearanceGateReason.NeedCleared, RouteClearanceGate.Align(true, trainsetFoul.Phase));

        var bobtailPose = new RouteCorridorPose(
            stillFouling.NoseX,
            stillFouling.NoseZ,
            stillFouling.PinX,
            stillFouling.PinZ,
            stillFouling.LocoForwardX,
            stillFouling.LocoForwardZ,
            BobtailHidesBugLengthM);
        var bobtailClear = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in bobtailPose, travelUsesReverse: false);
        Assert.Equal(RouteClearancePhase.Cleared, bobtailClear.Phase);
        Assert.True(bobtailClear.CanThrowAlign);

        var pastTail = HtpFixtures.AlongPinForward(in snap, trainset + frog + 5f, SawtoothPin);
        var cleared = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in pastTail, travelUsesReverse: false);
        Assert.Equal(RouteClearancePhase.Cleared, cleared.Phase);
        Assert.True(cleared.CanThrowAlign);
        Assert.Equal(RouteClearanceGateReason.Ok, RouteClearanceGate.Align(true, cleared.Phase));
    }

    [Fact]
    public void Smoke_8_7_live_B4L_Set_dest_latch_survives_S989_recheck()
    {
        RoutePinLatch.Clear();
        try
        {
            var snap = HtpFixtures.LoadCorridor();
            var tt = RouteCorridorDrive.Plan(new RouteCorridorSpec(
                snap.Edges,
                snap.Selected,
                "SW-B4L",
                "#Y-#S1774#T",
                "SW",
                PathPlanMode.Yard,
                SawtoothPin,
                "SW-B4L",
                "#Y-#S1774#T"));
            Assert.Equal(SawtoothPin, RouteCorridorDrive.PickPin(tt));
            RoutePinLatch.Observe("set-dest", tt);

            var hop = RouteCorridorDrive.Plan(new RouteCorridorSpec(
                snap.Edges,
                snap.Selected,
                "SW-B4L",
                "#Y-#S989#T",
                "SW",
                PathPlanMode.Yard,
                LadderFlip,
                "SW-B4L",
                "#Y-#S989#T"));
            RoutePinLatch.Observe("recheck", hop);
            Assert.Equal(SawtoothPin, RoutePinLatch.EffectivePin(hop));
        }
        finally
        {
            RoutePinLatch.Clear();
        }
    }
}
