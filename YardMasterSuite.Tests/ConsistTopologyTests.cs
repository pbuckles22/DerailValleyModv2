using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: couple/uncouple changes consist cars and tonnes (story 2.3).
/// </summary>
public class ConsistTopologyTests
{
    [Fact]
    public void Board_emits_T2_consist_baseline()
    {
        var cache = default(ConsistCache);
        var msg = ConsistTopology.Observe(carCount: 1, massKg: 38000f, ref cache);

        Assert.Equal("T2 consist: cars=1 t=38", msg);
        Assert.True(cache.Seeded);
        Assert.Equal(1, cache.CarCount);
        Assert.Equal(38, cache.MassTonnes);
    }

    [Fact]
    public void Couple_emits_higher_car_count()
    {
        var cache = default(ConsistCache);
        ConsistTopology.Observe(1, 38000f, ref cache);
        var msg = ConsistTopology.Observe(2, 56000f, ref cache);

        Assert.Equal("T2 consist: cars=2 t=56", msg);
        Assert.Equal(2, cache.CarCount);
        Assert.Equal(56, cache.MassTonnes);
    }

    [Fact]
    public void Uncouple_emits_lower_car_count()
    {
        var cache = default(ConsistCache);
        ConsistTopology.Observe(2, 56000f, ref cache);
        var msg = ConsistTopology.Observe(1, 38000f, ref cache);

        Assert.Equal("T2 consist: cars=1 t=38", msg);
    }

    [Fact]
    public void Same_consist_is_silent()
    {
        var cache = default(ConsistCache);
        ConsistTopology.Observe(2, 56100f, ref cache);
        var msg = ConsistTopology.Observe(2, 55900f, ref cache);

        Assert.Null(msg);
    }

    [Fact]
    public void Reset_emits_baseline_again_on_reboard()
    {
        var cache = default(ConsistCache);
        ConsistTopology.Observe(2, 56000f, ref cache);
        ConsistTopology.Reset(ref cache);
        var msg = ConsistTopology.Observe(2, 56000f, ref cache);

        Assert.Equal("T2 consist: cars=2 t=56", msg);
    }

    [Fact]
    public void Yard_uncouple_on_foot_emits_cars_down_without_reset()
    {
        var cache = default(ConsistCache);
        var bound = 0;
        Assert.Equal(
            ConsistBindAction.BindNewLoco,
            ConsistTopology.PrepareForLoco(-3087098, ref cache, ref bound));
        ConsistTopology.Observe(2, 56000f, ref cache);

        Assert.Equal(
            ConsistBindAction.KeepListening,
            ConsistTopology.PrepareForLoco(0, ref cache, ref bound));
        Assert.Equal(-3087098, bound);

        var msg = ConsistTopology.Observe(1, 38000f, ref cache);
        Assert.Equal("T2 consist: cars=1 t=38", msg);
    }

    [Fact]
    public void Reboard_same_loco_does_not_reset_consist()
    {
        var cache = default(ConsistCache);
        var bound = 0;
        ConsistTopology.PrepareForLoco(42, ref cache, ref bound);
        ConsistTopology.Observe(4, 92000f, ref cache);

        Assert.Equal(
            ConsistBindAction.KeepListening,
            ConsistTopology.PrepareForLoco(42, ref cache, ref bound));
        Assert.Null(ConsistTopology.Observe(4, 92000f, ref cache));
    }

    [Fact]
    public void Board_different_loco_resets_consist()
    {
        var cache = default(ConsistCache);
        var bound = 0;
        ConsistTopology.PrepareForLoco(1, ref cache, ref bound);
        ConsistTopology.Observe(6, 128000f, ref cache);

        Assert.Equal(
            ConsistBindAction.BindNewLoco,
            ConsistTopology.PrepareForLoco(2, ref cache, ref bound));
        Assert.False(cache.Seeded);
        Assert.Equal(2, bound);
        Assert.Equal("T2 consist: cars=1 t=38", ConsistTopology.Observe(1, 38000f, ref cache));
    }

    [Fact]
    public void Smoke_shunter_yard_on_foot_look_at_binds_consist_anchor()
    {
        Assert.Equal(100, ConsistTopology.ResolveConsistAnchor(boardedLocoId: 0, lookAtUsableLocoId: 100));

        var cache = default(ConsistCache);
        var bound = 0;
        Assert.Equal(
            ConsistBindAction.BindNewLoco,
            ConsistTopology.PrepareForLoco(100, ref cache, ref bound));
        Assert.Equal(
            "T2 consist: cars=3 t=74",
            ConsistTopology.Observe(3, 74000f, ref cache));
        Assert.Equal(3, cache.CarCount);
        Assert.Equal(74, cache.MassTonnes);
    }

    [Fact]
    public void Smoke_cab_look_at_other_train_keeps_boarded_consist_anchor()
    {
        Assert.Equal(42, ConsistTopology.ResolveConsistAnchor(boardedLocoId: 42, lookAtUsableLocoId: 99));
    }

    [Fact]
    public void Smoke_look_at_usable_train_rehydrates_after_unboard_without_t2_repeat()
    {
        var cache = default(ConsistCache);
        var bound = 0;
        ConsistTopology.PrepareForLoco(100, ref cache, ref bound);
        ConsistTopology.Observe(3, 74000f, ref cache);

        Assert.Equal(
            ConsistBindAction.KeepListening,
            ConsistTopology.PrepareForLoco(0, ref cache, ref bound));
        Assert.Equal(100, bound);
        Assert.Null(ConsistTopology.Observe(3, 74000f, ref cache));
        Assert.Equal(3, cache.CarCount);
        Assert.Equal(74, cache.MassTonnes);

        Assert.Equal(
            ConsistBindAction.KeepListening,
            ConsistTopology.PrepareForLoco(100, ref cache, ref bound));
        Assert.True(ConsistTopology.ShouldForceHudPublish(ConsistBindAction.KeepListening, consistAnchorId: 100));
    }

    [Fact]
    public void Look_at_different_consist_resets_and_emits()
    {
        var cache = default(ConsistCache);
        var bound = 0;
        ConsistTopology.PrepareForLoco(100, ref cache, ref bound);
        ConsistTopology.Observe(3, 74000f, ref cache);

        Assert.Equal(
            ConsistBindAction.BindNewLoco,
            ConsistTopology.PrepareForLoco(200, ref cache, ref bound));
        Assert.False(cache.Seeded);
        Assert.Equal(200, bound);
        Assert.Equal("T2 consist: cars=2 t=50", ConsistTopology.Observe(2, 50000f, ref cache));
    }
}

[Collection("YmsEventBus")]
public class ConsistSnapshotBusTests : IDisposable
{
    public ConsistSnapshotBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void Subscribe_then_raise_delivers_cars_and_tonnes()
    {
        ConsistSnapshot received = default;
        var calls = 0;
        void Handler(ConsistSnapshot snap)
        {
            received = snap;
            calls++;
        }

        YmsEventBus.OnConsistChanged += Handler;
        YmsEventBus.RaiseConsistChanged(new ConsistSnapshot(3, 128));

        Assert.Equal(1, calls);
        Assert.Equal(3, received.CarCount);
        Assert.Equal(128, received.MassTonnes);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_consist_handler()
    {
        var calls = 0;
        void Handler(ConsistSnapshot _) => calls++;

        YmsEventBus.OnConsistChanged += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseConsistChanged(new ConsistSnapshot(1, 40));

        Assert.Equal(0, calls);
    }
}
