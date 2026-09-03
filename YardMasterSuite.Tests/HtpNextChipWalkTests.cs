using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.4 Next-chip — cab smoke 2.13.1.14: sticky 40 + span 60@100 logged
/// next=— while Limit snapped at take 40@0. Display only; Evaluate stays.
/// </summary>
public class HtpNextChipWalkTests
{
    private const int TrackId = 7;

    [Fact]
    public void Smoke_sticky_40_span_60_at_100m_next_is_60_with_meters_not_dash()
    {
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f, TrackId),
        };
        var forty = SpanBoard(1, 0f, 40f);
        var sixty = SpanBoard(2, 100f, 60f);
        var roster = new[] { forty, sixty };

        var funnel = new PostedLimitFunnel();
        funnel.Warm(roster, 0f, 0f, 0f, 0f, 0f, 1f, preserveSticky: 40f);
        LockTravel(funnel);
        funnel.Evaluate(
            roster,
            segs,
            1,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            speedKmh: 20f,
            locoTrackId: TrackId,
            locoSpanMeters: 0f);

        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
        Assert.True(
            snap.NextAlongMeters is float along && along > 90f && along < 110f,
            "60 must sit ~100 m ahead, not a dash");

        var ahead = PostedBoardTelemetry.FormatAhead(
            snap.Kmh ?? 40f,
            20f,
            snap.NextKmh,
            snap.NextAlongMeters,
            new[] { new AheadBoard(60f, snap.NextAlongMeters ?? 0f, "span") },
            1,
            alongSrc: "span");
        Assert.DoesNotContain("next=—", ahead);
        Assert.Contains("next=60", ahead);

        var hud = SpeedLimitState.Resolve(
            hasUsableLoco: true,
            postedKmh: snap.Kmh,
            nextKmh: snap.NextKmh,
            nextAlongMeters: snap.NextAlongMeters);
        Assert.Contains("Next 60", SpeedLimitDisplay.FormatHudOrEmpty(
            20f,
            hud.LimitKmh,
            hud.NextKmh,
            hud.NextAlongMeters));
        Assert.Contains("100m", SpeedLimitDisplay.FormatHudOrEmpty(
            20f,
            hud.LimitKmh,
            hud.NextKmh,
            hud.NextAlongMeters));
    }

    [Fact]
    public void Smoke_increase_board_still_shows_next_when_evaluate_path_known()
    {
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f, TrackId),
        };
        var sixty = SpanBoard(2, 100f, 60f);
        var funnel = new PostedLimitFunnel();
        funnel.Warm(new[] { sixty }, 0f, 0f, 0f, 0f, 0f, 1f, preserveSticky: 40f);
        funnel.Evaluate(
            new[] { sixty },
            segs,
            1,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            speedKmh: 20f,
            locoTrackId: TrackId,
            locoSpanMeters: 0f);

        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
        Assert.True(snap.NextAlongMeters is float along && along > 90f);
    }

    [Fact]
    public void Smoke_behind_boards_do_not_evict_span_60_from_next()
    {
        const float locoSpan = 200f;
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f, TrackId),
        };
        var roster = new ParsedPostedBoard[PostedLimitFilo.MaxDepth + 2];
        for (var i = 0; i < PostedLimitFilo.MaxDepth + 1; i++)
        {
            roster[i] = SpanBoard(10 + i, locoSpan - (20f * (i + 1)), 40f);
        }

        roster[roster.Length - 1] = SpanBoard(2, locoSpan + 100f, 60f);

        var funnel = new PostedLimitFunnel();
        funnel.Warm(roster, 0f, 0f, locoSpan, 0f, 0f, 1f, preserveSticky: 40f);
        LockTravel(funnel);
        funnel.Evaluate(
            roster,
            segs,
            1,
            0f,
            0f,
            locoSpan,
            0f,
            0f,
            1f,
            speedKmh: 20f,
            locoTrackId: TrackId,
            locoSpanMeters: locoSpan);

        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
        Assert.True(snap.NextAlongMeters is float along && along > 90f && along < 110f);
    }

    [Fact]
    public void Smoke_behind_take_still_updates_limit_while_next_stays_60()
    {
        const float locoSpan = 200f;
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f, TrackId),
        };
        var behindForty = SpanBoard(1, locoSpan - 139f, 40f);
        var sixty = SpanBoard(2, locoSpan + 100f, 60f);
        var roster = new[] { behindForty, sixty };

        var funnel = new PostedLimitFunnel();
        funnel.Warm(roster, 0f, 0f, locoSpan, 0f, 0f, 1f);
        LockTravel(funnel);
        funnel.Evaluate(
            roster,
            segs,
            1,
            0f,
            0f,
            locoSpan,
            0f,
            0f,
            1f,
            speedKmh: 20f,
            locoTrackId: TrackId,
            locoSpanMeters: locoSpan);

        Assert.Equal(40f, funnel.StickyKmh);
        Assert.True(funnel.LastTakeAlongMeters < 0f);
        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
    }

    private static void LockTravel(PostedLimitFunnel funnel) =>
        funnel.SetTravel(0f, 0f, 1f, speedKmh: 20f, locoX: 0f, locoY: 0f, locoZ: 0f);

    private static ParsedPostedBoard SpanBoard(int id, float spanMeters, float kmh) =>
        new ParsedPostedBoard(
            id,
            0f,
            0f,
            spanMeters,
            0f,
            -1f,
            1f,
            0f,
            kmh,
            kmh,
            false,
            false,
            TrackId,
            spanMeters);
}
