using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP3 thin — GO on drive legs (Transit + Prep approach); fail-closed arm (**13.4**).
/// Couple knuckles stay human until **13.2.4**; no full Validate UI (**13.3**).
/// </summary>
[Collection("StaticSessions")]
public class HtpAutonomousTransitCp3Tests
{
    public HtpAutonomousTransitCp3Tests() => YmsRouteSessions.ClearAll();

    private const float Dt = 0.05f;
    private const int HoldTicks = 400;

    [Fact]
    public void Smoke_13_4_prep_approach_go_arms_pid_without_take()
    {
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        var haul = new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I");
        var steps = new[] { prep, haul };
        SwitchListSession.Bind("SW-FH-82", steps);

        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(SwitchListRunner.StepSupportsGo(SwitchListStepKind.Prep));
        Assert.False(SwitchListTakeArm.IsHaulTransitTake(steps, 0, prep));
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                prep,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle,
                derailRiskPercent: 40f));
        Assert.True(SwitchListRunner.PidGoActive(
            SwitchListRunnerSession.Mode,
            SwitchListSession.CurrentStep));
        Assert.False(SwitchListTakeArm.IsHaulTransitTake(
            steps,
            SwitchListSession.CurrentIndex,
            SwitchListSession.CurrentStep));
    }

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
    public void Smoke_13_4_go_fail_closed_no_path_wrong_kind_derail()
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
        // Pin AtSwitch must still arm GO — CLEARED is stop, not start (Player.log idle).
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunner.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.AtSwitch,
                derailRiskPercent: 10f));
        Assert.Equal(
            SwitchListRunnerResult.WrongStepKind,
            SwitchListRunner.TrySetGo(
                new SwitchListStep(1, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery"),
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
                RouteClearancePhase.AtSwitch,
                derailRiskPercent: LimitThrottleCap.DerailIntervenePercent));
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunner.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.AtSwitch,
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
