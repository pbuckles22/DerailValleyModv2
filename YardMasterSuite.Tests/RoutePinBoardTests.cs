using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Pin board at list-load: every pin-leg's label dest pin vs Maps dest pin.
/// No driving. SL-55 step 6 is the known split (B4L vs C4S).
/// </summary>
[Collection("StaticSessions")]
public class RoutePinBoardTests
{
    public RoutePinBoardTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_pin_board_SL55_step6_label_B4L_splits_from_maps_C4S()
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
            buf.Length);
        Assert.True(n >= 3);

        RoutePinBoardEntry? step6 = null;
        RoutePinBoardEntry? step1 = null;
        for (var i = 0; i < n; i++)
        {
            if (buf[i].StepIndex == 6)
            {
                step6 = buf[i];
            }

            if (buf[i].StepIndex == 1)
            {
                step1 = buf[i];
            }
        }

        Assert.True(step1.HasValue);
        Assert.Equal("990152", step1!.Value.MapsPinId);
        Assert.False(step1.Value.Split);

        Assert.True(step6.HasValue);
        Assert.Equal("SW-B4L", step6!.Value.LabelDest);
        Assert.Equal("SW-C4S", step6.Value.MapsDest);
        Assert.Equal("989976", step6.Value.MapsPinId);
        Assert.False(string.IsNullOrEmpty(step6.Value.LabelPinId));
        Assert.NotEqual("989976", step6.Value.LabelPinId);
        Assert.NotEqual("1003160", step6.Value.LabelPinId);
        Assert.True(step6.Value.Split);
        Assert.True(RoutePinBoard.CountSplits(buf, n) >= 1);

        var markers = new RoutePinBoardMarker[RoutePinBoard.Capacity];
        var m = RoutePinBoard.Flatten(buf, n, markers, markers.Length);
        Assert.True(m >= 3);
        Assert.Contains("6L", FlattenCaptions(markers, m));
        Assert.Contains("6M", FlattenCaptions(markers, m));
        Assert.Contains("1+4", RoutePinBoard.CaptionForPin(markers, m, "990152") ?? "");
        Assert.Equal(
            "1+4 At switch",
            RoutePinBoard.FormatLivePinCaption("At switch", "1+4"));
    }

    [Fact]
    public void Smoke_pin_board_session_rebuild_from_bound_list()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        SwitchListSession.Bind("SW-SL-55", steps);
        var n = RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW");
        Assert.True(n >= 3);
        Assert.True(RoutePinBoardSession.SplitCount >= 1);
        Assert.True(RoutePinBoardSession.HasBoard);
        Assert.True(RoutePinBoardSession.TryGetEntry(0, out _));
        RoutePinBoardEntry step6 = default;
        var found = false;
        for (var i = 0; i < RoutePinBoardSession.EntryCount; i++)
        {
            Assert.True(RoutePinBoardSession.TryGetEntry(i, out var e));
            if (e.StepIndex != 6)
            {
                continue;
            }

            step6 = e;
            found = true;
            break;
        }

        Assert.True(found);
        Assert.Contains("split=1", RoutePinBoard.FormatEntryLog(step6));
        Assert.Contains("SPLIT", RoutePinBoard.FormatDeskLine(step6));
        Assert.Contains("B4L", RoutePinBoard.FormatDeskLine(step6));
        Assert.Contains("C4S", RoutePinBoard.FormatDeskLine(step6));
    }

    private static string FlattenCaptions(RoutePinBoardMarker[] markers, int count)
    {
        var s = "";
        for (var i = 0; i < count; i++)
        {
            s += markers[i].Caption + ",";
        }

        return s;
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
}
