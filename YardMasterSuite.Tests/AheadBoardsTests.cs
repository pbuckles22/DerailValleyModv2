using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: cab Next is the nearest posted number that differs from Limit (**6.10**).
/// </summary>
public class AheadBoardsTests
{
    [Fact]
    public void Smoke_cab_next_picks_nearest_different_posted_number()
    {
        var boards = new[]
        {
            new AheadBoard(80f, 40f),
            new AheadBoard(50f, 120f),
            new AheadBoard(50f, 200f),
        };

        var next = AheadBoards.NextDifferent(80f, boards);
        Assert.NotNull(next);
        Assert.Equal(50f, next!.Value.Kmh);
        Assert.Equal(120f, next.Value.AlongMeters);
    }

    [Fact]
    public void Smoke_next_skips_same_posted_number()
    {
        var boards = new[]
        {
            new AheadBoard(80f, 30f),
            new AheadBoard(80f, 90f),
        };

        Assert.Null(AheadBoards.NextDifferent(80f, boards));
    }

    [Fact]
    public void NextDifferent_ignores_behind_or_zero_along()
    {
        var boards = new[]
        {
            new AheadBoard(40f, 0f),
            new AheadBoard(40f, -12f),
            new AheadBoard(50f, 80f),
        };

        var next = AheadBoards.NextDifferent(80f, boards);
        Assert.NotNull(next);
        Assert.Equal(50f, next!.Value.Kmh);
    }

    [Fact]
    public void Smoke_sw_turntable_ahead_four_is_nearest_not_filo()
    {
        var dest = new AheadBoard[AheadBoards.DiagnosticCap];
        var n = AheadBoards.CopyNearest(
            new[]
            {
                new AheadBoard(60f, 800f),
                new AheadBoard(40f, 135f),
                new AheadBoard(40f, 12f),
                new AheadBoard(80f, 400f),
                new AheadBoard(50f, 1200f),
            },
            dest);
        Assert.Equal(4, n);
        Assert.Equal(40f, dest[0].Kmh);
        Assert.Equal(12f, dest[0].AlongMeters);
        Assert.Equal(135f, dest[1].AlongMeters);
        Assert.Equal(80f, dest[2].Kmh);
        Assert.Equal(60f, dest[3].Kmh);
    }
}

public class NextLimitRevealTests
{
    [Fact]
    public void Reveal_for_60_to_40_is_hundreds_of_meters_not_kilometres()
    {
        var d = NextLimitReveal.RevealMeters(60f, 40f, massTonnes: 38f);
        Assert.InRange(d, 200f, 600f);
    }

    [Fact]
    public void Reveal_for_80_to_40_is_longer_than_60_to_40()
    {
        var mild = NextLimitReveal.RevealMeters(60f, 40f, 38f);
        var steep = NextLimitReveal.RevealMeters(80f, 40f, 38f);
        Assert.True(steep > mild);
        Assert.True(steep <= NextLimitReveal.MaxRevealMeters);
    }

    [Fact]
    public void ShowDistance_false_when_far()
    {
        Assert.False(NextLimitReveal.ShowDistance(800f, 70f, 50f, 38f));
        Assert.True(NextLimitReveal.ShowDistance(100f, 70f, 50f, 38f));
    }

    [Fact]
    public void Smoke_next_meters_hold_through_reveal_edge()
    {
        Assert.True(NextLimitReveal.ShowDistance(599f, 120f, 40f, 38f));
        Assert.False(NextLimitReveal.ShowDistance(601f, 120f, 40f, 38f));
        Assert.True(
            NextLimitReveal.ShowDistance(601f, 120f, 40f, 38f, wasShowing: true));
        Assert.False(
            NextLimitReveal.ShowDistance(650f, 120f, 40f, 38f, wasShowing: true));
    }
}
