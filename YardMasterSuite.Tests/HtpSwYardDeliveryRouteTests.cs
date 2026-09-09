using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Path OK yard delivery: aligned hops still need a dest-side frog pin.
/// Gemini 2026-09-08: <c>ShouldArmPin</c> false must not drop Observe latch.
/// </summary>
[Collection("StaticSessions")]
public sealed class HtpSwYardDeliveryRouteTests : IDisposable
{
    public const string ExitS72 = "#Y-#S72#T";
    public const string B4L = "SW-B4L";
    /// <summary>Cab B4L dest-side approach frog (2.13.2.5.6 relatch).</summary>
    public const string B4LDestSidePin = "1003030";

    public HtpSwYardDeliveryRouteTests() => YmsRouteSessions.ClearAll();

    public void Dispose() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_path_ok_approach_still_latches_dest_side_pin()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { ExitS72, B4L },
            new[] { new PathJunctionEval(B4LDestSidePin, 1, 1) },
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 10f,
            junctionFirstStop: null);

        Assert.Null(SwitchListRouteLeg.PickPinJunctionId(plan));
        Assert.False(SwitchListRouteLeg.ShouldArmPin(plan));
        Assert.Equal(B4LDestSidePin, RouteStepDestPolicy.PickLastJunctionId(plan));

        var past = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            B4L,
            "Past",
            bindNeedsReverse: true);
        SwitchListSession.Bind("SW-SL-55", new[] { past });

        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: true);

        Assert.True(RoutePinLatch.HasLatch);
        Assert.Equal(B4LDestSidePin, RoutePinLatch.Id);
        Assert.True(RoutePinLatch.TravelUsesReverse);
        Assert.True(RoutePinLatch.IsArmedForClearance(plan));
    }

    /// <summary>
    /// Cab 2.13.2.5.7: after B4L CLEARED, C4S Path OK latched first-stop
    /// <c>989976</c> (already behind), rem=0, kept going. Dest-side last
    /// junction is the Past-switch frog — not JunctionFirstStop.
    /// </summary>
    [Fact]
    public void Smoke_c4s_path_ok_must_latch_dest_side_not_behind_first_stop()
    {
        const string behindFirstStop = "989976";
        const string destSide = "1589160";
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "#Y-#S241#T", "SW-C4S" },
            new[]
            {
                new PathJunctionEval(behindFirstStop, 1, 1),
                new PathJunctionEval(destSide, 1, 1),
            },
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 10f,
            junctionFirstStop: new PathJunctionFirstStop(
                behindFirstStop,
                1,
                "#Y-#S241#T",
                "#Y-#S961#T"));

        Assert.Equal(behindFirstStop, SwitchListRouteLeg.PickPinJunctionId(plan));
        Assert.Equal(destSide, RouteStepDestPolicy.PickLastJunctionId(plan));

        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    7,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-C4S",
                    "Past",
                    bindNeedsReverse: false),
            });

        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: true);

        Assert.True(RoutePinLatch.HasLatch);
        Assert.Equal(destSide, RoutePinLatch.Id);
        Assert.NotEqual(behindFirstStop, RoutePinLatch.Id);
    }

    [Fact]
    public void Smoke_harvest_S241_to_C4S_observe_latches_last_junction_not_989976()
    {
        var snap = HtpFixtures.LoadCorridor();
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            "#Y-#S241#T",
            "SW-C4S",
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal("#Y-#S241#T", plan.TrackIds[0]);
        Assert.Equal("SW-C4S", plan.TrackIds[plan.TrackIds.Count - 1]);

        var last = RouteStepDestPolicy.PickLastJunctionId(plan);
        Assert.Equal("1003160", last);
        Assert.NotEqual("989976", last);

        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    7,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-C4S",
                    "Past"),
            });
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: true);
        Assert.Equal(last, RoutePinLatch.Id);
        Assert.NotEqual("989976", RoutePinLatch.Id);
    }

    [Fact]
    public void Smoke_path_ok_prep_must_not_latch_last_junction()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { ExitS72, B4L },
            new[] { new PathJunctionEval(B4LDestSidePin, 1, 1) },
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 10f);

        SwitchListSession.Bind(
            "SW-SL-55",
            new[] { new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep") });

        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: true);
        Assert.False(RoutePinLatch.HasLatch);
    }
}
