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
}
