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
}
