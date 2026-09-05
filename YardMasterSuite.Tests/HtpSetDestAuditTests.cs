using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Set-dest / pin-corridor audit for office tickets + SL-55 cab harvest.
/// Ticket screenshots 2026-09-04: FH-82 haul; SL-55 shunting
/// <c>[ B1S, C3S ] → B4L → C1O</c>. Cab PASS <c>2.13.2.4.4</c>: CLEARED then to-TT.
/// </summary>
[Collection("StaticSessions")]
public class HtpSetDestAuditTests
{
    public HtpSetDestAuditTests() => YmsRouteSessions.ClearAll();

    public const string Sl55ViaSpur = "SW-B4L";
    public const string Sl55PrepDest = "SW-C1O";
    public const string Sl55Turntable = "#Y-#S1774#T";
    public const string SawtoothPin = "990152";

    /// <summary>
    /// Office booklet SL-55: past-switch via B4L → TT → Prep C1O (not Prep-first freight).
    /// </summary>
    [Fact]
    public void Smoke_SL_55_planner_past_switch_via_B4L_to_TT_then_Prep_C1O()
    {
        var job = new JobSummary
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = Sl55PrepDest,
            DestTrackId = Sl55PrepDest,
            NeedsTurnAround = true,
            TurntableTrackId = Sl55Turntable,
            TurntablePivotTrackId = Sl55ViaSpur,
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = Sl55Turntable,
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.True(steps!.Count >= 4);
        Assert.Equal(SwitchListStepKind.Transit, steps[0].Kind);
        Assert.Equal(Sl55ViaSpur, steps[0].DestTrackId);
        Assert.Contains("until CLEARED", steps[0].Label);
        Assert.Equal(SwitchListStepKind.TurnAround, steps[1].Kind);
        Assert.Equal(Sl55Turntable, steps[1].DestTrackId);
        Assert.Contains("to TT", steps[1].Label);
        var prep = steps.First(s => s.Kind == SwitchListStepKind.Prep);
        Assert.Equal(Sl55PrepDest, prep.DestTrackId);
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, 0, out _, out var corridor));
        Assert.Equal(Sl55Turntable, corridor);
    }

    [Fact]
    public void Smoke_SL_55_harvest_graph_pin_corridor_B4L_to_TT()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        Assert.Equal("SW", snap.YardId);
        Assert.Equal(Sl55ViaSpur, snap.OriginTrackId);
        Assert.Equal(Sl55Turntable, snap.DestTrackId);
        Assert.Equal(SawtoothPin, snap.PinJunctionId);
        Assert.Equal(PathPlanMode.Yard, snap.Mode);
        Assert.True(snap.Edges.Count > 100);

        var graph = HtpFixtures.LoadGraphSw20260904();
        Assert.True(graph.Tracks.Count >= 20);
        Assert.True(graph.Junctions.Count >= 10);

        var spec = HtpFixtures.ToSpec(in snap);
        var plan = RouteCorridorDrive.Plan(in spec);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal(Sl55ViaSpur, plan.TrackIds[0]);
        Assert.Equal(Sl55Turntable, plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.NotNull(plan.JunctionFirstStop);
        var replanPin = RouteCorridorDrive.PickPin(plan);
        Assert.False(string.IsNullOrEmpty(replanPin));
        Assert.Equal(plan.JunctionFirstStop!.Value.JunctionId, replanPin);
        // Cab latched harvest header pin 990152; frozen-sel replan first-stops 1576058.
        // That disagree is the SL-55 lock: do not treat TT-corridor first-stop as gospel.
        Assert.Equal("1576058", replanPin);
        Assert.NotEqual(snap.PinJunctionId, replanPin);
        Assert.True(
            RouteStepDestPolicy.CorridorPinDisagreesWithApproach(
                new PathPlanResult(
                    PathCheckStatus.Misaligned,
                    new[] { Sl55ViaSpur, "#Y-#S200#T" },
                    new[] { new PathJunctionEval(SawtoothPin, 1, 0) },
                    1,
                    1,
                    true,
                    4f,
                    junctionFirstStop: new PathJunctionFirstStop(
                        SawtoothPin,
                        1,
                        Sl55ViaSpur,
                        "#Y-#S200#T")),
                plan));
    }

    [Fact]
    public void Smoke_FH_82_and_SL_55_list_load_sets_pin_corridor_not_approach_recheck()
    {
        // Maps dest = TT (pin-corridor). Recheck to B4L steals the sawtooth pin.
        Assert.True(RouteStepDestPolicy.ShouldSetPinCorridorDest("list-load"));
        Assert.False(RouteStepDestPolicy.ShouldRetargetMapsDest(
            "list-load",
            RouteClearancePhase.Idle,
            SwitchListStepKind.Transit));

        var steps = new[]
        {
            new SwitchListStep(
                1,
                SwitchListStepKind.Transit,
                "SW",
                Sl55ViaSpur,
                "Set Reverse · Past switch → " + Sl55ViaSpur + " until CLEARED",
                bindNeedsReverse: true),
            new SwitchListStep(
                2,
                SwitchListStepKind.TurnAround,
                "SW",
                Sl55Turntable,
                SwitchListDriveFacing.FormatDriveLabel(
                    false,
                    SwitchListDriveFacing.ToTurntableAction,
                    Sl55Turntable)),
            new SwitchListStep(3, SwitchListStepKind.Prep, "SW", Sl55PrepDest, "Prep → " + Sl55PrepDest),
        };
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, 0, out _, out var corridor));
        Assert.Equal(Sl55Turntable, corridor);
    }

    [Fact]
    public void Smoke_SL_55_cab_PASS_CLEARED_then_to_tt_not_roll_through()
    {
        // Cab 2.13.2.4.4: At switch → CLEARED → cleared-next → arm-go step 2 → stop-tt.
        var steps = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", Sl55ViaSpur, "Past switch"),
            new SwitchListStep(
                2,
                SwitchListStepKind.TurnAround,
                "SW",
                Sl55Turntable,
                SwitchListDriveFacing.FormatDriveLabel(
                    false,
                    SwitchListDriveFacing.ToTurntableAction,
                    Sl55Turntable)),
            new SwitchListStep(3, SwitchListStepKind.Prep, "SW", Sl55PrepDest, "Prep"),
        };

        Assert.Equal(
            SwitchListYardChainAction.StopGoCompleteCleared,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.Cleared,
                prepAtSpur: false,
                hasPlan: true));

        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true));

        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: true));
    }

    [Fact]
    public void Smoke_FH_82_approach_pin_agrees_with_tt_corridor_keeps_sawtooth()
    {
        var shared = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { Sl55ViaSpur, "#Y-#S989#T", Sl55Turntable },
            new[] { new PathJunctionEval(SawtoothPin, 1, 0) },
            misalignedCount: 1,
            reverseCount: 1,
            lastHopRequiresReverse: true,
            totalCost: 10f,
            junctionFirstStop: new PathJunctionFirstStop(SawtoothPin, 1, Sl55ViaSpur, "#Y-#S989#T"));
        Assert.False(RouteStepDestPolicy.CorridorPinDisagreesWithApproach(shared, shared));
        Assert.Equal(
            SawtoothPin,
            RouteStepDestPolicy.PickPastSwitchPinJunctionId(shared, shared));
    }
}
