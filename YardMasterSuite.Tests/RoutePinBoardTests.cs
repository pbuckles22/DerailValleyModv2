using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Pin board at list-load: every pin-leg. Pull-out after Prep is this-leg
/// B4L (no 6L/6M C4S split). Leave-TT still shares 990152 as 1+4.
/// </summary>
[Collection("StaticSessions")]
public class RoutePinBoardTests
{
    public RoutePinBoardTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_pin_board_SL55_step6_pull_out_is_B4L_not_C4S_first_stop()
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
        Assert.Equal("SW-B4L", step6.Value.MapsDest);
        Assert.False(string.IsNullOrEmpty(step6.Value.MapsPinId));
        Assert.NotEqual("989976", step6.Value.MapsPinId);
        Assert.False(step6.Value.Split);

        var markers = new RoutePinBoardMarker[RoutePinBoard.Capacity];
        var m = RoutePinBoard.Flatten(buf, n, markers, markers.Length);
        Assert.True(m >= 1);
        var caps = FlattenCaptions(markers, m);
        Assert.DoesNotContain("6M", caps);
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
        Assert.Contains("split=0", RoutePinBoard.FormatEntryLog(step6));
        Assert.DoesNotContain("SPLIT", RoutePinBoard.FormatDeskLine(step6));
        Assert.Contains("B4L", RoutePinBoard.FormatDeskLine(step6));
        Assert.DoesNotContain("C4S", RoutePinBoard.FormatDeskLine(step6));
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
