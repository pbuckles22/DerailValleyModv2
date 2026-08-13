using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: board/unboard a loco (story 2.1). Named after the in-world
/// scenario, not the helper.
/// </summary>
public class LocoStateTests
{
    [Fact]
    public void Board_loco_caches_instance_and_emits_T2_loco_board()
    {
        var cache = default(LocoStateCache);
        var msg = LocoState.Observe(instanceId: 42, ref cache);

        Assert.Equal("T2 loco-board: id=42", msg);
        Assert.Equal(42, cache.CurrentInstanceId);
    }

    [Fact]
    public void Unboard_clears_cache_and_emits_T2_loco_unboard()
    {
        var cache = new LocoStateCache { CurrentInstanceId = 42 };
        var msg = LocoState.Observe(instanceId: 0, ref cache);

        Assert.Equal("T2 loco-unboard: id=42", msg);
        Assert.Equal(0, cache.CurrentInstanceId);
    }

    [Fact]
    public void Same_loco_is_silent()
    {
        var cache = new LocoStateCache { CurrentInstanceId = 42 };
        var msg = LocoState.Observe(instanceId: 42, ref cache);

        Assert.Null(msg);
        Assert.Equal(42, cache.CurrentInstanceId);
    }

    [Fact]
    public void Already_unboarded_is_silent()
    {
        var cache = default(LocoStateCache);
        var msg = LocoState.Observe(instanceId: 0, ref cache);

        Assert.Null(msg);
        Assert.Equal(0, cache.CurrentInstanceId);
    }

    [Fact]
    public void Switch_loco_emits_board_for_new_id()
    {
        var cache = new LocoStateCache { CurrentInstanceId = 10 };
        var msg = LocoState.Observe(instanceId: 20, ref cache);

        Assert.Equal("T2 loco-board: id=20", msg);
        Assert.Equal(20, cache.CurrentInstanceId);
    }
}

[Collection("YmsEventBus")]
public class LocoPresenceBusTests : IDisposable
{
    public LocoPresenceBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void Subscribe_then_raise_delivers_boarded_presence()
    {
        LocoPresence received = default;
        var calls = 0;
        void Handler(LocoPresence presence)
        {
            received = presence;
            calls++;
        }

        YmsEventBus.OnPlayerBoardedTrain += Handler;
        YmsEventBus.RaisePlayerBoardedTrain(new LocoPresence(42));

        Assert.Equal(1, calls);
        Assert.Equal(42, received.InstanceId);
        Assert.True(received.IsBoarded);
    }

    [Fact]
    public void Raise_unboarded_presence_has_instance_zero()
    {
        LocoPresence received = new LocoPresence(99);
        void Handler(LocoPresence presence) => received = presence;

        YmsEventBus.OnPlayerBoardedTrain += Handler;
        YmsEventBus.RaisePlayerBoardedTrain(LocoPresence.None);

        Assert.Equal(0, received.InstanceId);
        Assert.False(received.IsBoarded);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_loco_presence_handler()
    {
        var calls = 0;
        void Handler(LocoPresence _) => calls++;

        YmsEventBus.OnPlayerBoardedTrain += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaisePlayerBoardedTrain(new LocoPresence(1));

        Assert.Equal(0, calls);
    }
}
