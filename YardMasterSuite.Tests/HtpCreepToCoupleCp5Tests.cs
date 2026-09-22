using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP5 / <b>13.2.4</b> — Prep GO creep ≤8 toward car; green/contact → Stop GO (no shove);
/// slam speed refuses Couple.
/// </summary>
[Collection("StaticSessions")]
public class HtpCreepToCoupleCp5Tests
{
    public HtpCreepToCoupleCp5Tests() => YmsRouteSessions.ClearAll();
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

        Assert.Equal(3f, PrepCreepPolicy.CreepRequestKmh);
        Assert.True(PrepCreepPolicy.CreepRequestKmh <= AutoCoupleAssist.MaxCoupleSpeedKmh);
        Assert.True(PrepCreepPolicy.WantsCreepCap(prep));
        Assert.False(PrepCreepPolicy.WantsCreepCap(toTt));
        Assert.Equal(
            PrepCreepPolicy.CreepRequestKmh,
            PidSpeedTarget.RequestForYardStep(prep, null, 1.5f, null, null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(toTt, 40f, null, null, null));
        Assert.True(
            AutoCoupleAssist.SpeedAllowsCouple(
                PidSpeedTarget.RequestForYardStep(prep, null, 1.5f, null, null)));
        Assert.False(AutoCoupleAssist.SpeedAllowsCouple(YardKissPolicy.CruiseKmh));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                remToAimMeters: 1.5f,
                speedKmh: YardKissPolicy.CruiseKmh));
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtCouple,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                prep,
                new[] { prep },
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleStop: true,
                remToAimMeters: 1.5f,
                speedKmh: PrepCreepPolicy.CreepRequestKmh));
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
        Assert.False(
            PrepCreepPolicy.ShouldArmCreepInSafetyZone(0.4f, 0f));
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
        Assert.False(
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

        Assert.False(
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
            mechanicallyCoupled: true);
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
        Assert.False(PrepCreepSession.HoldAfterCoupleStop);
        PrepCreepSession.LatchCoupleHold();
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
        PrepCreepSession.Observe(
            clearanceMeters: null,
            speedKmh: 0f,
            mechanicallyCoupled: true,
            spurPickupComplete: false);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.True(PrepCreepSession.WantsCoupleStop);
        PrepCreepSession.Observe(
            clearanceMeters: null,
            speedKmh: 0f,
            mechanicallyCoupled: true,
            spurPickupComplete: true);
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
            speedOk: AutoCoupleAssist.SpeedAllowsCouple(3f));
        Assert.Equal(AutoCoupleAction.Couple, ok);
    }

    /// <summary>
    /// Cab 22.54: kiss dump physically coupled (cars=1→3) with no autocouple
    /// done, then arm-go step 5 shoved at 3 km/h. Knuckle/hold must sit.
    /// </summary>
    [Fact]
    public void Smoke_22_54_kiss_dump_consist_grow_holds_and_does_not_rearm_go()
    {
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S");
        var steps = new[]
        {
            new SwitchListStep(4, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Past"),
            prep,
        };
        Assert.True(
            PrepCoupleExitGate.ShouldLatchHoldOnConsistGrow(
                SwitchListStepKind.Prep,
                fromCarCount: 1,
                toCarCount: 3));
        Assert.False(
            PrepCoupleExitGate.ShouldLatchHoldOnConsistGrow(
                SwitchListStepKind.Prep,
                fromCarCount: 0,
                toCarCount: 1));
        Assert.False(
            PrepCoupleExitGate.ShouldLatchHoldOnConsistGrow(
                SwitchListStepKind.Transit,
                fromCarCount: 1,
                toCarCount: 3));

        PrepCreepSession.Clear();
        PrepCreepSession.LatchKnuckle();
        Assert.False(
            SwitchListYardChain.ShouldAutoArmGo(
                SwitchListRunMode.Manual,
                prep,
                inYardPrepScope: true,
                pinBlocksAlign: false,
                RouteClearancePhase.Idle,
                prepCoupleHold: false,
                prepTipCoupled: true));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: null,
                speedKmh: 0f,
                prepCoupleHold: false,
                prepTipCoupled: true));
        PrepCreepSession.Clear();
    }

    [Fact]
    public void Smoke_C4S_prep_stopped_short_resumes_3kmh_creep()
    {
        Assert.True(PrepCreepPolicy.ShouldDropHoldForShortStop(
            holdAfterCouple: true,
            tipCoupled: false,
            speedKmh: 0f,
            clearanceMeters: 1.6f));
        Assert.False(PrepCreepPolicy.ShouldDropHoldForShortStop(
            holdAfterCouple: true,
            tipCoupled: true,
            speedKmh: 0f,
            clearanceMeters: 1.6f));
        Assert.False(PrepCreepPolicy.ShouldDropHoldForShortStop(
            holdAfterCouple: true,
            tipCoupled: false,
            speedKmh: 0f,
            clearanceMeters: 0.4f));
        Assert.False(PrepCreepPolicy.ShouldDropHoldForShortStop(
            holdAfterCouple: true,
            tipCoupled: false,
            speedKmh: 0f,
            clearanceMeters: 1.6f,
            partnerRefused: true));
        Assert.True(AutoCoupleAssist.ShouldHoldCreepForRefusedPartner(
            switchListActive: true,
            SwitchListStepKind.Prep,
            mechanicallyCoupled: false,
            partnerInRange: true,
            partnerAllowsCouple: false,
            clearanceMeters: 0.4f));
        Assert.False(AutoCoupleAssist.ShouldHoldCreepForRefusedPartner(
            switchListActive: true,
            SwitchListStepKind.Prep,
            mechanicallyCoupled: false,
            partnerInRange: true,
            partnerAllowsCouple: true,
            clearanceMeters: 0.4f));
        Assert.Equal(0f, YardKissPolicy.RequestKmh(
            new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep"),
            hudProximityMeters: 0.4f));
        Assert.True(AutoCoupleAssist.ClearanceAllowsSlideCouple(0.54f));
        Assert.False(AutoCoupleAssist.ClearanceAllowsSlideCouple(1.2f));
        Assert.False(PrepCreepPolicy.ShouldDropHoldForShortStop(
            holdAfterCouple: true,
            tipCoupled: false,
            speedKmh: 3f,
            clearanceMeters: 1.6f));

        SwitchListSession.Clear();
        PrepCreepSession.Clear();
        var prep = new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep");
        SwitchListSession.Bind("SW-SL-55", new[] { prep });
        PrepCreepSession.Observe(1.6f, 0f, mechanicallyCoupled: false);
        PrepCreepSession.LatchCoupleHold();
        Assert.True(PrepCreepSession.TryResumeAfterShortStop(0f));
        Assert.False(PrepCreepSession.HoldAfterCoupleStop);
        Assert.True(PrepCreepPolicy.ShouldArmCreepInSafetyZone(1.6f, 0f));
        SwitchListSession.Clear();
    }

    [Fact]
    public void Smoke_21618_kiss_dump_stays_while_fast_walks_at_3_from_rest()
    {
        var prep = new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep");

        Assert.False(PrepCreepPolicy.ShouldArmCreepInSafetyZone(9.2f, 16f));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                9.2f,
                16f));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                new[] { prep },
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                goStopActive: true,
                remToAimMeters: 9.2f,
                speedKmh: 16f));

        Assert.True(PrepCreepPolicy.ShouldArmCreepInSafetyZone(9.2f, 0f));
        Assert.Equal(
            PrepCreepPolicy.CreepRequestKmh,
            YardKissPolicy.RequestKmh(prep, hudProximityMeters: 9.2f));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                9.2f,
                0f));
        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                new[] { prep },
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                goStopActive: false,
                remToAimMeters: 9.2f,
                speedKmh: 0f));
        Assert.False(PrepCreepPolicy.ShouldArmCreepInSafetyZone(0.4f, 0f));
        Assert.Equal(0f, YardKissPolicy.RequestKmh(prep, hudProximityMeters: 0.4f));
    }

    /// <summary>
    /// Cab 2.16.21: 38 t knuckle twitched to 4. 86 t ran to 9 while train
    /// brake was still 1–8% after the handbrake let go. Latch on the heavy
    /// walk, then couple-hold snaps train air. The light walk still slews.
    /// </summary>
    [Fact]
    public void Smoke_21621_heavy_prep_snaps_train_brake_on_couple()
    {
        Assert.False(PrepCreepPolicy.IsHeavyApproach(38f));
        Assert.True(PrepCreepPolicy.IsHeavyApproach(86f));
        Assert.False(PrepCreepPolicy.ShouldLatchHeavyKnuckle(
            coupleHold: false,
            tipCoupled: false,
            remMeters: 7f,
            requestKmh: PrepCreepPolicy.CreepRequestKmh,
            massTonnes: 38f));
        Assert.True(PrepCreepPolicy.ShouldLatchHeavyKnuckle(
            coupleHold: false,
            tipCoupled: false,
            remMeters: 7f,
            requestKmh: PrepCreepPolicy.CreepRequestKmh,
            massTonnes: 86f));
        Assert.False(PrepCreepPolicy.ShouldLatchHeavyKnuckle(
            coupleHold: true,
            tipCoupled: true,
            remMeters: 1f,
            requestKmh: PrepCreepPolicy.CreepRequestKmh,
            massTonnes: 110f));
        Assert.False(PrepCreepPolicy.ShouldSnapTrainOnHeavyKnuckle(
            coupleHold: true,
            heavyApproachLatched: false));
        Assert.True(PrepCreepPolicy.ShouldSnapTrainOnHeavyKnuckle(
            coupleHold: true,
            heavyApproachLatched: true));

        var light = PidGoStop.Tick(
            0.02f,
            throttle: 0f,
            independent: 1f,
            train: 0f,
            reverser: 0f,
            snapTrain: false);
        Assert.True(light.DesiredTrain < 0.05f);

        var heavy = PidGoStop.Tick(
            0.02f,
            throttle: 0f,
            independent: 1f,
            train: 0f,
            reverser: 0f,
            snapTrain: true);
        Assert.Equal(PidGoStop.StopTrain, heavy.DesiredTrain);
        Assert.Equal(PidGoStop.StopIndependent, heavy.DesiredIndependent);
        Assert.Equal("T2 pid: heavy-knuckle", PidSpeedTelemetry.HeavyKnuckle);
    }
}
