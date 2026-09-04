using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class TurntableArrivalGateTests
{
    public TurntableArrivalGateTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_4_drive_to_tt_latches_on_dest_rail()
    {
        var toTt = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));
        Assert.True(TurntableArrivalGate.StepWantsArrival(toTt));
        Assert.Equal(
            TurntableArrival.AtTrack,
            TurntableArrivalGate.Evaluate(
                toTt,
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 5f,
                trackLengthMeters: 20f,
                uniqueTrack: true));
        Assert.True(TurntableArrivalSession.TryArrive(TurntableArrival.AtTrack));
        Assert.True(TurntableArrivalSession.OnTable);
        Assert.Equal("on TT #Y-#S1774#T", TurntableArrivalGate.FormatDeskCue("#Y-#S1774#T"));
        Assert.Equal(
            "T2 switch-list: yard-chain stop-tt",
            SwitchListRunnerTelemetry.YardChainStopTt);
    }

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
                true));
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
        var toTt = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));
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

        // Probe leaves TT rail briefly while crawl-stopping / reversing.
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
