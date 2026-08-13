using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: one lever at a time — throttle, indy, train brake, engine brake (story 2.2).
/// </summary>
public class ControlTelemetryTests
{
    [Fact]
    public void First_sample_seeds_cache_and_is_silent()
    {
        var cache = default(ControlLeversCache);
        var msg = ControlTelemetry.Observe(
            throttle: 0.12f, indy: 0f, train: 0f, engine: 0f, enginePresent: false, reverser: 0.5f, ref cache);

        Assert.Null(msg);
        Assert.True(cache.Seeded);
        Assert.Equal(12, cache.ThrottlePct);
        Assert.Equal(0, cache.IndyPct);
        Assert.Equal(0, cache.TrainPct);
        Assert.False(cache.EnginePresent);
        Assert.Equal(50, cache.ReverserPct);
    }

    [Fact]
    public void Throttle_move_emits_named_thr_field()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0.12f, 0f, 0f, 0f, false, 0.5f, ref cache);
        var msg = ControlTelemetry.Observe(0.40f, 0f, 0f, 0f, false, 0.5f, ref cache);

        Assert.Equal("T2 controls: thr=40 indy=0 train=0 eng=na rev=50 raw=0.40,0.00,0.00,-,0.50", msg);
    }

    [Fact]
    public void Indy_move_does_not_change_train()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0f, 0f, 0f, 0f, false, 0.5f, ref cache);
        var msg = ControlTelemetry.Observe(0f, 0.25f, 0f, 0f, false, 0.5f, ref cache);

        Assert.Equal("T2 controls: thr=0 indy=25 train=0 eng=na rev=50 raw=0.00,0.25,0.00,-,0.50", msg);
        Assert.Equal(25, cache.IndyPct);
        Assert.Equal(0, cache.TrainPct);
    }

    [Fact]
    public void Train_brake_move_does_not_change_indy()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0f, 0f, 0f, 0f, false, 0.5f, ref cache);
        var msg = ControlTelemetry.Observe(0f, 0f, 0.25f, 0f, false, 0.5f, ref cache);

        Assert.Equal("T2 controls: thr=0 indy=0 train=25 eng=na rev=50 raw=0.00,0.00,0.25,-,0.50", msg);
        Assert.Equal(0, cache.IndyPct);
        Assert.Equal(25, cache.TrainPct);
    }

    [Fact]
    public void Engine_brake_move_when_present()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0f, 0f, 0f, 0f, true, 0.5f, ref cache);
        var msg = ControlTelemetry.Observe(0f, 0f, 0f, 0.40f, true, 0.5f, ref cache);

        Assert.Equal("T2 controls: thr=0 indy=0 train=0 eng=40 rev=50 raw=0.00,0.00,0.00,0.40,0.50", msg);
        Assert.Equal(40, cache.EnginePct);
    }

    [Fact]
    public void Reverser_move_emits_rev_field()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0f, 0f, 0f, 0f, false, 0.5f, ref cache);
        var msg = ControlTelemetry.Observe(0f, 0f, 0f, 0f, false, 1f, ref cache);

        Assert.Equal("T2 controls: thr=0 indy=0 train=0 eng=na rev=100 raw=0.00,0.00,0.00,-,1.00", msg);
    }

    [Fact]
    public void Same_percent_is_silent()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0.40f, 0.10f, 0.20f, 0f, false, 0.5f, ref cache);
        var msg = ControlTelemetry.Observe(0.401f, 0.104f, 0.204f, 0f, false, 0.502f, ref cache);

        Assert.Null(msg);
    }

    [Fact]
    public void Reset_allows_silent_reseed()
    {
        var cache = default(ControlLeversCache);
        ControlTelemetry.Observe(0.40f, 0f, 0f, 0f, false, 0.5f, ref cache);
        ControlTelemetry.Reset(ref cache);
        var msg = ControlTelemetry.Observe(0.40f, 0f, 0f, 0f, false, 0.5f, ref cache);

        Assert.Null(msg);
        Assert.True(cache.Seeded);
    }
}

[Collection("YmsEventBus")]
public class CabControlsBusTests : IDisposable
{
    public CabControlsBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void Subscribe_then_raise_delivers_named_brake_channels()
    {
        CabControlsState received = default;
        var calls = 0;
        void Handler(CabControlsState state)
        {
            received = state;
            calls++;
        }

        YmsEventBus.OnCabControlsChanged += Handler;
        YmsEventBus.RaiseCabControlsChanged(new CabControlsState(0.4f, 0.1f, 0.2f, 0f, false, 0.5f));

        Assert.Equal(1, calls);
        Assert.Equal(0.4f, received.Throttle);
        Assert.Equal(0.1f, received.IndyBrake);
        Assert.Equal(0.2f, received.TrainBrake);
        Assert.False(received.HasEngineBrake);
        Assert.Equal(0.5f, received.Reverser);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_cab_controls_handler()
    {
        var calls = 0;
        void Handler(CabControlsState _) => calls++;

        YmsEventBus.OnCabControlsChanged += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseCabControlsChanged(new CabControlsState(1f, 0f, 0f, 0f, false, 0.5f));

        Assert.Equal(0, calls);
    }
}
