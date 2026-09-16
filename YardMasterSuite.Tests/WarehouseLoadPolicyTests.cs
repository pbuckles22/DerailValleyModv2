using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab auto-load: stay seated; warehouse machine runs like TT auto-spin.
/// </summary>
[Collection("StaticSessions")]
public class WarehouseLoadPolicyTests
{
    public WarehouseLoadPolicyTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_load_human_hold_at_rest_on_loader_starts()
    {
        var step = LoadStep();
        Assert.True(WarehouseLoadPolicy.StepIsLoad(step));
        Assert.False(WarehouseLoadPolicy.IsUnload(step.Label));
        Assert.True(WarehouseLoadPolicy.OnLoaderTrack("SW-B4L", "SW-B4L", uniqueTrack: true));
        Assert.False(WarehouseLoadPolicy.OnLoaderTrack("SW-B4L", "SW-B4L", uniqueTrack: false));
        Assert.True(
            WarehouseLoadPolicy.ShouldStartLoad(
                SwitchListRunMode.HumanHold,
                step,
                onLoaderTrack: true,
                carsReady: true,
                goStopActive: false,
                speedKmh: 0f,
                throttle01: 0f,
                loadActive: false,
                loadLocked: false,
                loadAttempted: false));
    }

    [Fact]
    public void Smoke_load_does_not_start_while_rolling_or_after_attempt()
    {
        var step = LoadStep();
        Assert.False(
            WarehouseLoadPolicy.ShouldStartLoad(
                SwitchListRunMode.HumanHold,
                step,
                onLoaderTrack: true,
                carsReady: true,
                goStopActive: false,
                speedKmh: 4f,
                throttle01: 0f,
                loadActive: false,
                loadLocked: false,
                loadAttempted: false));
        Assert.False(
            WarehouseLoadPolicy.ShouldStartLoad(
                SwitchListRunMode.HumanHold,
                step,
                onLoaderTrack: true,
                carsReady: true,
                goStopActive: false,
                speedKmh: 0f,
                throttle01: 0f,
                loadActive: false,
                loadLocked: false,
                loadAttempted: true));
        Assert.False(
            WarehouseLoadPolicy.ShouldStartLoad(
                SwitchListRunMode.Manual,
                step,
                onLoaderTrack: true,
                carsReady: true,
                goStopActive: false,
                speedKmh: 0f,
                throttle01: 0f,
                loadActive: false,
                loadLocked: false,
                loadAttempted: false));
    }

    [Fact]
    public void Smoke_load_finishes_when_machine_locked()
    {
        var step = LoadStep();
        Assert.False(WarehouseLoadPolicy.ShouldFinishLoad(step, loadLocked: false));
        Assert.True(WarehouseLoadPolicy.ShouldFinishLoad(step, loadLocked: true));
    }

    [Fact]
    public void Smoke_session_locks_after_work_then_idle()
    {
        WarehouseLoadSession.Clear();
        WarehouseLoadSession.Begin();
        WarehouseLoadSession.Observe(ongoing: true, stillHasWork: true);
        Assert.True(WarehouseLoadSession.Active);
        Assert.False(WarehouseLoadSession.Locked);
        WarehouseLoadSession.Observe(ongoing: false, stillHasWork: false);
        Assert.True(WarehouseLoadSession.Locked);
        Assert.False(WarehouseLoadSession.Active);
        WarehouseLoadSession.Clear();
    }

    [Fact]
    public void Smoke_yard_chain_load_start_then_done_next()
    {
        var steps = new[]
        {
            new SwitchListStep(
                4,
                SwitchListStepKind.Prep,
                "SW",
                "SW-B4L",
                SwitchListWarehouseLegs.SpotLabel("SW-B4L")),
            LoadStep(),
            new SwitchListStep(
                6,
                SwitchListStepKind.Transit,
                "SW",
                "SW-C1O",
                "Past switch → SW-C1O until CLEARED"),
        };

        Assert.Equal(
            SwitchListYardChainAction.StartWarehouseLoad,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.HumanHold,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onLoaderTrack: true,
                loaderCarsReady: true,
                speedKmh: 0f,
                throttle01: 0f));

        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.HumanHold,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onLoaderTrack: true,
                loaderCarsReady: true,
                warehouseLoadActive: true,
                warehouseLoadAttempted: true,
                speedKmh: 0f,
                throttle01: 0f));

        Assert.Equal(
            SwitchListYardChainAction.WarehouseLoadDone,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.HumanHold,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                warehouseLoadLocked: true));
    }

    private static SwitchListStep LoadStep() =>
        new(
            5,
            SwitchListStepKind.Load,
            "SW",
            "SW-B4L",
            "Load Wood Chips at SW-B4L");
}
