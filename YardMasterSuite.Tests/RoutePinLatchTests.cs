using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke 2026-08-28 B4L → Turntable: Set dest pin 990152, then Switch List
/// Recheck to S989 stole 990218, then Path OK pin=none idled CLEARED.
/// </summary>
[Collection("StaticSessions")]
public class RoutePinLatchTests : IDisposable
{
    public RoutePinLatchTests() => RoutePinLatch.Clear();

    public void Dispose() => RoutePinLatch.Clear();

    private static PathPlanResult SawtoothSetDest() =>
        new(
            PathCheckStatus.Misaligned,
            new[] { "SW-B4L", "#Y-#S989#T", "#Y-#S1774#T" },
            new[]
            {
                new PathJunctionEval("990218", requiredBranch: 1, actualBranch: 0),
                new PathJunctionEval("990152", requiredBranch: 1, actualBranch: 0),
            },
            misalignedCount: 2,
            reverseCount: 1,
            lastHopRequiresReverse: true,
            totalCost: 639f,
            junctionFirstStop: new PathJunctionFirstStop("990152", 1, "SW-B4L", "#Y-#S989#T"));

    private static PathPlanResult CheaperS989Hop() =>
        new(
            PathCheckStatus.Misaligned,
            new[] { "SW-B4L", "#Y-#S989#T" },
            new[] { new PathJunctionEval("990218", requiredBranch: 1, actualBranch: 0) },
            misalignedCount: 1,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 443f);

    private static PathPlanResult PathOkNoPin() =>
        new(
            PathCheckStatus.Aligned,
            new[] { "#Y-#S1320#T", "#Y-#S989#T" },
            Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 298f);

    [Fact]
    public void Smoke_8_7_switch_list_bind_S989_must_not_steal_Set_dest_sawtooth_pin()
    {
        var setDest = SawtoothSetDest();
        Assert.Equal("990152", SwitchListRouteLeg.PickPinJunctionId(setDest));
        RoutePinLatch.Observe("set-dest", setDest);
        Assert.Equal("990152", RoutePinLatch.EffectivePin(setDest));

        var stolen = CheaperS989Hop();
        Assert.Equal("990218", SwitchListRouteLeg.PickPinJunctionId(stolen));
        RoutePinLatch.Observe("recheck", stolen);
        Assert.Equal("990152", RoutePinLatch.EffectivePin(stolen));
        Assert.NotEqual(SwitchListRouteLeg.PickPinJunctionId(stolen), RoutePinLatch.EffectivePin(stolen));
    }

    [Fact]
    public void Smoke_8_7_Path_OK_pin_none_must_not_idle_latched_sawtooth()
    {
        RoutePinLatch.Observe("set-dest", SawtoothSetDest());
        var live = PathOkNoPin();
        Assert.Null(SwitchListRouteLeg.PickPinJunctionId(live));
        Assert.Equal("990152", RoutePinLatch.EffectivePin(live));
        Assert.True(RoutePinLatch.HasLatch);
        Assert.Equal(
            RouteClearanceGateReason.NeedCleared,
            RouteClearanceGate.Align(hasPin: true, RouteClearancePhase.AtSwitch));
    }

    [Fact]
    public void Recheck_without_Set_dest_still_uses_live_PickPin()
    {
        var stolen = CheaperS989Hop();
        RoutePinLatch.Observe("recheck", stolen);
        Assert.False(RoutePinLatch.HasLatch);
        Assert.Equal("990218", RoutePinLatch.EffectivePin(stolen));
    }

    [Fact]
    public void Harvest_corridor_writes_on_Set_dest_only()
    {
        Assert.True(RouteHarvestPolicy.ShouldWriteCorridor("set-dest"));
        Assert.False(RouteHarvestPolicy.ShouldWriteCorridor("recheck"));
        Assert.False(RouteHarvestPolicy.ShouldWriteCorridor("align"));
        Assert.False(RouteHarvestPolicy.ShouldWriteCorridor(null));
    }

    [Fact]
    public void Smoke_8_7_route_bind_and_align_must_not_Retarget_dest_before_CLEARED()
    {
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest("route-bind", RouteClearancePhase.AtSwitch));
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest("list-align", RouteClearancePhase.AtSwitch));
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest("list-next", RouteClearancePhase.AtSwitch));
        Assert.True(RouteStepDestPolicy.ShouldRetargetMapsDest("list-next", RouteClearancePhase.Cleared));
        Assert.True(RouteStepDestPolicy.ShouldRetargetMapsDest("list-load", RouteClearancePhase.Idle));
        Assert.True(RouteStepDestPolicy.ShouldRetargetMapsDest(
            "list-load",
            RouteClearancePhase.Idle,
            SwitchListStepKind.Prep));
    }

    [Fact]
    public void Smoke_SW_FH_82_list_load_past_switch_must_not_Recheck_Maps_to_B4L()
    {
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest(
            "list-load",
            RouteClearancePhase.Idle,
            SwitchListStepKind.Transit));
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest(
            "list-load",
            RouteClearancePhase.Idle,
            SwitchListStepKind.Pivot));
    }

    [Fact]
    public void Smoke_SW_FH_82_pin_corridor_dest_is_TT_not_approach()
    {
        var steps = new[]
        {
            new SwitchListStep(
                1,
                SwitchListStepKind.Transit,
                "SW",
                "SW-B4L",
                "Set Reverse · Past switch → SW-B4L until CLEARED",
                bindNeedsReverse: true),
            new SwitchListStep(
                2,
                SwitchListStepKind.TurnAround,
                "SW",
                "#Y-#S1774#T",
                SwitchListDriveFacing.TurnAroundOnTurntable),
            new SwitchListStep(3, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
        };
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, 0, out var yard, out var track));
        Assert.Equal("SW", yard);
        Assert.Equal("#Y-#S1774#T", track);
        Assert.False(RouteStepDestPolicy.TryPinCorridorDest(steps, 2, out _, out _));

        var route = SwitchListPlanner.BuildFromRoute(
            "SW",
            "#Y-#S1774#T",
            new PathPlanResult(
                PathCheckStatus.Misaligned,
                new[] { "SW-B4L", "#Y-#S989#T", "#Y-#S1774#T" },
                new[] { new PathJunctionEval("990152", 1, 0) },
                misalignedCount: 1,
                reverseCount: 1,
                lastHopRequiresReverse: true,
                totalCost: 10f,
                junctionFirstStop: new PathJunctionFirstStop("990152", 1, "SW-B4L", "#Y-#S989#T")),
            pinNeedsReverse: true,
            destNeedsReverse: true);
        Assert.NotNull(route);
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(route, 0, out _, out var into));
        Assert.Equal("#Y-#S1774#T", into);
    }

    [Fact]
    public void Smoke_SW_FH_82_drive_to_tt_is_not_pin_corridor()
    {
        var steps = new[]
        {
            new SwitchListStep(
                1,
                SwitchListStepKind.Transit,
                "SW",
                "SW-B4L",
                "Set Reverse · Past switch → SW-B4L until CLEARED",
                bindNeedsReverse: true),
            new SwitchListStep(
                2,
                SwitchListStepKind.TurnAround,
                "SW",
                "#Y-#S1774#T",
                SwitchListDriveFacing.FormatDriveLabel(
                    true,
                    SwitchListDriveFacing.ToTurntableAction,
                    "#Y-#S1774#T"),
                bindNeedsReverse: true),
            new SwitchListStep(
                3,
                SwitchListStepKind.TurnAround,
                "SW",
                "#Y-#S1774#T",
                SwitchListDriveFacing.TurnAroundOnTurntable),
            new SwitchListStep(4, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
            new SwitchListStep(5, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
            new SwitchListStep(6, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery → GF-D5I"),
        };
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, 0, out _, out var inbound));
        Assert.Equal("#Y-#S1774#T", inbound);
        Assert.False(RouteStepDestPolicy.TryPinCorridorDest(steps, 1, out _, out _));
        Assert.False(RouteStepDestPolicy.TryPinCorridorDest(steps, 2, out _, out _));
        Assert.False(RouteStepDestPolicy.TryPinCorridorDest(steps, 3, out _, out _));
        Assert.True(RouteStepDestPolicy.ShouldSetPinCorridorDest("list-next"));
        Assert.True(RouteStepDestPolicy.ShouldSetPinCorridorDest("list-load"));
        Assert.False(RouteStepDestPolicy.ShouldSetPinCorridorDest("list-align"));
    }

    [Fact]
    public void Smoke_8_7_Next_after_pin_hide_still_retargets_dest()
    {
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest("list-next", RouteClearancePhase.Idle));
        RoutePinLatch.Observe("set-dest", SawtoothSetDest(), pinIsBehind: true);
        RoutePinLatch.DismissDisplay();
        Assert.True(RouteStepDestPolicy.ShouldRetargetMapsDest("list-next", RouteClearancePhase.Idle));
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest("list-align", RouteClearancePhase.Idle));
    }

    [Fact]
    public void Smoke_8_7_step2_Align_must_not_use_B4L_Path_OK_from_S113()
    {
        Assert.True(RouteAlignOrigin.NeedsRecompute("SW-B4L", "#Y-#S113#T"));
        Assert.False(RouteAlignOrigin.NeedsRecompute("SW-B4L", "SW-B4L"));
        Assert.True(RouteAlignOrigin.NeedsRecompute("SW-B4L", null));
    }

    [Fact]
    public void Smoke_8_7_live_IsPinBehind_flip_must_not_block_CLEARED()
    {
        const float length = 7.49f;
        const float frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        RoutePinLatch.Observe("set-dest", SawtoothSetDest(), pinIsBehind: true);
        Assert.True(RoutePinLatch.TravelUsesReverse);

        // Passed the frog: pin now 60 m ahead of the hood. Live IsPinBehind is false.
        const float goldenPast = -60f;
        var liveFlip = RouteClearanceTravel.TravelPastJunctionM(
            goldenPast, length, travelReverse: false);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, liveFlip, length, frog, 120f)));

        var latched = RouteClearanceTravel.TravelPastJunctionM(
            goldenPast, length, RoutePinLatch.EffectiveReverse(livePinIsBehind: false));
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, latched, length, frog, 120f)));
        Assert.Equal(
            RouteClearancePhase.Cleared,
            RouteClearanceEval.Evaluate(
                RouteClearancePhase.AtSwitch,
                new RouteClearanceSample(true, latched, length, frog, 120f)).Phase);
    }

    [Fact]
    public void Smoke_8_7_butt_is_virtual_nose_pin_in_windshield_after_pass_is_CLEARED()
    {
        Assert.Equal(0, ConsistTravelLead.LeadingIndex(0, 0, travelReverse: false));
        Assert.Equal(0, ConsistTravelLead.LeadingIndex(0, 0, travelReverse: true));
        Assert.Equal(0, ConsistTravelLead.LeadingIndex(0, 5, travelReverse: false));
        Assert.Equal(5, ConsistTravelLead.LeadingIndex(0, 5, travelReverse: true));

        const float length = 7.49f;
        const float frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        RoutePinLatch.Observe("set-dest", SawtoothSetDest(), pinIsBehind: true);

        // Butt (rear car) has gone through; pin sits 20 m in front of loco.forward
        // (windshield). That is the reverse cleared side.
        const float goldenFromButt = -20f;
        var leadingPast = RouteClearanceTravel.LeadingEdgePastM(
            goldenFromButt, RoutePinLatch.TravelUsesReverse);
        Assert.Equal(20f, leadingPast);
        Assert.True(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, leadingPast, length, frog, 120f)));

        var stillApproaching = RouteClearanceTravel.LeadingEdgePastM(
            goldenFromLeadingCar: 40f, travelReverse: true);
        Assert.False(RouteClearanceEval.IsClearedOfFrog(
            new RouteClearanceSample(true, stillApproaching, length, frog, 120f)));
    }

    [Fact]
    public void Latch_log_names_pin_and_reverse()
    {
        Assert.Null(RoutePinLatch.FormatLatchLog());
        RoutePinLatch.Observe("set-dest", SawtoothSetDest(), pinIsBehind: true);
        Assert.Equal("T2 route-pin: latch 990152 reverse=1", RoutePinLatch.FormatLatchLog());
    }

    [Fact]
    public void Smoke_13_1_list_load_reset_drops_inbound_latch()
    {
        RoutePinLatch.Observe("set-dest", SawtoothSetDest(), pinIsBehind: true);
        Assert.True(RoutePinLatch.ShowPin);
        Assert.Equal("990152", RoutePinLatch.ResetForNewSwitchList());
        Assert.False(RoutePinLatch.HasLatch);
        Assert.False(RoutePinLatch.ShowPin);
        Assert.Null(RoutePinLatch.ResetForNewSwitchList());
    }

    [Fact]
    public void Smoke_8_7_Next_off_past_switch_hides_pin_Recheck_must_not_steal()
    {
        var setDest = SawtoothSetDest();
        RoutePinLatch.Observe("set-dest", setDest, pinIsBehind: true);
        Assert.True(RoutePinLatch.ShowPin);
        Assert.True(RoutePinLatch.IsArmedForClearance(setDest));

        RoutePinLatch.DismissDisplay();
        Assert.False(RoutePinLatch.ShowPin);
        Assert.True(RoutePinLatch.HasLatch);
        Assert.False(RoutePinLatch.IsArmedForClearance(setDest));
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Align(
                RoutePinLatch.IsArmedForClearance(setDest),
                RouteClearancePhase.Idle));
        Assert.Equal("990152", RoutePinLatch.EffectivePin(CheaperS989Hop()));

        RoutePinLatch.Observe("recheck", CheaperS989Hop());
        Assert.False(RoutePinLatch.ShowPin);
        Assert.Equal("990152", RoutePinLatch.EffectivePin(CheaperS989Hop()));

        RoutePinLatch.Observe("set-dest", setDest, pinIsBehind: true);
        Assert.True(RoutePinLatch.ShowPin);
        Assert.True(RoutePinLatch.IsArmedForClearance(setDest));
    }
}
