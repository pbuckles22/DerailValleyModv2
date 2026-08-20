using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PostedStickyLimitTests
{
    [Fact]
    public void Taken_board_sets_sticky()
    {
        Assert.Equal(40f, PostedStickyLimit.Resolve(sticky: 60f, takenKmh: 40f, seedKmh: 60f));
    }

    [Fact]
    public void Smoke_looser_board_behind_cannot_raise_sticky()
    {
        Assert.Equal(40f, PostedStickyLimit.Resolve(sticky: 40f, takenKmh: null, seedKmh: 60f));
    }

    [Fact]
    public void Nearest_behind_only_seeds_when_sticky_is_unknown()
    {
        Assert.Equal(60f, PostedStickyLimit.Resolve(sticky: null, takenKmh: null, seedKmh: 60f));
        Assert.Null(PostedStickyLimit.Resolve(sticky: null, takenKmh: null, seedKmh: null));
    }

    [Fact]
    public void Smoke_rolling_does_not_seed_behind_without_a_take()
    {
        Assert.Null(
            PostedStickyLimit.Resolve(
                sticky: null,
                takenKmh: null,
                seedKmh: 60f,
                speedKmh: 10f));
        Assert.Equal(
            60f,
            PostedStickyLimit.Resolve(
                sticky: null,
                takenKmh: null,
                seedKmh: 60f,
                speedKmh: 0f));
        Assert.Equal(
            40f,
            PostedStickyLimit.Resolve(
                sticky: 40f,
                takenKmh: null,
                seedKmh: 60f,
                speedKmh: 10f));
    }

    [Fact]
    public void Take_wins_even_when_it_is_looser_than_sticky()
    {
        Assert.Equal(80f, PostedStickyLimit.Resolve(sticky: 40f, takenKmh: 80f, seedKmh: 40f));
    }

    [Fact]
    public void ShouldClearForReverse_ignores_standstill_jitter()
    {
        Assert.False(
            PostedStickyLimit.ShouldClearForReverse(
                speedKmh: 0.2f,
                stickyTravelX: 0f,
                stickyTravelZ: 1f,
                travelX: 0f,
                travelZ: -1f));
    }

    [Fact]
    public void ShouldClearForReverse_when_moving_opposite()
    {
        Assert.True(
            PostedStickyLimit.ShouldClearForReverse(
                speedKmh: 12f,
                stickyTravelX: 0f,
                stickyTravelZ: 1f,
                travelX: 0f,
                travelZ: -1f));
    }
}

public class BoardTakeDetectorTests
{
    [Fact]
    public void Smoke_passing_board_is_a_take()
    {
        var detector = new BoardTakeDetector();
        Assert.Null(detector.Observe(1, 40f, alongMeters: 12f));
        Assert.Equal(40f, detector.Observe(1, 40f, alongMeters: -0.5f));
    }

    [Fact]
    public void Staying_behind_is_not_a_repeat_take()
    {
        var detector = new BoardTakeDetector();
        detector.Observe(1, 40f, alongMeters: 8f);
        Assert.Equal(40f, detector.Observe(1, 40f, alongMeters: -1f));
        Assert.Null(detector.Observe(1, 40f, alongMeters: -20f));
    }

    [Fact]
    public void Board_first_seen_behind_is_not_a_take()
    {
        var detector = new BoardTakeDetector();
        Assert.Null(detector.Observe(7, 60f, alongMeters: -40f));
    }

    [Fact]
    public void Reset_forgets_sides()
    {
        var detector = new BoardTakeDetector();
        detector.Observe(1, 40f, alongMeters: 10f);
        detector.Reset();
        Assert.Null(detector.Observe(1, 40f, alongMeters: -1f));
    }
}
