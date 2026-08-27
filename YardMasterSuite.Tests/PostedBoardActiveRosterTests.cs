using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PostedBoardActiveRosterTests
{
    [Fact]
    public void WithinActiveRadius_keeps_near_drops_far()
    {
        Assert.True(PostedBoardActiveRoster.WithinActiveRadius(0f, 0f, 0f, 0f, 0f, 0f));
        Assert.True(
            PostedBoardActiveRoster.WithinActiveRadius(
                PostedBoardActiveRoster.ActiveRadiusMeters,
                0f,
                0f,
                0f,
                0f,
                0f));
        Assert.False(
            PostedBoardActiveRoster.WithinActiveRadius(
                PostedBoardActiveRoster.ActiveRadiusMeters + 1f,
                0f,
                0f,
                0f,
                0f,
                0f));
    }

    [Fact]
    public void NeedsRefresh_first_move_or_empty_retry_never_periodic_when_warm()
    {
        Assert.True(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f,
                lastRefreshAt: -999f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: false));

        Assert.False(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f + PostedBoardActiveRoster.RefreshSeconds,
                lastRefreshAt: 10f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true,
                rosterEmpty: false));

        Assert.True(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 11f,
                lastRefreshAt: 10f,
                originX: PostedBoardActiveRoster.MoveInvalidateMeters + 1f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true));

        Assert.True(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f + PostedBoardActiveRoster.EmptyRetrySeconds,
                lastRefreshAt: 10f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true,
                rosterEmpty: true,
                emptyRetriesDone: 0));

        Assert.False(
            PostedBoardActiveRoster.NeedsRefresh(
                now: 10f + PostedBoardActiveRoster.EmptyRetrySeconds,
                lastRefreshAt: 10f,
                originX: 0f,
                originZ: 0f,
                lastOriginX: 0f,
                lastOriginZ: 0f,
                hasLastOrigin: true,
                rosterEmpty: true,
                emptyRetriesDone: PostedBoardActiveRoster.MaxEmptyRetries));
    }

    [Fact]
    public void PickKmh_single_and_dual()
    {
        var single = Board(1, 0f, 0f, 60f, 60f, isDual: false);
        Assert.Equal(60f, PostedBoardActiveRoster.PickKmh(single, diverging: true));
        Assert.Equal(60f, PostedBoardActiveRoster.PickKmh(single, diverging: false));

        var dual = Board(2, 0f, 0f, 60f, 40f, isDual: true);
        Assert.Equal(60f, PostedBoardActiveRoster.PickKmh(dual, diverging: false));
        Assert.Equal(40f, PostedBoardActiveRoster.PickKmh(dual, diverging: true));
    }

    [Fact]
    public void SelectGoverningBehind_picks_closest_behind_along_forward()
    {
        var boards = new[]
        {
            Board(1, 0f, -50f, 40f),
            Board(2, 0f, -10f, 60f),
            Board(3, 0f, 20f, 80f),
        };

        var kmh = PostedBoardActiveRoster.SelectGoverningBehindKmh(
            boards,
            locoX: 0f,
            locoY: 0f,
            locoZ: 0f,
            forwardX: 0f,
            forwardY: 0f,
            forwardZ: 1f,
            lookbackMeters: 300f);

        Assert.Equal(60f, kmh);
    }

    [Fact]
    public void SelectGoverningBehind_ignores_ahead_and_beyond_lookback()
    {
        var boards = new[]
        {
            Board(1, 0f, 50f, 80f),
            Board(2, 0f, -400f, 40f),
        };

        var kmh = PostedBoardActiveRoster.SelectGoverningBehindKmh(
            boards,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            lookbackMeters: 300f);

        Assert.Null(kmh);
    }

    [Fact]
    public void Lookahead_is_at_least_sixteen_hundred_meters()
    {
        Assert.Equal(PostedBoardActiveRoster.LookaheadMinMeters, PostedBoardActiveRoster.LookaheadMeters(0f));
        Assert.Equal(PostedBoardActiveRoster.LookaheadMinMeters, PostedBoardActiveRoster.LookaheadMeters(40f));
        Assert.True(PostedBoardActiveRoster.LookaheadMeters(160f) > PostedBoardActiveRoster.LookaheadMinMeters);
    }

    [Fact]
    public void Smoke_nearby_posted_6_is_kept_when_board_track_unknown()
    {
        Assert.False(PostedBoardRoute.IsOffRoute(hasPath: true, boardTrackKnown: false, onPath: false));
    }

    [Fact]
    public void Smoke_facing_60_on_straight_not_dropped_for_weak_siding_attach()
    {
        Assert.False(PostedBoardRoute.TrackIdentityTrusted(onPath: false, attachMeters: 8f));
        Assert.False(
            PostedBoardRoute.IsOffRoute(
                hasPath: true,
                boardTrackKnown: PostedBoardRoute.TrackIdentityTrusted(false, 8f),
                onPath: false));
    }

    [Fact]
    public void Smoke_branch_board_is_ignored_when_on_other_path_track()
    {
        Assert.True(PostedBoardRoute.TrackIdentityTrusted(onPath: false, attachMeters: 1.5f));
        Assert.True(PostedBoardRoute.IsOffRoute(hasPath: true, boardTrackKnown: true, onPath: false));
        Assert.False(PostedBoardRoute.IsOffRoute(hasPath: true, boardTrackKnown: true, onPath: true));
        Assert.False(PostedBoardRoute.IsOffRoute(hasPath: false, boardTrackKnown: true, onPath: false));
        Assert.True(PostedBoardRoute.TrackIdentityTrusted(onPath: true, attachMeters: 11f));
    }

    private static ParsedPostedBoard Board(
        int id,
        float x,
        float z,
        float throughKmh,
        float? divergeKmh = null,
        bool isDual = false) =>
        new ParsedPostedBoard(
            id,
            x,
            0f,
            z,
            0f,
            -1f,
            1f,
            0f,
            throughKmh,
            divergeKmh ?? throughKmh,
            isDual,
            isDual);
}
