using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// List-load pin board: one numbered frog per Switch List step so the cab
/// can drive 1/9…n by hand. After Prep, dest is this-leg B4L (not C4S
/// look-ahead). No dump junction-id goldens — cab harvests the live frog.
/// </summary>
[Collection("StaticSessions")]
public class RoutePinBoardTests
{
    public RoutePinBoardTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_pin_board_SL55_step6_is_this_leg_B4L_not_C4S_lookahead()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            destYardId: "SW",
            buf,
            buf.Length,
            originTrackId: snap.OriginTrackId);
        Assert.True(n >= 2);

        var step6 = FindStep(buf, n, 6);
        Assert.True(step6.HasValue);
        Assert.Equal("SW-B1S", step6!.Value.FromTrackId);
        Assert.Equal("SW-C4S", step6.Value.DestTrackId);
        Assert.NotEqual("SW-B4L", step6.Value.DestTrackId);
        Assert.False(string.IsNullOrEmpty(step6.Value.PinId));
        Assert.False(FindStep(buf, n, 5).HasValue);
        Assert.False(FindStep(buf, n, 2).HasValue);
        Assert.False(FindStep(buf, n, 3).HasValue);
        Assert.False(FindStep(buf, n, 7).HasValue);
        Assert.True(FindStep(buf, n, 8).HasValue);
        Assert.False(FindStep(buf, n, 9).HasValue);
        Assert.False(FindStep(buf, n, 10).HasValue);
        Assert.False(FindStep(buf, n, 12).HasValue);
        foreach (var idx in StepIndexes(buf, n))
        {
            Assert.Contains(idx, new[] { 1, 4, 6, 8, 11 });
        }

        Assert.NotEqual("990152", step6.Value.PinId);
        Assert.NotEqual("1002868", step6.Value.PinId);
        Assert.NotEqual("1003254", step6.Value.PinId);
        Assert.NotEqual("1003160", step6.Value.PinId);

        var inboundB1S = RouteStepDestPolicy.WalkAfterPrepPin(
            snap.Edges,
            snap.Selected,
            "#Y-#S1774#T",
            "SW-B1S",
            "SW",
            id => string.Equals(id, "990152", System.StringComparison.Ordinal));
        var leaveB1SWrongSide = RouteStepDestPolicy.WalkFirstStopPin(
            snap.Edges,
            snap.Selected,
            "SW-B1S",
            "SW-B4L",
            "SW");
        var c4sFar = RouteStepDestPolicy.WalkDestSidePin(
            snap.Edges,
            snap.Selected,
            "SW-B1S",
            "SW-C4S",
            "SW");
        Assert.False(string.IsNullOrEmpty(inboundB1S));
        Assert.Equal(inboundB1S, step6.Value.PinId);
        Assert.NotEqual(leaveB1SWrongSide, step6.Value.PinId);
        Assert.NotEqual(c4sFar, step6.Value.PinId);
        Assert.True(HtpFixtures.TryJunctionXz(in snap, "990152", out var leadX, out var leadZ));
        Assert.True(HtpFixtures.TryJunctionXz(in snap, step6.Value.PinId, out var p6x, out var p6z));
        Assert.False(string.IsNullOrEmpty(leaveB1SWrongSide));
        Assert.True(HtpFixtures.TryJunctionXz(in snap, leaveB1SWrongSide, out var farX, out var farZ));
        var dLead = Dist2(p6x, p6z, leadX, leadZ);
        var dFar = Dist2(p6x, p6z, farX, farZ);
        Assert.True(dLead < dFar, "step 6 must sit on the 1+4 side of the yard");

        var step1 = FindStep(buf, n, 1);
        Assert.True(step1.HasValue);
        Assert.Equal("SW-B4L", step1!.Value.DestTrackId);
        var step4 = FindStep(buf, n, 4);
        Assert.True(step4.HasValue);
        Assert.Equal(step1.Value.PinId, step4.Value.PinId);
        var step8 = FindStep(buf, n, 8);
        Assert.True(step8.HasValue);
        Assert.Equal("SW-C4S", step8!.Value.FromTrackId);
        Assert.Equal("SW-B4L", step8.Value.DestTrackId);
        Assert.Equal(step6.Value.PinId, step8.Value.PinId);
        Assert.NotEqual(step1.Value.PinId, step8.Value.PinId);
        var step11 = FindStep(buf, n, 11);
        Assert.True(step11.HasValue);
        Assert.Equal("SW-B4L", step11!.Value.FromTrackId);
        Assert.Equal("SW-C1O", step11.Value.DestTrackId);
        Assert.False(string.IsNullOrEmpty(step11.Value.PinId));
        Assert.NotEqual("1002868", step11.Value.PinId);
        Assert.NotEqual(step1.Value.PinId, step11.Value.PinId);
        Assert.NotEqual(step6.Value.PinId, step11.Value.PinId);
    }

    [Fact]
    public void Smoke_pin_board_SL55_captions_are_list_step_numbers()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            destYardId: "SW",
            buf,
            buf.Length,
            originTrackId: snap.OriginTrackId);
        Assert.Contains(1, StepIndexes(buf, n));
        Assert.Contains(6, StepIndexes(buf, n));

        var markers = new RoutePinBoardMarker[RoutePinBoard.Capacity];
        var m = RoutePinBoard.Flatten(buf, n, markers, markers.Length);
        Assert.True(m >= 1);

        var step6 = FindStep(buf, n, 6);
        Assert.True(step6.HasValue);
        var cap6 = RoutePinBoard.CaptionForPin(markers, m, step6!.Value.PinId);
        Assert.Contains("6", cap6);

        var step1 = FindStep(buf, n, 1);
        Assert.True(step1.HasValue);
        var cap1 = RoutePinBoard.CaptionForPin(markers, m, step1!.Value.PinId);
        Assert.Equal("1", cap1);

        SwitchListSession.Bind("SW-SL-55", steps);
        RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW", snap.OriginTrackId);
        var shared = RoutePinBoardSession.PinIdForStep(1);
        Assert.False(string.IsNullOrEmpty(shared));
        Assert.True(RoutePinBoardSession.LaterStepOwnsPin(1, shared));
        Assert.False(
            SwitchListRunner.ShouldDisposePinOnCleared(
                new SwitchListStep(
                    1,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Past switch → SW-B4L until CLEARED"),
                new SwitchListStep(
                    2,
                    SwitchListStepKind.TurnAround,
                    "SW",
                    "#Y-#S1774#T",
                    SwitchListDriveFacing.FormatDriveLabel(
                        false,
                        SwitchListDriveFacing.ToTurntableAction,
                        "#Y-#S1774#T")),
                laterStepOwnsPin: true));
        RoutePinBoardSession.DropSpentThrough(1);
        Assert.Equal("4", RoutePinBoardSession.CaptionForPin(shared));
        RoutePinLatch.Observe("set-dest", SawtoothLatchPlan(shared!), pinIsBehind: true);
        RoutePinLatch.DismissDisplay();
        Assert.False(RoutePinLatch.ShowPin);
        Assert.True(
            RouteStepDestPolicy.ShouldRetargetMapsDest(
                "list-next",
                RouteClearancePhase.Idle));
    }

    [Fact]
    public void Smoke_22_12_pin_board_1_and_4_exist_when_loco_already_on_B4L()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            destYardId: "SW",
            buf,
            buf.Length,
            originTrackId: "SW-B4L");
        Assert.True(FindStep(buf, n, 1).HasValue);
        Assert.True(FindStep(buf, n, 4).HasValue);
        Assert.Equal(FindStep(buf, n, 1)!.Value.PinId, FindStep(buf, n, 4)!.Value.PinId);
        Assert.True(FindStep(buf, n, 6).HasValue);
        Assert.NotEqual(FindStep(buf, n, 1)!.Value.PinId, FindStep(buf, n, 6)!.Value.PinId);
    }

    [Fact]
    public void Smoke_pin_board_session_rebuild_from_bound_list()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        SwitchListSession.Bind("SW-SL-55", steps);
        var n = RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW", snap.OriginTrackId);
        Assert.True(n >= 2);
        Assert.True(RoutePinBoardSession.HasBoard);
        Assert.True(RoutePinBoardSession.TryGetEntry(0, out _));
        YmsRouteSessions.ClearAll();
        Assert.False(RoutePinBoardSession.HasBoard);
    }

    [Fact]
    public void Smoke_walk_from_label_after_Prep_B1S_is_B4L_origin()
    {
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        Assert.Equal("SW-B1S", RouteStepDestPolicy.WalkFromLabelTrack(steps, 5, "SW-C4S"));
        Assert.Equal("SW-C4S", RouteStepDestPolicy.WalkFromLabelTrack(steps, 6, "SW-C1O"));
        Assert.Equal("SW-B4L", RouteStepDestPolicy.WalkFromLabelTrack(steps, 0, "#Y-#S1774#T"));
        Assert.True(RouteStepDestPolicy.PreferCorridorDestSidePin(steps, 5));
        Assert.False(RouteStepDestPolicy.PreferCorridorDestSidePin(steps, 0));
        var haulIdx = -1;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Index == 8)
            {
                haulIdx = i;
                break;
            }
        }

        Assert.True(haulIdx > 0);
        Assert.True(RouteStepDestPolicy.NextStepIsLoaderSpot(steps, haulIdx));
        Assert.False(RouteStepDestPolicy.PreferCorridorDestSidePin(steps, haulIdx));
    }

    [Fact]
    public void Smoke_live_pin_caption_keeps_step_number()
    {
        Assert.Equal("1 At switch", RoutePinBoard.FormatLivePinCaption("At switch", "1"));
        Assert.Equal("6", RoutePinBoard.FormatLivePinCaption("PIN", "6"));
        Assert.Equal("PIN", RoutePinBoard.FormatLivePinCaption("PIN", null));
    }

    private static float Dist2(float ax, float az, float bx, float bz)
    {
        var dx = ax - bx;
        var dz = az - bz;
        return (dx * dx) + (dz * dz);
    }

    private static RoutePinBoardEntry? FindStep(RoutePinBoardEntry[] buf, int n, int stepIndex)
    {
        for (var i = 0; i < n; i++)
        {
            if (buf[i].StepIndex == stepIndex)
            {
                return buf[i];
            }
        }

        return null;
    }

    private static int[] StepIndexes(RoutePinBoardEntry[] buf, int n)
    {
        var ids = new int[n];
        for (var i = 0; i < n; i++)
        {
            ids[i] = buf[i].StepIndex;
        }

        return ids;
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
            LoadTrackId = "SW-B4L",
            LoadCargoLabel = "Wood Chips",
        };

    private static PathPlanResult SawtoothLatchPlan(string pinId) =>
        new(
            PathCheckStatus.Misaligned,
            new[] { "SW-B4L", "#Y-#S1774#T" },
            new[] { new PathJunctionEval(pinId, 1, 0) },
            misalignedCount: 1,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 1f,
            junctionFirstStop: new PathJunctionFirstStop(pinId, 1, "SW-B4L", "#Y-#S1774#T"));
}
