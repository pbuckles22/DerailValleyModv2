using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab lock: after take 40, windshield 60 never became Next because Evaluate
/// used a 12 m In→Out chord. DVRouteManager / v1 keep boards by live hop
/// <c>RailTrack</c> instance. Same decision in Core: TrackId membership.
/// </summary>
public class HtpLiveHopEvaluateTests
{
    private const int FortyTrack = 11;
    private const int SixtyTrack = 22;

    [Fact]
    public void Smoke_sw_leave_after_take_40_sixty_on_hop_track_stays_next_even_off_chord()
    {
        var segs = Corridor();
        var forty = Board(1398156, 0f, 80f, 40f, FortyTrack);
        var sixty = Board(1402212, 50f, 380f, 60f, SixtyTrack);

        Assert.False(
            PostedPathAheadGate.IsOnAnyCorridor(sixty.X, sixty.Z, segs, segs.Length),
            "60 sits 50 m off the hop chord — the cab n=0 fail");
        Assert.True(PostedPathAheadGate.IsBoardOnPath(in sixty, segs, segs.Length));
        Assert.True(PostedPathAheadGate.IsBoardOnPath(in forty, segs, segs.Length));

        var funnel = new PostedLimitFunnel();
        funnel.Warm(new[] { forty, sixty }, 0f, 0f, 0f, 0f, 0f, 1f);
        funnel.SetTravel(0f, 0f, 1f, speedKmh: 20f, 0f, 0f, 0f);

        funnel.Evaluate(
            new[] { forty, sixty },
            segs,
            segs.Length,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            speedKmh: 20f,
            locoTrackId: FortyTrack);
        var sit = funnel.ToSnapshot();
        Assert.Null(sit.Kmh);
        Assert.Equal(40f, sit.NextKmh);

        funnel.Evaluate(
            new[] { forty, sixty },
            segs,
            segs.Length,
            0f,
            0f,
            90f,
            0f,
            0f,
            1f,
            speedKmh: 20f,
            locoTrackId: FortyTrack);
        var past = funnel.ToSnapshot();
        Assert.Equal(40f, past.Kmh);
        Assert.Equal(60f, past.NextKmh);
    }

    [Fact]
    public void SelectSegmentIndex_prefers_loco_track_id_when_off_chord()
    {
        var segs = Corridor();
        Assert.Equal(
            1,
            PostedPathAheadGate.SelectSegmentIndex(50f, 300f, segs, segs.Length, SixtyTrack));
        Assert.Equal(
            0,
            PostedPathAheadGate.SelectSegmentIndex(50f, 40f, segs, segs.Length, FortyTrack));
    }

    private static PathSegmentAlong[] Corridor() =>
        new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 200f, FortyTrack),
            new PathSegmentAlong(200f, 0f, 0f, 200f, 0f, 1f, 400f, SixtyTrack),
        };

    private static ParsedPostedBoard Board(int id, float x, float z, float kmh, int trackId) =>
        new ParsedPostedBoard(
            id,
            x,
            0f,
            z,
            0f,
            -1f,
            1f,
            0f,
            kmh,
            kmh,
            false,
            false,
            trackId);
}
