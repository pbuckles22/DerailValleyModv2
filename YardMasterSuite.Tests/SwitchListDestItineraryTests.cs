using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// W1–W5 dest itinerary, harvest first-stop walks, ShowPin after dismiss.
/// Live latch uses Maps dest; English prints label dest.
/// </summary>
[Collection("StaticSessions")]
public class SwitchListDestItineraryTests
{
    public SwitchListDestItineraryTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void W1_SL55_list_next_Maps_dest_itinerary()
    {
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        Assert.Equal(9, steps!.Count);

        // label dest | Maps dest (list-next) | pin-corridor | needs pin
        AssertRow(steps, 0, "SW-B4L", "#Y-#S1774#T", pinCorridor: true, needsPin: true);
        AssertRow(steps, 1, "#Y-#S1774#T", "#Y-#S1774#T", pinCorridor: false, needsPin: false);
        AssertRow(steps, 2, "#Y-#S1774#T", "#Y-#S1774#T", pinCorridor: false, needsPin: false);
        AssertRow(steps, 3, "#Y-#S1512#T", "SW-B1S", pinCorridor: true, needsPin: true);
        AssertRow(steps, 4, "SW-B1S", "SW-B1S", pinCorridor: false, needsPin: false);
        AssertRow(steps, 5, "SW-B4L", "SW-C4S", pinCorridor: true, needsPin: true);
        AssertRow(steps, 6, "SW-C4S", "SW-C4S", pinCorridor: false, needsPin: false);
        AssertRow(steps, 7, "SW-C1O", "SW-C1O", pinCorridor: false, needsPin: true);
        AssertRow(steps, 8, "SW-C1O", "SW-C1O", pinCorridor: false, needsPin: false);
    }

    [Fact]
    public void W2_FH82_list_next_Maps_dest_itinerary()
    {
        var steps = SwitchListPlanner.Build(Fh82LiveJob());
        Assert.NotNull(steps);
        Assert.Equal(7, steps!.Count);

        AssertRow(steps, 0, "SW-B4L", "#Y-#S1774#T", pinCorridor: true, needsPin: true);
        AssertRow(steps, 1, "#Y-#S1774#T", "#Y-#S1774#T", pinCorridor: false, needsPin: false);
        AssertRow(steps, 2, "#Y-#S1774#T", "#Y-#S1774#T", pinCorridor: false, needsPin: false);
        AssertRow(steps, 3, "#Y-#S1512#T", "SW-C1O", pinCorridor: true, needsPin: true);
        AssertRow(steps, 4, "SW-C1O", "SW-C1O", pinCorridor: false, needsPin: false);
        AssertRow(steps, 5, "GF-D5I", "GF-D5I", pinCorridor: false, needsPin: true);
        AssertRow(steps, 6, "GF-D5I", "GF-D5I", pinCorridor: false, needsPin: false);
    }

    [Fact]
    public void W3_SL55_pin_corridor_first_stops_on_harvest()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);

        AssertPinWalk(snap, steps!, 0, "SW-B4L", "#Y-#S1774#T", "990152");
        AssertPinWalk(snap, steps!, 3, "#Y-#S1774#T", "SW-B1S", "990152");
        // First-stop on B1S→C4S is behind 989976. Dest-side last (not 1003030)
        // is pin-policy 5.21, not this walk.
        AssertPinWalk(snap, steps!, 5, "SW-B1S", "SW-C4S", "989976");
    }

    [Fact]
    public void W3_FH82_pin_corridor_first_stops_on_harvest()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Fh82LiveJob());
        Assert.NotNull(steps);

        AssertPinWalk(snap, steps!, 0, "SW-B4L", "#Y-#S1774#T", "990152");
        AssertPinWalk(snap, steps!, 3, "#Y-#S1774#T", "SW-C1O", "990152");
    }

    [Fact]
    public void W5_SL55_dismiss_then_set_dest_showpin_is_walk_first_stop()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        AssertShowPinAfterDismiss(snap, steps!, "SW-SL-55", 0, "990152");
        AssertShowPinAfterDismiss(snap, steps!, "SW-SL-55", 3, "990152");
        AssertShowPinAfterDismiss(snap, steps!, "SW-SL-55", 5, "989976");
    }

    [Fact]
    public void W5_FH82_dismiss_then_set_dest_showpin_is_walk_first_stop()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Fh82LiveJob());
        Assert.NotNull(steps);
        AssertShowPinAfterDismiss(snap, steps!, "SW-FH-82", 0, "990152");
        AssertShowPinAfterDismiss(snap, steps!, "SW-FH-82", 3, "990152");
    }

    [Fact]
    public void W1_list_next_is_Set_not_Recheck()
    {
        Assert.Equal(MapsDestKind.Set, RouteStepDestPolicy.DestCommandKindAfterRetarget("list-next"));
        Assert.Equal(MapsDestKind.Set, RouteStepDestPolicy.DestCommandKindAfterRetarget("list-load"));
        Assert.Equal(MapsDestKind.Recheck, RouteStepDestPolicy.DestCommandKindAfterRetarget("list-align"));
    }

    private static void AssertRow(
        System.Collections.Generic.IReadOnlyList<SwitchListStep> steps,
        int index,
        string labelDest,
        string mapsDest,
        bool pinCorridor,
        bool needsPin)
    {
        var step = steps[index];
        Assert.Equal(labelDest, step.DestTrackId);
        Assert.Equal(needsPin, SwitchListRunner.StepNeedsPinClearance(step.Kind));
        Assert.True(
            RouteStepDestPolicy.TryMapsDestForListProgress(
                steps,
                index,
                "list-next",
                out var track,
                out var kind,
                out var corridor));
        Assert.Equal(MapsDestKind.Set, kind);
        Assert.Equal(mapsDest, track);
        Assert.Equal(pinCorridor, corridor);
    }

    private static void AssertPinWalk(
        RouteHarvestSnapshot snap,
        System.Collections.Generic.IReadOnlyList<SwitchListStep> steps,
        int index,
        string fromTrack,
        string mapsDest,
        string expectedPin)
    {
        Assert.True(
            RouteStepDestPolicy.TryMapsDestForListProgress(
                steps, index, "list-next", out var maps, out _, out var corridor));
        Assert.True(corridor);
        Assert.Equal(mapsDest, maps);
        Assert.Equal(fromTrack, RouteStepDestPolicy.WalkFromTrack(steps, index, maps));
        var pin = RouteStepDestPolicy.WalkFirstStopPin(
            snap.Edges,
            snap.Selected,
            fromTrack,
            mapsDest,
            destYardId: "SW");
        Assert.False(string.IsNullOrEmpty(pin));
        Assert.Equal(expectedPin, pin);
    }

    private static void AssertShowPinAfterDismiss(
        RouteHarvestSnapshot snap,
        System.Collections.Generic.IReadOnlyList<SwitchListStep> steps,
        string jobId,
        int index,
        string expectedPin)
    {
        YmsRouteSessions.ClearAll();
        SwitchListSession.Bind(jobId, steps);
        for (var i = 0; i < index; i++)
        {
            Assert.True(SwitchListSession.TryAdvance());
        }

        Assert.Equal(index, SwitchListSession.CurrentIndex);
        Assert.True(
            RouteStepDestPolicy.TryMapsDestForListProgress(
                steps, index, "list-next", out var maps, out _, out var corridor));
        Assert.True(corridor);
        var from = RouteStepDestPolicy.WalkFromTrack(steps, index, maps);
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            from,
            maps,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal(expectedPin, SwitchListRouteLeg.PickPinJunctionId(plan));

        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: false);
        RoutePinLatch.DismissDisplay();
        Assert.False(RoutePinLatch.ShowPin);
        RoutePinLatch.Observe(
            "set-dest",
            plan,
            pinIsBehind: false,
            junctionAlreadyCleared: _ => true);
        Assert.True(RoutePinLatch.ShowPin);
        Assert.False(RoutePinLatch.DisplayDismissed);
        Assert.Equal(expectedPin, RoutePinLatch.Id);
        YmsRouteSessions.ClearAll();
    }

    private static JobSummary Sl55LiveMultiPickupJob() =>
        new()
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
        };

    private static JobSummary Fh82LiveJob() =>
        new()
        {
            JobId = "SW-FH-82",
            JobTypeLabel = "FH",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C1O",
            DestTrackId = "GF-D5I",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
        };
}
