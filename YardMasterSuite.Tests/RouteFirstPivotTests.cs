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

public class SwitchListRouteLegTests
{
    [Fact]
    public void PickPinJunctionId_prefers_JunctionFirstStop()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            System.Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 1f,
            junctionFirstStop: new PathJunctionFirstStop("J-first", 1, "A", "B"));
        Assert.Equal("J-first", SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    [Fact]
    public void PickPinJunctionId_falls_back_to_first_RequiredFlip()
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
}
