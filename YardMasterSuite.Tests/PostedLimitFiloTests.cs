using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>v1 PostedLimitFilo: ≤5 nearest boards per exit, both sides.</summary>
public class PostedLimitFiloTests
{
    [Fact]
    public void PartitionExits_caps_each_side_nearest_first()
    {
        var boards = new[]
        {
            Board(0, 0, 10, 40),
            Board(0, 0, 20, 50),
            Board(0, 0, 30, 60),
            Board(0, 0, 40, 70),
            Board(0, 0, 50, 80),
            Board(0, 0, 60, 90),
            Board(0, 0, -10, 30),
            Board(0, 0, -100, 20),
        };

        PostedLimitFilo.PartitionExits(
            boards,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            out var plus,
            out var minus);

        Assert.Equal(PostedLimitFilo.MaxDepth, plus.Length);
        Assert.Equal(40f, plus[0].ThroughKmh);
        Assert.Equal(80f, plus[4].ThroughKmh);
        Assert.Equal(2, minus.Length);
        Assert.Equal(30f, minus[0].ThroughKmh);
        Assert.Equal(20f, minus[1].ThroughKmh);
    }

    [Fact]
    public void SelectActiveExit_follows_travel_polarity()
    {
        var plus = new[] { Board(1, 0, 10, 60) };
        var minus = new[] { Board(2, 0, -10, 40) };

        var same = PostedLimitFilo.SelectActiveExit(plus, minus, 0f, 1f, 0f, 1f);
        Assert.Same(plus, same);

        var opp = PostedLimitFilo.SelectActiveExit(plus, minus, 0f, 1f, 0f, -1f);
        Assert.Same(minus, opp);
    }

    [Fact]
    public void ShouldLockDirection_above_crawl()
    {
        Assert.False(PostedLimitFilo.ShouldLockDirection(0f));
        Assert.True(PostedLimitFilo.ShouldLockDirection(PostedLimitFilo.DirectionLockMinSpeedKmh + 0.1f));
    }

    [Fact]
    public void Smoke_sw_turntable_left_40_is_first_plus_exit()
    {
        var boards = new[]
        {
            Board(1, -2f, 12f, 40f),
            Board(2, 2f, 135f, 40f),
            Board(3, 2f, 400f, 80f),
        };

        PostedLimitFilo.PartitionExits(
            boards,
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            out var plus,
            out var minus);

        Assert.Empty(minus);
        Assert.Equal(3, plus.Length);
        Assert.Equal(40f, plus[0].ThroughKmh);
        Assert.Equal(12f, plus[0].Z);
    }

    [Fact]
    public void Smoke_sw_shack_40_just_behind_is_scanned_until_direction_lock()
    {
        var shack = Board(1, -2f, -2f, 40f);
        var far = Board(2, 2f, 135f, 40f);
        PostedLimitFilo.PartitionExits(
            new[] { shack, far },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            out var plus,
            out var minus);

        Assert.Equal(far.InstanceId, plus[0].InstanceId);
        Assert.Equal(shack.InstanceId, minus[0].InstanceId);

        var sit = PostedLimitFilo.SelectScanSet(
            plus,
            minus,
            directionLocked: false,
            0f,
            1f,
            0f,
            1f);
        Assert.Equal(2, sit.Length);
        Assert.Equal(far.InstanceId, sit[0].InstanceId);
        Assert.Equal(shack.InstanceId, sit[1].InstanceId);

        var rolling = PostedLimitFilo.SelectScanSet(
            plus,
            minus,
            directionLocked: true,
            0f,
            1f,
            0f,
            1f);
        Assert.Same(plus, rolling);
    }

    [Fact]
    public void PartitionExits_abeam_board_joins_plus()
    {
        PostedLimitFilo.PartitionExits(
            new[] { Board(1, -2f, 0f, 40f) },
            0f,
            0f,
            0f,
            0f,
            0f,
            1f,
            out var plus,
            out var minus);
        Assert.Single(plus);
        Assert.Empty(minus);
        Assert.Equal(40f, plus[0].ThroughKmh);
    }

    private static ParsedPostedBoard Board(int id, float x, float z, float kmh) =>
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
            false);
}
