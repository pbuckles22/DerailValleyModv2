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

    /// <summary>
    /// Cab 2.13.2.5.9: after B1S couple, Next onto B4L Past-switch relatched
    /// behind frog 1003030 (reverse=1), CLEARED with rem=0, then a 315 m
    /// At switch that never CLEARED. Extra pin is C4S dest-side last.
    /// </summary>
    [Fact]
    public void Smoke_after_B1S_couple_relatch_must_use_C4S_dest_side_not_behind_B4L()
    {
        const string behindFirst = "989976";
        const string b4lApproach = "1003030";
        const string c4sDestSide = "1003160";
        var corridor = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B1S", "SW-B4L", "SW-C4S" },
            new[]
            {
                new PathJunctionEval(behindFirst, 1, 1),
                new PathJunctionEval(b4lApproach, 1, 1),
                new PathJunctionEval(c4sDestSide, 1, 1),
            },
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 593f,
            junctionFirstStop: new PathJunctionFirstStop(
                behindFirst, 1, "SW-B1S", "#Y-#S241#T"));
        var approachB4L = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B1S", "SW-B4L" },
            new[] { new PathJunctionEval(b4lApproach, 1, 1) },
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 10f,
            junctionFirstStop: new PathJunctionFirstStop(
                b4lApproach, 1, "SW-B1S", "SW-B4L"));

        var steps = new[]
        {
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
            new SwitchListStep(
                6,
                SwitchListStepKind.Transit,
                "SW",
                "SW-B4L",
                "Past switch until CLEARED"),
            new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
        };

        Assert.True(RouteStepDestPolicy.PreferCorridorDestSidePin(steps, 1));
        Assert.False(RouteStepDestPolicy.PreferCorridorDestSidePin(steps, 0));
        Assert.Equal(c4sDestSide, RouteStepDestPolicy.PickLastJunctionId(corridor));
        Assert.Equal(
            b4lApproach,
            RouteStepDestPolicy.PickPastSwitchPinJunctionId(approachB4L, corridor));
        Assert.Equal(
            c4sDestSide,
            RouteStepDestPolicy.PickRelatchPastSwitchPin(
                approachB4L, corridor, preferCorridorDestSide: true));
        Assert.NotEqual(
            b4lApproach,
            RouteStepDestPolicy.PickRelatchPastSwitchPin(
                approachB4L, corridor, preferCorridorDestSide: true));
    }

    [Fact]
    public void Smoke_world_skip_spent_frogs_picks_first_uncleared_on_corridor()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B1S", "SW-B4L", "SW-C4S" },
            new[]
            {
                new PathJunctionEval("989976", 1, 1),
                new PathJunctionEval("1003030", 1, 1),
                new PathJunctionEval("1003160", 1, 1),
            },
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 10f,
            junctionFirstStop: new PathJunctionFirstStop("989976", 1, "SW-B1S", "SW-B4L"));

        bool Spent(string id) =>
            id == "989976" || id == "1003030" || id == "989918";

        Assert.Equal(
            "1003160",
            RouteStepDestPolicy.PickFirstUnspentJunctionId(plan, Spent));
        Assert.Equal(
            "1003160",
            RouteStepDestPolicy.PickRelatchPastSwitchPin(
                plan,
                plan,
                preferCorridorDestSide: false,
                Spent));
    }

    [Fact]
    public void Smoke_world_spent_dest_side_last_must_not_win_over_ahead_first_stop()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "#Y-#S1774#T", "#Y-#S1512#T", "SW-B1S" },
            new[]
            {
                new PathJunctionEval("990152", 1, 0),
                new PathJunctionEval("989918", 1, 1),
            },
            misalignedCount: 1,
            reverseCount: 1,
            lastHopRequiresReverse: true,
            totalCost: 315f,
            junctionFirstStop: new PathJunctionFirstStop(
                "990152", 1, "#Y-#S1512#T", "#Y-#S989#T"));

        bool Spent(string id) => id == "989918";

        Assert.Equal(
            "990152",
            RouteStepDestPolicy.PickFirstUnspentJunctionId(plan, Spent));
        Assert.Null(
            RouteStepDestPolicy.PickFirstUnspentJunctionId(
                plan,
                id => id == "990152" || id == "989918"));
    }

    [Fact]
    public void Smoke_leave_TT_relatch_still_prefers_approach_sawtooth()
    {
        var steps = new[]
        {
            new SwitchListStep(
                3,
                SwitchListStepKind.TurnAround,
                "SW",
                "#Y-#S1774#T",
                SwitchListDriveFacing.TurnAroundOnTurntable),
            new SwitchListStep(
                4,
                SwitchListStepKind.Transit,
                "SW",
                "#Y-#S1512#T",
                "Past switch until CLEARED"),
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
        };
        Assert.False(RouteStepDestPolicy.PreferCorridorDestSidePin(steps, 1));
    }

    /// <summary>
    /// Cab 2.13.2.5.8: after TT spin, pin-corridor dest B1S latched dest-side
    /// last <c>989918</c> (already under the loco), CLEARED with no At switch,
    /// GO re-armed Forward 315 m. Leave-TT extra pin is the ahead first-stop.
    /// Dest-side last stays for C4S when that first-stop is behind.
    /// </summary>
    [Fact]
    public void Smoke_leave_TT_past_switch_ahead_first_stop_not_dest_side_last()
    {
        const string leaveFrog = "990152";
        const string destSideLast = "989918";
        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "#Y-#S1774#T", "#Y-#S1512#T", "SW-B1S" },
            new[]
            {
                new PathJunctionEval(leaveFrog, 1, 0),
                new PathJunctionEval(destSideLast, 1, 1),
            },
            misalignedCount: 1,
            reverseCount: 1,
            lastHopRequiresReverse: true,
            totalCost: 315f,
            junctionFirstStop: new PathJunctionFirstStop(
                leaveFrog,
                1,
                "#Y-#S1512#T",
                "#Y-#S989#T"));

        Assert.Equal(leaveFrog, SwitchListRouteLeg.PickPinJunctionId(plan));
        Assert.Equal(destSideLast, RouteStepDestPolicy.PickLastJunctionId(plan));

        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    4,
                    SwitchListStepKind.Transit,
                    "SW",
                    "#Y-#S1512#T",
                    "Past switch until CLEARED"),
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
            });

        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: false);

        Assert.Equal(leaveFrog, RoutePinLatch.Id);
        Assert.NotEqual(destSideLast, RoutePinLatch.Id);
        Assert.False(RoutePinLatch.TravelUsesReverse);
    }
}
