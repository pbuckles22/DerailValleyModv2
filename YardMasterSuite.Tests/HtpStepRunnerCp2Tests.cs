using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP2 — Step runner GO / Human / Done on Switch List + Physics ticks (**13.1**).
/// </summary>
[Collection("StaticSessions")]
public class HtpStepRunnerCp2Tests
{
    public HtpStepRunnerCp2Tests() => YmsRouteSessions.ClearAll();
    private const float Dt = 0.05f;
    private const int HoldTicks = 400;

    [Fact]
    public void Smoke_13_1_go_on_transit_ticks_pid_on_corridor()
    {
        var spec = SwTurntableCorridorTests.SwToTurntable();
        var plan = RouteCorridorDrive.Plan(in spec);
        var steps = RouteCorridorDrive.BindSteps(
            in spec, plan, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.NotNull(steps);

        var transit = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Transit");
        SwitchListSession.Bind("route:SW", new[] { transit });
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);

        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                transit,
                hasPlan: true,
                pinForAlign: true,
                RouteClearancePhase.Cleared));

        var speed = 0f;
        var along = 0f;
        var throttle = 0f;
        var independent = 0f;
        var state = default(PidSpeedState);
        for (var i = 0; i < HoldTicks; i++)
        {
            var armed = SwitchListRunner.PidGoActive(
                SwitchListRunnerSession.Mode,
                SwitchListSession.CurrentStep);
            Assert.True(armed);
            var cmd = PidSpeedHold.Tick(
                new PidSpeedInput(
                    Dt,
                    speed,
                    PidSpeedTarget.DefaultRequestKmh,
                    postedKmh: null,
                    throttle,
                    independent,
                    armed,
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
    public void Smoke_13_1_SW_FH_82_six_rows_leave_past_switch_then_prep_human()
    {
        var snap = HtpFixtures.LoadCorridor();
        var leavePlan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            "#Y-#S1774#T",
            "SW-C1O",
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        var leave = SwitchListPlanner.TryPickTurntableApproachTrack(leavePlan);
        Assert.NotNull(leave);
        Assert.NotEqual("SW-C1O", leave);

        var job = new JobSummary
        {
            JobId = "SW-FH-82",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C1O",
            DestTrackId = "GF-D5I",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = leave,
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(6, steps!.Count);
        Assert.Equal(SwitchListStepKind.Transit, steps[2].Kind);
        Assert.True(SwitchListRunner.StepNeedsPinClearance(steps[2].Kind));
        Assert.Equal(SwitchListStepKind.Prep, steps[3].Kind);
        Assert.False(SwitchListRunner.StepNeedsPinClearance(steps[3].Kind));

        SwitchListSession.Bind(job.JobId, steps);
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(steps, 0, out _, out var inboundDest));
        Assert.Equal("#Y-#S1774#T", inboundDest);
        Assert.Equal(SwitchListRunnerResult.NotHumanHold, SwitchListRunnerSession.TryMarkDone());

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.TurnAround, SwitchListSession.CurrentStep!.Kind);
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal(leave, SwitchListSession.CurrentStep.DestTrackId);
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(RouteStepDestPolicy.TryPinCorridorDest(
            steps, SwitchListSession.CurrentIndex, out _, out var leaveDest));
        Assert.Equal("SW-C1O", leaveDest);

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal(SwitchListRunMode.HumanHold, SwitchListRunnerSession.Mode);
        Assert.False(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryMarkDone());
        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
    }

    [Fact]
    public void Smoke_13_1_human_reverse_into_holds_until_done()
    {
        var spec = SwTurntableCorridorTests.SwToTurntable();
        var plan = RouteCorridorDrive.Plan(in spec);
        var steps = RouteCorridorDrive.BindSteps(
            in spec, plan, pinNeedsReverse: true, destNeedsReverse: true);
        Assert.NotNull(steps);

        SwitchListSession.Bind("route:SW", steps);
        SwitchListSession.TryAdvance();
        var reverse = SwitchListSession.CurrentStep;
        Assert.Equal(SwitchListStepKind.ReverseInto, reverse!.Kind);
        Assert.Equal(SwitchListRunMode.HumanHold, SwitchListRunnerSession.Mode);
        Assert.False(SwitchListRunner.PidGoActive(SwitchListRunnerSession.Mode, reverse));
        Assert.False(SwitchListSession.TryAdvance());

        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryMarkDone());
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.False(SwitchListSession.TryAdvance());
        Assert.True(SwitchListSession.IsComplete);
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
