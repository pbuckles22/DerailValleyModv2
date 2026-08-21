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
}
