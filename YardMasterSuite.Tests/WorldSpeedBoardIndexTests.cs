using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class WorldSpeedBoardIndexTests
{
    [Fact]
    public void Remember_survives_and_returns_by_track()
    {
        var index = new WorldSpeedBoardIndex();
        index.Remember(42, 50f, 100f, 2f, 200f, travelX: 1f, travelZ: 0f);
        index.Remember(42, 80f, 150f, 2f, 250f, travelX: 1f, travelZ: 0f);
        index.Remember(7, 40f, 0f, 0f, 0f, travelX: 0f, travelZ: 1f);

        Assert.Equal(2, index.CountForTrack(42));
        Assert.Equal(1, index.CountForTrack(7));
    }

    [Fact]
    public void SameTravel_rejects_opposite_direction()
    {
        var index = new WorldSpeedBoardIndex();
        index.Remember(1, 50f, 10f, 0f, 10f, travelX: 1f, travelZ: 0f);
        Assert.True(index.TryGetFirst(1, out var pin));
        Assert.True(WorldSpeedBoardIndex.SameTravel(pin, 1f, 0f));
        Assert.False(WorldSpeedBoardIndex.SameTravel(pin, -1f, 0f));
    }

    [Fact]
    public void Smoke_seed_behind_picks_nearest_same_travel_board()
    {
        var index = new WorldSpeedBoardIndex();
        index.Remember(9, 40f, 0f, 0f, -80f, travelX: 0f, travelZ: 1f);
        index.Remember(9, 60f, 0f, 0f, -10f, travelX: 0f, travelZ: 1f);
        index.Remember(9, 80f, 0f, 0f, 20f, travelX: 0f, travelZ: 1f);
        index.Remember(9, 30f, 0f, 0f, -15f, travelX: 0f, travelZ: -1f);

        var seed = index.SeedBehind(
            trackId: 9,
            originX: 0f,
            originY: 0f,
            originZ: 0f,
            travelX: 0f,
            travelZ: 1f,
            lookbackMeters: 600f);

        Assert.Equal(60f, seed);
    }

    [Fact]
    public void Remember_and_seed_fail_closed_on_bad_inputs()
    {
        var index = new WorldSpeedBoardIndex();
        Assert.Equal(0, index.Count);
        index.Remember(0, 50f, 1f, 1f, 1f, 1f, 0f);
        index.Remember(1, 0f, 1f, 1f, 1f, 1f, 0f);
        index.Remember(1, -10f, 1f, 1f, 1f, 1f, 0f);
        index.Remember(1, float.NaN, 1f, 1f, 1f, 1f, 0f);
        index.Remember(1, 50f, float.PositiveInfinity, 1f, 1f, 1f, 0f);
        index.Remember(1, 50f, 1f, 1f, 1f, 0f, 0f);
        Assert.Equal(0, index.Count);
        Assert.Equal(0, index.CountForTrack(99));
        Assert.False(index.TryGetFirst(99, out _));

        index.Remember(5, 40f, 10f, 0f, 10f, 1f, 0f);
        Assert.Equal(1, index.Count);
        index.Remember(5, 40f, 10f, 0f, 10f, 1f, 0f); // same key overwrite
        Assert.Equal(1, index.Count);
        Assert.True(index.TryGetFirst(5, out var pin));
        Assert.Equal(40f, pin.Kmh);

        Assert.Null(index.SeedBehind(0, 0f, 0f, 0f, 1f, 0f, 100f));
        Assert.Null(index.SeedBehind(5, 0f, 0f, 0f, 1f, 0f, 0f));
        Assert.Null(index.SeedBehind(5, 0f, 0f, 0f, 0f, 0f, 100f));
        Assert.Null(index.SeedBehind(5, 100f, 0f, 100f, 1f, 0f, 5f)); // ahead / out of lookback
        Assert.False(WorldSpeedBoardIndex.SameTravel(pin, 0f, 0f));
        index.Clear();
        Assert.Equal(0, index.Count);
        Assert.Equal(0, index.CountForTrack(5));
    }
}
