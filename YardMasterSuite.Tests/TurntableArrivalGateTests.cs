using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class TurntableArrivalGateTests
{
    public TurntableArrivalGateTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_4_drive_to_tt_latches_on_dest_rail()
    {
        var toTt = DriveToTtStep();
        Assert.True(TurntableArrivalGate.StepWantsArrival(toTt));
        Assert.Equal(
            TurntableArrival.AtTrack,
            TurntableArrivalGate.Evaluate(
                toTt,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 12.5f,
                trackLengthMeters: 25f,
                uniqueTrack: true,
                speedKmh: 0f));
        Assert.True(TurntableArrivalSession.TryArrive(TurntableArrival.AtTrack));
        Assert.True(TurntableArrivalSession.OnTable);
        Assert.Equal("on TT #Y-#S1774#T", TurntableArrivalGate.FormatDeskCue("#Y-#S1774#T"));
        Assert.Equal(
            "T2 switch-list: yard-chain stop-tt",
            SwitchListRunnerTelemetry.YardChainStopTt);
    }

    /// <summary>
    /// Cab 2.13.4.15 FAIL: mid-only latch at ~25 km/h → stop at pit rim, not centered.
    /// Crawl on entry must keep rolling toward mid; hot entry must arm early (rem ≤ d_stop).
    /// </summary>
    [Fact]
    public void Smoke_13_4_16_crawl_entry_waits_for_mid_hot_entry_arms_early()
    {
        var toTt = DriveToTtStep();
        Assert.Equal(
            TurntableArrival.OffTrack,
            TurntableArrivalGate.Evaluate(
                toTt,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 1f,
                trackLengthMeters: 25f,
                uniqueTrack: true,
                speedKmh: 5f));
        Assert.Equal(
            TurntableArrival.AtTrack,
            TurntableArrivalGate.Evaluate(
                toTt,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 1f,
                trackLengthMeters: 25f,
                uniqueTrack: true,
                speedKmh: 25f));
        Assert.True(
            TurntableArrivalGate.YardStoppingDistanceMeters(25f)
            > 12f - TurntableArrivalGate.MidpointToleranceMeters);
        Assert.Equal(2f, YardStopKinematics.DecelMetersPerSecSq);
    }

    [Fact]
    public void Smoke_13_4_15_tt_midpoint_band_latches()
    {
        var toTt = DriveToTtStep();
        Assert.Equal(12.5f, TurntableArrivalGate.MidpointAlongMeters(25f));
        Assert.Equal(2f, TurntableArrivalGate.MidpointToleranceMeters);
        Assert.Equal(
            TurntableArrival.AtTrack,
            TurntableArrivalGate.Evaluate(
                toTt,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 10.5f,
                trackLengthMeters: 25f,
                uniqueTrack: true,
                speedKmh: 5f));
        Assert.Equal(
            TurntableArrival.AtTrack,
            TurntableArrivalGate.Evaluate(
                toTt,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 14.5f,
                trackLengthMeters: 25f,
                uniqueTrack: true,
                speedKmh: 5f));
        Assert.Equal(
            "T2 switch-list: on TT along=1 len=25 spd=25",
            TurntableArrivalGate.FormatLatchLog(1f, 25f, 25f));
    }

    private static SwitchListStep DriveToTtStep() =>
        new(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));

    [Fact]
    public void Smoke_13_4_on_table_spin_row_does_not_want_arrival()
    {
        var spin = new SwitchListStep(
            3,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.TurnAroundOnTurntable);
        Assert.False(TurntableArrivalGate.StepWantsArrival(spin));
        Assert.Equal(
            TurntableArrival.OffTrack,
            TurntableArrivalGate.Evaluate(
                spin,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                5f,
                20f,
                true,
                0f));
    }

    /// <summary>
    /// Cab 2.13.4.11: stop-tt → go-stop done → arm-go (overshoot / reverse).
    /// OffTrack probe flicker must not drop the on-table latch mid drive-to-TT.
    /// </summary>
    [Fact]
    public void Smoke_13_4_tt_stop_offtrack_flicker_keeps_on_table()
    {
        Assert.True(TurntableArrivalSession.TryArrive(TurntableArrival.AtTrack));
        Assert.True(TurntableArrivalSession.OnTable);
        Assert.False(TurntableArrivalSession.TryArrive(TurntableArrival.OffTrack));
        Assert.True(TurntableArrivalSession.OnTable);
        Assert.False(TurntableArrivalSession.TryArrive(TurntableArrival.Ambiguous));
        Assert.True(TurntableArrivalSession.OnTable);
        TurntableArrivalSession.Clear();
        Assert.False(TurntableArrivalSession.OnTable);
    }

    [Fact]
    public void Smoke_13_4_tt_stop_go_stop_done_does_not_rearm()
    {
        var toTt = DriveToTtStep();
        var steps = new[] { toTt };

        Assert.True(TurntableArrivalSession.TryArrive(TurntableArrival.AtTrack));
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                toTt,
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: TurntableArrivalSession.OnTable));

        TurntableArrivalSession.TryArrive(TurntableArrival.OffTrack);
        PidGoStopSession.Clear();

        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                toTt,
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                goStopActive: false,
                onTurntable: TurntableArrivalSession.OnTable));
    }
}
