using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP3 thin — one Transit leg GO after CLEARED/Align; fail-closed arm (**13.4**).
/// Prep stays manual; no full Validate UI (**13.3**).
/// </summary>
[Collection("StaticSessions")]
public class HtpAutonomousTransitCp3Tests
{
    public HtpAutonomousTransitCp3Tests() => YmsRouteSessions.ClearAll();

    private const float Dt = 0.05f;
    private const int HoldTicks = 400;

    [Fact]
    public void Smoke_13_4_cleared_align_go_ticks_pid_on_one_transit()
    {
        var transit = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Transit");
        SwitchListSession.Bind("route:SW", new[] { transit });

        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Align(hasPin: true, RouteClearancePhase.Cleared));
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.Cleared,
                derailRiskPercent: 40f));

        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 0f;
        var state = default(PidSpeedState);
        for (var i = 0; i < HoldTicks; i++)
        {
            Assert.True(SwitchListRunner.PidGoActive(
                SwitchListRunnerSession.Mode,
                SwitchListSession.CurrentStep));
            var cmd = PidSpeedHold.Tick(
                new PidSpeedInput(
                    Dt,
                    speed,
                    PidSpeedTarget.DefaultRequestKmh,
                    postedKmh: null,
                    throttle,
                    independent,
                    armed: true,
                    derailIntervening: false,
                    thermalCeiling: 1f,
                    reverser: 1f,
                    legNeedsReverse: false,
                    trainBrake: 0f),
                ref state);
            Assert.True(cmd.Active);
            CabPlant(cmd, ref speed, ref along, ref throttle, ref independent);
        }

        Assert.True(along > 10f);
    }

    [Fact]
    public void Smoke_13_4_go_fail_closed_no_path_not_cleared_prep_derail()
    {
        var transit = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "#Y-#S969#T", "Past switch");
        Assert.Equal(
            SwitchListRunnerResult.NeedPlan,
            SwitchListRunner.TrySetGo(
                transit,
                hasPlan: false,
                pinForAlign: true,
                RouteClearancePhase.Cleared,
                derailRiskPercent: null));
        Assert.Equal(
            SwitchListRunnerResult.NeedCleared,
            SwitchListRunner.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.AtSwitch,
                derailRiskPercent: 10f));
        Assert.Equal(
            SwitchListRunnerResult.WrongStepKind,
            SwitchListRunner.TrySetGo(
                new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-B3I", "Prep"),
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle,
                derailRiskPercent: null));
        Assert.Equal(
            SwitchListRunnerResult.RefuseDerail,
            SwitchListRunner.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.Cleared,
                derailRiskPercent: LimitThrottleCap.DerailIntervenePercent));
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunner.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.Cleared,
                derailRiskPercent: LimitThrottleCap.DerailIntervenePercent - 1f));
    }

    private static void CabPlant(
        in PidSpeedCommand cmd,
        ref float speed,
        ref float along,
        ref float throttle,
        ref float independent)
    {
        var want = PidSpeedTelemetry.WantsThrottle(
            armed: true,
            cmd.GearPending,
            cmd.BrakePending,
            cmd.DesiredThrottle);
        PidSpeedCab.Apply(cmd, want, ref throttle, ref independent);
        PidSpeedPlant.Step(ref speed, ref along, throttle, independent, Dt, LocoTypeId.De2);
    }
}
