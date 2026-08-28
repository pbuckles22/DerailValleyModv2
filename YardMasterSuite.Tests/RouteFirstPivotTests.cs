using System;
using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteFirstPivotTests
{
    [Fact]
    public void Pick_prefers_bridge_over_pull()
    {
        var candidates = new[]
        {
            new RoutePivotCandidate("PULL", canReachFromOrigin: true, canReachFinal: false, 5f, 10f),
            new RoutePivotCandidate("BRIDGE", canReachFromOrigin: true, canReachFinal: true, 20f, 100f),
        };
        Assert.Equal("BRIDGE", RouteFirstPivot.Pick("A", "Z", candidates));
    }

    [Fact]
    public void Pick_pull_when_no_bridge()
    {
        var candidates = new[]
        {
            new RoutePivotCandidate("FAR", canReachFromOrigin: true, canReachFinal: false, 1f, 90f),
            new RoutePivotCandidate("NEAR", canReachFromOrigin: true, canReachFinal: false, 2f, 12f),
        };
        Assert.Equal("NEAR", RouteFirstPivot.Pick("A", "Z", candidates));
    }

    [Fact]
    public void Pick_skips_origin_and_final()
    {
        var candidates = new[]
        {
            new RoutePivotCandidate("A", canReachFromOrigin: true, canReachFinal: true, 0f, 0f),
            new RoutePivotCandidate("Z", canReachFromOrigin: true, canReachFinal: true, 0f, 0f),
            new RoutePivotCandidate("P", canReachFromOrigin: true, canReachFinal: true, 3f, 5f),
        };
        Assert.Equal("P", RouteFirstPivot.Pick("A", "Z", candidates));
    }
}

/// <summary>Golden <c>2.8.7.2</c> pin pick + reverse leading-edge CLEARED math (.13).</summary>
public class SwitchListRouteLegTests
{
    [Fact]
    public void Smoke_SW_Turntable_Path_N_switch_golden_pick_JunctionFirstStop_when_flips()
    {
        var junctions = new[]
        {
            new PathJunctionEval("J-near", requiredBranch: 1, actualBranch: 0),
            new PathJunctionEval("J-TT", requiredBranch: 1, actualBranch: 0),
        };
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "SW-B4L", "B", "#Y-#S113#T", "TT" },
            junctions,
            misalignedCount: 2,
            reverseCount: 1,
            lastHopRequiresReverse: true,
            totalCost: 1f,
            junctionFirstStop: new PathJunctionFirstStop("J-TT", 1, "B", "TT"));

        Assert.True(SwitchListRouteLeg.ShouldArmPin(plan));
        Assert.Equal("J-TT", SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    [Fact]
    public void Smoke_SW_Turntable_Path_OK_still_pins_JunctionFirstStop_sawtooth()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B3I", "B", "#Y-#S113#T", "TT" },
            Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 1f,
            junctionFirstStop: new PathJunctionFirstStop("J-TT", 1, "B", "TT"));
        Assert.True(SwitchListRouteLeg.ShouldArmPin(plan));
        Assert.Equal("J-TT", SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    [Fact]
    public void PickPinJunctionId_falls_back_to_first_RequiredFlip_when_no_JunctionFirstStop()
    {
        var junctions = new[]
        {
            new PathJunctionEval("J1", requiredBranch: 1, actualBranch: 0),
            new PathJunctionEval("J2", requiredBranch: 1, actualBranch: 0),
        };
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "A", "B", "C" },
            junctions,
            misalignedCount: 2,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 1f);
        Assert.Equal("J1", SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    /// <summary>
    /// Reverse travel: leadingPast = -Dot(nose−pin, fwd) + length.
    /// Approaching must not CLEARED; after clear must CLEARED.
    /// </summary>
    [Fact]
    public void Smoke_reverse_leading_edge_CLEARED_not_inverted_vs_golden_forward_dot()
    {
        const float length = 38f;
        // Nose +10 east of pin, fwd east → golden Dot = +10 (false CLEARED on golden).
        const float goldenApproaching = 10f;
        var approachingPast = (-goldenApproaching) + length;
        Assert.Equal(28f, approachingPast);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, approachingPast, length, 12f, 120f)));
        Assert.True(RouteClearanceEval.IsFouling(
            new RouteClearanceSample(true, approachingPast, length, 12f, 120f)));

        // Nose −50 west of pin, fwd east → golden Dot = −50; leadingPast = 50+38 = 88.
        const float goldenPast = -50f;
        var clearedPast = (-goldenPast) + length;
        Assert.Equal(88f, clearedPast);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, clearedPast, length, 12f, 120f)));
    }
}
