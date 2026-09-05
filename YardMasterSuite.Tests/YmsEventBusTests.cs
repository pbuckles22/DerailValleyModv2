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

    [Fact]
    public void Subscribe_then_raise_delivers_remaining_type_a_events()
    {
        LocoPresence loco = default;
        CabControlsState cab = default;
        ConsistSnapshot consist = default;
        CompassHeading heading = default;
        SpeedSnapshot speed = default;
        SpeedLimitSnapshot limit = default;
        PostedLimitSnapshot posted = default;
        UsableTrainState usable = default;
        HudBarSnapshot look = default;
        HudBarSnapshot job = default;
        HudBarSnapshot extras = default;
        TrainGadgetSnapshot gadgets = default;
        HudBarSnapshot backup = default;
        LimitGovCue cue = default;
        MailboxItem mail = default;
        PathGraphReady graph = default;
        RoutePlanReady route = default;

        YmsEventBus.OnPlayerBoardedTrain += s => loco = s;
        YmsEventBus.OnCabControlsChanged += s => cab = s;
        YmsEventBus.OnConsistChanged += s => consist = s;
        YmsEventBus.OnHeadingChanged += s => heading = s;
        YmsEventBus.OnSpeedChanged += s => speed = s;
        YmsEventBus.OnSpeedLimitChanged += s => limit = s;
        YmsEventBus.OnPostedLimitChanged += s => posted = s;
        YmsEventBus.OnUsableTrainChanged += s => usable = s;
        YmsEventBus.OnLookAtBarChanged += s => look = s;
        YmsEventBus.OnJobBarChanged += s => job = s;
        YmsEventBus.OnAlwaysOnExtrasChanged += s => extras = s;
        YmsEventBus.OnTrainGadgetsChanged += s => gadgets = s;
        YmsEventBus.OnBackupProximityChanged += s => backup = s;
        YmsEventBus.OnLimitGovCue += s => cue = s;
        YmsEventBus.OnMailboxItem += s => mail = s;
        YmsEventBus.OnPathGraphReady += s => graph = s;
        YmsEventBus.OnRoutePlanReady += s => route = s;

        YmsEventBus.RaisePlayerBoardedTrain(new LocoPresence(9));
        YmsEventBus.RaiseCabControlsChanged(new CabControlsState(0.1f, 0.2f, 0.3f, 0.4f, true, 0.5f));
        YmsEventBus.RaiseConsistChanged(new ConsistSnapshot(4, 120));
        YmsEventBus.RaiseHeadingChanged(new CompassHeading(3));
        YmsEventBus.RaiseSpeedChanged(new SpeedSnapshot(42));
        YmsEventBus.RaiseSpeedLimitChanged(new SpeedLimitSnapshot(60f, LimitAuthority.Posted));
        YmsEventBus.RaisePostedLimitChanged(new PostedLimitSnapshot(50f, 3));
        YmsEventBus.RaiseUsableTrainChanged(new UsableTrainState(true, 7));
        YmsEventBus.RaiseLookAtBarChanged(new HudBarSnapshot("look"));
        YmsEventBus.RaiseJobBarChanged(new HudBarSnapshot("job"));
        YmsEventBus.RaiseAlwaysOnExtrasChanged(new HudBarSnapshot("extras"));
        YmsEventBus.RaiseTrainGadgetsChanged(new TrainGadgetSnapshot(fuelPercent: 80f));
        YmsEventBus.RaiseBackupProximityChanged(new HudBarSnapshot("Rear 2.0m"));
        YmsEventBus.RaiseLimitGovCue(new LimitGovCue(true, false, true));
        YmsEventBus.RaiseMailboxItem(new MailboxItem(11));
        YmsEventBus.RaisePathGraphReady(new PathGraphReady(1, 2, 3, true, 4, 5f));
        YmsEventBus.RaiseRoutePlanReady(new RoutePlanReady(2, null, "A", null, null, null, null));

        Assert.Equal(9, loco.InstanceId);
        Assert.Equal(0.1f, cab.Throttle);
        Assert.Equal(4, consist.CarCount);
        Assert.Equal(3, heading.PointIndex);
        Assert.Equal(42, speed.Kmh);
        Assert.Equal(60f, limit.LimitKmh);
        Assert.Equal(50f, posted.Kmh);
        Assert.True(usable.HasUsableLocoTrain);
        Assert.Equal("look", look.Text);
        Assert.Equal("job", job.Text);
        Assert.Equal("extras", extras.Text);
        Assert.Equal(80f, gadgets.FuelPercent);
        Assert.Equal("Rear 2.0m", backup.Text);
        Assert.True(cue.Throttle);
        Assert.False(cue.Independent);
        Assert.Equal(11, mail.Sequence);
        Assert.Equal(1, graph.Generation);
        Assert.Equal(2, route.Generation);
        Assert.Equal("A", route.OriginTrackId);
    }

    [Fact]
    public void Drain_route_plan_raises_type_a()
    {
        RoutePlanReady received = default;
        YmsEventBus.OnRoutePlanReady += item => received = item;
        YmsEventBus.RoutePlan.Enqueue(new RoutePlanReady(5, null, "O", "exit", 1f, null, "log"));
        Assert.Equal(1, YmsEventBus.DrainRoutePlan(8));
        Assert.Equal(5, received.Generation);
        Assert.Equal("O", received.OriginTrackId);
    }

    [Fact]
    public void Raise_without_subscribers_is_safe_for_all_channels()
    {
        YmsEventBus.ClearAllSubscriptions();
        YmsEventBus.RaiseSignal(new YmsSignal(1, 1f));
        YmsEventBus.RaiseCount(1);
        YmsEventBus.RaisePlayerBoardedTrain(new LocoPresence(1));
        YmsEventBus.RaiseCabControlsChanged(new CabControlsState(0, 0, 0, 0, false, 0));
        YmsEventBus.RaiseConsistChanged(new ConsistSnapshot(1, 1));
        YmsEventBus.RaiseHeadingChanged(new CompassHeading(0));
        YmsEventBus.RaiseMailboxItem(new MailboxItem(1));
        YmsEventBus.RaisePathGraphReady(new PathGraphReady(1, 0, 0, false, 0, 0f));
        YmsEventBus.RaiseRoutePlanReady(new RoutePlanReady(1, null, null, null, null, null, null));
        YmsEventBus.RaiseSpeedChanged(new SpeedSnapshot(0));
        YmsEventBus.RaiseSpeedLimitChanged(SpeedLimitSnapshot.None);
        YmsEventBus.RaisePostedLimitChanged(PostedLimitSnapshot.None);
        YmsEventBus.RaiseUsableTrainChanged(new UsableTrainState(false));
        YmsEventBus.RaiseLookAtBarChanged(new HudBarSnapshot(""));
        YmsEventBus.RaiseJobBarChanged(new HudBarSnapshot(""));
        YmsEventBus.RaiseAlwaysOnExtrasChanged(new HudBarSnapshot(""));
        YmsEventBus.RaiseTrainGadgetsChanged(default);
        YmsEventBus.RaiseBackupProximityChanged(new HudBarSnapshot(""));
        YmsEventBus.RaiseLimitGovCue(LimitGovCue.None);
        YmsEventBus.RaiseMapsDestCommand(new MapsDestCommand(MapsDestKind.Clear));
    }
}

[CollectionDefinition("YmsEventBus", DisableParallelization = true)]
public sealed class YmsEventBusCollection
{
}
