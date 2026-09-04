using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP5 / <b>13.2.4</b> — Prep GO creep ≤8 toward car; green/contact → Stop GO (no shove);
/// slam speed refuses Couple.
/// </summary>
public class HtpCreepToCoupleCp5Tests
{
    [Fact]
    public void Smoke_13_2_4_prep_go_request_is_creep_not_yard_crawl()
    {
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        var toTt = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));

        Assert.Equal(5f, PrepCreepPolicy.CreepRequestKmh);
        Assert.True(PrepCreepPolicy.CreepRequestKmh <= AutoCoupleAssist.MaxCoupleSpeedKmh);
        Assert.True(PrepCreepPolicy.WantsCreepCap(prep));
        Assert.False(PrepCreepPolicy.WantsCreepCap(toTt));
        Assert.Equal(PrepCreepPolicy.CreepRequestKmh, PidSpeedTarget.RequestForStep(prep));
        Assert.Equal(PidSpeedTarget.YardApproachRequestKmh, PidSpeedTarget.RequestForStep(toTt));
        Assert.True(AutoCoupleAssist.SpeedAllowsCouple(PidSpeedTarget.RequestForStep(prep)));
        Assert.False(AutoCoupleAssist.SpeedAllowsCouple(PidSpeedTarget.YardApproachRequestKmh + 0.1f));
    }

    [Fact]
    public void Smoke_13_2_4_green_clearance_stops_prep_go()
    {
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep");
        Assert.True(
            PrepCreepPolicy.IsGreenClearance(clearanceMeters: 0.4f, partnerInCoupleRange: true));
        Assert.False(
            PrepCreepPolicy.IsGreenClearance(clearanceMeters: 0.4f, partnerInCoupleRange: false));
        Assert.False(
            PrepCreepPolicy.IsGreenClearance(clearanceMeters: 1.2f, partnerInCoupleRange: true));

        Assert.True(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 0.4f,
                speedKmh: 8f,
                mechanicallyCoupled: false));
        Assert.True(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: null,
                speedKmh: 8f,
                mechanicallyCoupled: true));
        Assert.False(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 20f,
                speedKmh: 8f,
                mechanicallyCoupled: false));
        Assert.False(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Manual,
                prep,
                clearanceMeters: 0.4f,
                speedKmh: 8f,
                mechanicallyCoupled: false));
    }

    [Fact]
    public void Smoke_13_2_4_clearance_rem_d_stop_arms_before_hard_bump()
    {
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep");
        var creep = PrepCreepPolicy.CreepRequestKmh;
        Assert.True(
            YardStopKinematics.StoppingDistanceMeters(creep)
            <= BackupProximityDisplay.CoupleNearRangeMeters);

        // At creep: first scan (1.5 m) must arm stop — not wait for knuckle rem≤d_stop.
        Assert.True(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: BackupProximityDisplay.CoupleNearRangeMeters,
                speedKmh: creep,
                mechanicallyCoupled: false));
        Assert.False(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 20f,
                speedKmh: creep,
                mechanicallyCoupled: false));

        Assert.True(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: BackupProximityDisplay.CoupleNearRangeMeters,
                speedKmh: PidSpeedTarget.YardApproachRequestKmh,
                mechanicallyCoupled: false));
    }

    [Fact]
    public void Smoke_13_2_4_coupler_tick_stops_go_without_desk_poll()
    {
        SwitchListSession.Clear();
        PrepCreepSession.Clear();
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep");
        var haul = new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit");
        SwitchListSession.Bind("SW-FH-92", new[] { prep, haul });
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                prep,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));

        PrepCreepSession.Observe(
            clearanceMeters: BackupProximityDisplay.CoupleNearRangeMeters,
            speedKmh: PrepCreepPolicy.CreepRequestKmh,
            mechanicallyCoupled: false);
        Assert.True(PrepCreepSession.WantsCoupleStop);
        Assert.True(PrepCreepSession.TryStopGoIfNeeded(prep));
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.False(PrepCreepSession.TryStopGoIfNeeded(prep));
        SwitchListSession.Clear();
    }

    [Fact]
    public void Smoke_13_2_4_yard_chain_stop_couple_and_hold_no_rearm()
    {
        var steps = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past"),
            new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep"),
            new SwitchListStep(3, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit"),
        };

        Assert.Equal(
            SwitchListYardChainAction.StopGoAtCouple,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleStop: true));

        // After stop → Manual: sticky hold blocks ArmGo (kill shove loop).
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleHold: true));

        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleHold: false));
    }

    [Fact]
    public void Smoke_13_2_4_stop_go_on_prep_latches_hold_against_rearm()
    {
        SwitchListSession.Clear();
        PrepCreepSession.Clear();
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep");
        var haul = new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit");
        SwitchListSession.Bind("SW-FH-92", new[] { prep, haul });
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                prep,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));
        Assert.Equal(SwitchListRunMode.Go, SwitchListRunnerSession.Mode);

        Assert.Equal(SwitchListRunnerResult.Ok, SwitchListRunnerSession.TryStopGo());
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);

        var steps = new[] { prep, haul };
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunnerSession.Mode,
                prep,
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleHold: PrepCreepSession.HoldAfterCoupleStop));
        SwitchListSession.Clear();
    }

    [Fact]
    public void Smoke_13_2_4_mech_couple_observe_latches_hold()
    {
        PrepCreepSession.Clear();
        PrepCreepSession.Observe(clearanceMeters: null, speedKmh: 0f, mechanicallyCoupled: true);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.True(PrepCreepSession.WantsCoupleStop);
        PrepCreepSession.Clear();
    }

    [Fact]
    public void Smoke_13_2_4_slam_speed_refuses_couple_action()
    {
        var action = AutoCoupleAssist.Decide(
            hasTravelAim: true,
            hasTip: true,
            partnerInRange: true,
            mechanicallyCoupled: false,
            linkComplete: false,
            closeEnough: true,
            speedOk: AutoCoupleAssist.SpeedAllowsCouple(10f));
        Assert.Equal(AutoCoupleAction.None, action);

        var ok = AutoCoupleAssist.Decide(
            hasTravelAim: true,
            hasTip: true,
            partnerInRange: true,
            mechanicallyCoupled: false,
            linkComplete: false,
            closeEnough: true,
            speedOk: AutoCoupleAssist.SpeedAllowsCouple(7f));
        Assert.Equal(AutoCoupleAction.Couple, ok);
    }
}
