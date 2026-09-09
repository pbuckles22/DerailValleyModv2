using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Set-dest / pin-corridor audit for office tickets + SL-55 cab harvest.
/// Ticket screenshots 2026-09-04: FH-82 haul; SL-55 shunting
/// <c>[ B1S, C3S ] → B4L → C1O</c> (live DV second spur is C4S). Cab PASS <c>2.13.2.4.4</c>: CLEARED then to-TT.
/// </summary>
[Collection("StaticSessions")]
public class HtpSetDestAuditTests
{
    public HtpSetDestAuditTests() => YmsRouteSessions.ClearAll();

    public const string Sl55ViaSpur = "SW-B4L";
    public const string Sl55FirstPickup = "SW-B1S";
    /// <summary>Live SL-55 second spur (booklet C3S; DV C4S).</summary>
    public const string Sl55SecondPickup = "SW-C4S";
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

    /// <summary>
        /// Cab: Prep B1S → Past switch B4L until CLEARED → Past switch C4S until CLEARED → Prep C4S.
    /// Frog pin between pickups, not ReverseInto into the world.
    /// </summary>
    [Fact]
    public void Smoke_SL_55_planner_two_Prep_CLEARED_staging_then_Transit_C1O()
    {
        var job = Sl55MultiPickupJob();
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);

        var preps = steps!.Where(s => s.Kind == SwitchListStepKind.Prep).Select(s => s.DestTrackId).ToArray();
        Assert.Equal(new[] { Sl55FirstPickup, Sl55SecondPickup }, preps);
        Assert.DoesNotContain(steps, s => s.Kind == SwitchListStepKind.ReverseInto);

        var prepIdx = Array.FindIndex(steps.ToArray(), s => s.Kind == SwitchListStepKind.Prep);
        Assert.Equal(SwitchListStepKind.Transit, steps[prepIdx + 1].Kind);
        Assert.Equal(Sl55ViaSpur, steps[prepIdx + 1].DestTrackId);
        Assert.Contains("until CLEARED", steps[prepIdx + 1].Label);
        Assert.True(SwitchListRunner.StepNeedsPinClearance(steps[prepIdx + 1].Kind));
        Assert.Equal(SwitchListStepKind.Transit, steps[prepIdx + 2].Kind);
        Assert.Equal(Sl55SecondPickup, steps[prepIdx + 2].DestTrackId);
        Assert.Contains("until CLEARED", steps[prepIdx + 2].Label);
        Assert.True(SwitchListRunner.StepNeedsPinClearance(steps[prepIdx + 2].Kind));
        Assert.Equal(SwitchListStepKind.Prep, steps[prepIdx + 3].Kind);
        Assert.Equal(Sl55SecondPickup, steps[prepIdx + 3].DestTrackId);
        Assert.Equal(SwitchListStepKind.Transit, steps[prepIdx + 4].Kind);
        Assert.Equal(Sl55PrepDest, steps[prepIdx + 4].DestTrackId);
        Assert.Equal(SwitchListStepKind.Delivery, steps[prepIdx + 5].Kind);
    }

    /// <summary>
    /// Cab 2.13.2.5.4: near B1S/B4L frog CLEARED then ArmGo Prep C4S with pin
    /// idle — skipped the far C4S approach. Next row after B4L must still be a
    /// Past-switch pin-leg to C4S, not Prep.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_5_after_B4L_cleared_next_is_C4S_past_switch_not_prep()
    {
        var job = Sl55MultiPickupJob();
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);

        var prep0 = Array.FindIndex(steps!.ToArray(), s => s.Kind == SwitchListStepKind.Prep);
        var b4l = steps[prep0 + 1];
        var c4Approach = steps[prep0 + 2];
        Assert.Equal(Sl55ViaSpur, b4l.DestTrackId);
        Assert.True(SwitchListRunner.StepNeedsPinClearance(b4l.Kind));
        Assert.Equal(SwitchListStepKind.Transit, c4Approach.Kind);
        Assert.Equal(Sl55SecondPickup, c4Approach.DestTrackId);
        Assert.Contains("Past switch", c4Approach.Label);
        Assert.Contains("until CLEARED", c4Approach.Label);
        Assert.False(c4Approach.BindNeedsReverse);
        Assert.True(SwitchListRunner.StepNeedsPinClearance(c4Approach.Kind));
        Assert.NotEqual(SwitchListStepKind.Prep, c4Approach.Kind);
        Assert.Equal(SwitchListStepKind.Prep, steps[prep0 + 3].Kind);
        Assert.Equal(Sl55SecondPickup, steps[prep0 + 3].DestTrackId);
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, prep0 + 2, out _, out var corridor));
        Assert.Equal(Sl55SecondPickup, corridor);
        Assert.True(SwitchListYardChain.ShouldAutoNextAfterCleared(steps, prep0 + 1, hasNextStep: true));
        Assert.True(SwitchListYardChain.InYardPrepScope(steps, prep0 + 2));
    }

    /// <summary>
    /// Cab 2.13.2.5: after B1S couple, step 6 stayed Set Reverse and shoved
    /// into a foreign cut (cars=3→8). Pull-out is Set Forward to B4L.
    /// </summary>
    [Fact]
    public void Smoke_SL_55_after_first_couple_B4L_staging_is_Set_Forward()
    {
        var job = Sl55MultiPickupJob();
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);

        var prep0 = Array.FindIndex(steps!.ToArray(), s => s.Kind == SwitchListStepKind.Prep);
        var between = steps[prep0 + 1];
        Assert.Equal(SwitchListStepKind.Transit, between.Kind);
        Assert.Equal(Sl55ViaSpur, between.DestTrackId);
        Assert.False(between.BindNeedsReverse);
        Assert.Contains(SwitchListDriveFacing.Forward, between.Label);
        Assert.DoesNotContain(SwitchListDriveFacing.Reverse, between.Label);
        Assert.Contains("Past switch", between.Label);
        Assert.Contains("until CLEARED", between.Label);
    }

    [Fact]
    public void Smoke_SL_55_harvest_graph_plans_B1S_and_C4S_via_B4L()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var b1 = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            Sl55FirstPickup,
            Sl55ViaSpur,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, b1.Status);
        Assert.Equal(Sl55FirstPickup, b1.TrackIds[0]);
        Assert.Equal(Sl55ViaSpur, b1.TrackIds[b1.TrackIds.Count - 1]);

        var c4 = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            Sl55ViaSpur,
            Sl55SecondPickup,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, c4.Status);
        Assert.Equal(Sl55ViaSpur, c4.TrackIds[0]);
        Assert.Equal(Sl55SecondPickup, c4.TrackIds[c4.TrackIds.Count - 1]);

        var toDest = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            Sl55SecondPickup,
            Sl55PrepDest,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, toDest.Status);
        Assert.Equal(Sl55SecondPickup, toDest.TrackIds[0]);
        Assert.Equal(Sl55PrepDest, toDest.TrackIds[toDest.TrackIds.Count - 1]);
    }

    /// <summary>
    /// Between-pickup Past switch (after first Prep): pin-corridor dest = next Prep (C4S).
    /// Do not use the earlier TT-approach B4L row (corridor = TT).
    /// </summary>
    [Fact]
    public void Smoke_SL_55_between_pickup_pin_corridor_is_second_Prep()
    {
        var job = Sl55MultiPickupJob();
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);

        var prep0 = Array.FindIndex(steps!.ToArray(), s => s.Kind == SwitchListStepKind.Prep);
        Assert.True(prep0 >= 0);
        var between = -1;
        for (var i = prep0 + 1; i < steps.Count; i++)
        {
            if (steps[i].Kind == SwitchListStepKind.Transit
                && steps[i].DestTrackId == Sl55ViaSpur)
            {
                between = i;
                break;
            }
        }

        Assert.True(between > prep0);
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, between, out _, out var corridor));
        Assert.Equal(Sl55SecondPickup, corridor);
        Assert.True(SwitchListRunner.StepNeedsPinClearance(steps[between].Kind));
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
        // Same calculator as cab: PickPin is JunctionFirstStop, not a second picker.
        Assert.Equal(plan.JunctionFirstStop!.Value.JunctionId, replanPin);
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

    private static JobSummary Sl55MultiPickupJob() =>
        new()
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = Sl55FirstPickup,
            AdditionalPickupTrackIds = new[] { Sl55SecondPickup },
            DestTrackId = Sl55PrepDest,
            NeedsTurnAround = true,
            TurntableTrackId = Sl55Turntable,
            TurntablePivotTrackId = Sl55ViaSpur,
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = Sl55Turntable,
            NeedsReverseInto = true,
            ReverseIntoTrackId = Sl55ViaSpur,
        };
}
