using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Hand-built SW sketch for pin/CLEARED polarity. Live-yard routing goldens
/// are <see cref="HtpSwTurntableLiveDumpTests"/> after the 2026-08-28 dump.
/// </summary>
public class SwTurntableCorridorTests
{
    public const string Origin = "SW-B4L";
    public const string PastSwitch = "#Y-#S969#T";
    public const string Turntable = "#Y-#S1774#T";
    public const string PinJunction = "J-pin";
    public const string FirstFlipDistractor = "J-near";

    /// <summary>
    /// Yard sawtooth: first flip is J-near; pin must stay on the conflict
    /// switch (golden JunctionFirstStop), not the distractor.
    /// </summary>
    public static RouteCorridorSpec SwToTurntable()
    {
        var edges = new[]
        {
            Hop(Origin, PastSwitch, FirstFlipDistractor, 1),
            Hop(PastSwitch, Origin, FirstFlipDistractor, 1),
            Hop(PastSwitch, "#Y-#S970#T", PinJunction, 0),
            Hop("#Y-#S970#T", PastSwitch, PinJunction, 0),
            Hop("#Y-#S970#T", "#Y-#S971#T", PinJunction, 1),
            Hop("#Y-#S971#T", "#Y-#S970#T", PinJunction, 1),
            new PathEdge("#Y-#S971#T", Turntable, cost: 1f, requiresReverse: true),
            new PathEdge(Turntable, "#Y-#S971#T", cost: 1f),
        };
        var selected = new Dictionary<string, int>
        {
            [FirstFlipDistractor] = 0,
            [PinJunction] = 0,
        };
        return new RouteCorridorSpec(
            edges,
            selected,
            Origin,
            Turntable,
            yardId: "SW",
            PathPlanMode.Yard,
            expectedPinJunctionId: PinJunction,
            expectedPastSwitchTrackId: PastSwitch,
            expectedReverseIntoTrackId: Turntable);
    }

    private static PathEdge Hop(string from, string to, string junction, int branch) =>
        new(from, to, junction, branch, 1f);

    [Fact]
    public void Smoke_SW_B4L_to_Turntable_plans_Path_1_switch_and_pins_conflict_not_first_flip()
    {
        var spec = SwToTurntable();
        var plan = RouteCorridorDrive.Plan(in spec);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal(Origin, plan.TrackIds[0]);
        Assert.Equal(Turntable, plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.True(plan.LastHopRequiresReverse);
        Assert.NotNull(plan.JunctionFirstStop);
        Assert.Equal(PinJunction, plan.JunctionFirstStop!.Value.JunctionId);
        Assert.Equal(PastSwitch, plan.JunctionFirstStop.Value.FromTrackId);

        var flips = PathPlan.RequiredFlips(plan);
        Assert.NotEmpty(flips);
        Assert.Equal(FirstFlipDistractor, flips[0].JunctionId);

        var pin = RouteCorridorDrive.PickPin(plan);
        Assert.Equal(PinJunction, pin);
        Assert.NotEqual(FirstFlipDistractor, pin);
        Assert.Equal(spec.ExpectedPinJunctionId, pin);
    }

    [Fact]
    public void Smoke_desk_Past_switch_S969_then_Reverse_into_S1774()
    {
        var spec = SwToTurntable();
        var plan = RouteCorridorDrive.Plan(in spec);
        var steps = RouteCorridorDrive.BindSteps(
            in spec, plan, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.NotNull(steps);
        Assert.Equal(2, steps!.Count);
        Assert.Equal(SwitchListStepKind.Transit, steps[0].Kind);
        Assert.Equal(PastSwitch, steps[0].DestTrackId);
        Assert.Contains("Past switch", steps[0].Label);
        Assert.Contains("until CLEARED", steps[0].Label);
        Assert.Equal(SwitchListStepKind.ReverseInto, steps[1].Kind);
        Assert.Equal(Turntable, steps[1].DestTrackId);
        Assert.Contains("Reverse into", steps[1].Label);
    }

    [Fact]
    public void Smoke_pin_50m_ahead_in_windshield_is_At_switch_not_CLEARED()
    {
        // Cab shot: R selected, 0 km/h, pin on rails ahead, desk At switch.
        // Reverse leading-edge math while pin is in front falsely CLEARS.
        const float length = 38f;
        var pose = new RouteCorridorPose(
            noseX: 0f,
            noseZ: 0f,
            pinX: 50f,
            pinZ: 0f,
            locoForwardX: 1f,
            locoForwardZ: 0f,
            consistLengthM: length);
        Assert.False(pose.PinIsBehind);

        var golden = RouteClearanceTravel.GoldenNosePastM(
            pose.NoseX, pose.NoseZ, pose.PinX, pose.PinZ, pose.LocoForwardX, pose.LocoForwardZ);
        Assert.True(golden < 0f);

        var trap = RouteClearanceTravel.TravelPastJunctionM(golden, length, travelReverse: true);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, trap, length, 12f, 120f)));

        var d = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle, in pose, travelUsesReverse: false);
        Assert.Equal(RouteClearancePhase.AtSwitch, d.Phase);
        Assert.Equal("At switch", d.Caption);
        Assert.False(d.CanThrowAlign);
        Assert.Equal(RouteClearanceGateReason.NeedCleared, RouteClearanceGate.Align(true, d.Phase));
    }

    [Fact]
    public void Smoke_drive_reverse_when_pin_behind_CLEARED_only_after_leading_edge()
    {
        const float length = 38f;
        var pinX = 0f;
        var fwdX = 1f;
        var spec = SwToTurntable();
        var poses = new[]
        {
            // Approaching in reverse: nose east of pin, facing east.
            new RouteCorridorPose(80f, 0f, pinX, 0f, fwdX, 0f, length),
            new RouteCorridorPose(20f, 0f, pinX, 0f, fwdX, 0f, length),
            // Tail still fouling.
            new RouteCorridorPose(-10f, 0f, pinX, 0f, fwdX, 0f, length),
            // Leading edge + length well past frog.
            new RouteCorridorPose(-60f, 0f, pinX, 0f, fwdX, 0f, length),
        };
        Assert.True(poses[0].PinIsBehind);
        Assert.False(poses[3].PinIsBehind);

        var walk = RouteCorridorDrive.Walk(
            in spec, poses, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.Equal(PinJunction, walk.PinJunctionId);
        Assert.Equal(RouteClearancePhase.AtSwitch, walk.Phases[0].Phase);
        Assert.False(walk.Phases[0].CanThrowAlign);
        Assert.Equal(RouteClearancePhase.AtSwitch, walk.Phases[1].Phase);
        Assert.Equal(RouteClearancePhase.AtSwitch, walk.Phases[2].Phase);
        Assert.Equal(RouteClearancePhase.Cleared, walk.Phases[3].Phase);
        Assert.True(walk.Phases[3].CanThrowAlign);
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Align(true, walk.Phases[3].Phase));
    }

    [Fact]
    public void Smoke_TT_back_to_B4L_pin_is_deterministic()
    {
        var outbound = SwToTurntable();
        var inbound = new RouteCorridorSpec(
            outbound.Edges,
            outbound.SelectedBranches,
            originTrackId: Turntable,
            destTrackId: Origin,
            yardId: "SW",
            PathPlanMode.Yard,
            expectedPinJunctionId: PinJunction,
            expectedPastSwitchTrackId: "#Y-#S970#T",
            expectedReverseIntoTrackId: Origin);
        var plan = RouteCorridorDrive.Plan(in inbound);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal(Turntable, plan.TrackIds[0]);
        Assert.Equal(Origin, plan.TrackIds[plan.TrackIds.Count - 1]);
        var pin = RouteCorridorDrive.PickPin(plan);
        Assert.False(string.IsNullOrEmpty(pin));
        Assert.Equal(pin, RouteCorridorDrive.PickPin(plan));
    }
}
