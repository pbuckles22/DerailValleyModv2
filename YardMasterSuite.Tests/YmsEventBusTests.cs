using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Static bus tests must not run in parallel with each other.
/// </summary>
[Collection("YmsEventBus")]
public class YmsEventBusTests : IDisposable
{
    public YmsEventBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void ClearAllSubscriptions_is_safe_to_call_with_no_subscribers()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void Subscribe_then_raise_delivers_struct_payload()
    {
        YmsSignal received = default;
        var calls = 0;
        void Handler(YmsSignal signal)
        {
            received = signal;
            calls++;
        }

        YmsEventBus.OnSignal += Handler;
        YmsEventBus.RaiseSignal(new YmsSignal(7, 3.5f));

        Assert.Equal(1, calls);
        Assert.Equal(7, received.Id);
        Assert.Equal(3.5f, received.Value);
    }

    [Fact]
    public void Subscribe_then_raise_delivers_primitive_payload()
    {
        var received = 0;
        void Handler(int count) => received = count;

        YmsEventBus.OnCount += Handler;
        YmsEventBus.RaiseCount(42);

        Assert.Equal(42, received);
    }

    [Fact]
    public void Unsubscribe_then_raise_does_not_invoke_handler()
    {
        var calls = 0;
        void Handler(YmsSignal _) => calls++;

        YmsEventBus.OnSignal += Handler;
        YmsEventBus.OnSignal -= Handler;
        YmsEventBus.RaiseSignal(new YmsSignal(1, 1f));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ClearAllSubscriptions_then_raise_does_not_invoke_handler()
    {
        var signalCalls = 0;
        var countCalls = 0;
        void SignalHandler(YmsSignal _) => signalCalls++;
        void CountHandler(int _) => countCalls++;

        YmsEventBus.OnSignal += SignalHandler;
        YmsEventBus.OnCount += CountHandler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseSignal(new YmsSignal(1, 1f));
        YmsEventBus.RaiseCount(9);

        Assert.Equal(0, signalCalls);
        Assert.Equal(0, countCalls);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_backup_proximity_handler()
    {
        var calls = 0;
        void Handler(HudBarSnapshot _) => calls++;

        YmsEventBus.OnBackupProximityChanged += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseBackupProximityChanged(new HudBarSnapshot("Rear 1.0m"));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_limit_gov_cue_handler()
    {
        var calls = 0;
        void Handler(LimitGovCue _) => calls++;

        YmsEventBus.OnLimitGovCue += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseLimitGovCue(new LimitGovCue(true, true, true));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_maps_dest_handler()
    {
        var calls = 0;
        void Handler(MapsDestCommand _) => calls++;

        YmsEventBus.OnMapsDestCommand += Handler;
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseMapsDestCommand(new MapsDestCommand(MapsDestKind.Set));

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Subscribe_then_raise_delivers_maps_dest_kind()
    {
        MapsDestCommand received = default;
        void Handler(MapsDestCommand cmd) => received = cmd;

        YmsEventBus.OnMapsDestCommand += Handler;
        YmsEventBus.RaiseMapsDestCommand(new MapsDestCommand(MapsDestKind.Recheck));

        Assert.Equal(MapsDestKind.Recheck, received.Kind);
    }
}

[CollectionDefinition("YmsEventBus", DisableParallelization = true)]
public sealed class YmsEventBusCollection
{
}
