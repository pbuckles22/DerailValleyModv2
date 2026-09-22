using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP4 — Prep knuckle holds; GO-after-couple is pull-out (**13.2.5**).
/// </summary>
[Collection("StaticSessions")]
public class HtpCoupleAutoAdvanceCp4Tests
{
    public HtpCoupleAutoAdvanceCp4Tests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_2_5_22_36_prep_couple_advances_list_hold_blocks_armgo()
    {
        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
                new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
            });

        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: false));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.False(PrepCreepSession.HoldAfterCoupleStop);

        PrepSpurPickupSession.Observe(attachedJobCars: 5, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.Equal("T2 switch-list: couple-hold", SwitchListRunnerTelemetry.CoupleHold);
        Assert.False(SwitchListYardChain.ShouldAutoArmGo(
            SwitchListRunMode.Manual,
            SwitchListSession.CurrentStep,
            inYardPrepScope: true,
            pinBlocksAlign: false,
            RouteClearancePhase.Idle,
            prepCoupleHold: PrepCreepSession.HoldAfterCoupleStop));
    }

    /// <summary>
    /// Cab 22.36: knuckle during Prep GO did not Next. TryAdvance still
    /// requires Manual; couple-success must Stop GO first, keep ArmGo off.
    /// </summary>
    [Fact]
    public void TryAdvanceOnCoupleSuccess_WhileGoActive_AdvancesList()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
            });
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                SwitchListSession.CurrentStep,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));
        Assert.Equal(SwitchListRunMode.Go, SwitchListRunnerSession.Mode);
        Assert.False(SwitchListRunner.AllowsManualNext(SwitchListRunMode.Go, hasNextStep: true));

        PrepSpurPickupSession.Observe(attachedJobCars: 5, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.NotEqual(SwitchListRunMode.Go, SwitchListRunnerSession.Mode);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.False(SwitchListYardChain.ShouldAutoArmGo(
            SwitchListRunnerSession.Mode,
            SwitchListSession.CurrentStep,
            inYardPrepScope: true,
            pinBlocksAlign: false,
            RouteClearancePhase.Idle,
            prepCoupleHold: PrepCreepSession.HoldAfterCoupleStop));
    }

    [Fact]
    public void Smoke_13_2_5_22_2_GO_stops_and_nexts_when_B1S_job_cars_are_on_hook()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Set Reverse · Prep → SW-B1S"),
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
            });
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                SwitchListSession.CurrentStep,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));
        Assert.Equal(SwitchListRunMode.Go, SwitchListRunnerSession.Mode);
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Go,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: true));

        PrepSpurPickupSession.Observe(attachedJobCars: 5, unattachedOnPrepSpur: 0);
        Assert.True(PrepSpurPickupSession.IsComplete);
        PrepCreepSession.Observe(
            clearanceMeters: null,
            speedKmh: 24f,
            mechanicallyCoupled: false,
            spurPickupComplete: true);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.True(PrepCreepSession.WantsCoupleStop);
        Assert.True(PrepCreepSession.TryStopGoIfNeeded(SwitchListSession.CurrentStep));
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal("SW-B4L", SwitchListSession.CurrentStep!.DestTrackId);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
    }

    /// <summary>
    /// Cab: couple-next + facing-prep at 2 km/h blew TMS. Knuckle holds Prep;
    /// yard-chain ArmGo stays off until GO-after-couple at crawl.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_1_prep_couple_holds_so_ArmGo_does_not_shove()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
            });
        PrepSpurPickupSession.Observe(attachedJobCars: 5, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                SwitchListSession.CurrentStep,
                SwitchListSession.Steps,
                SwitchListSession.CurrentIndex,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleHold: PrepCreepSession.HoldAfterCoupleStop));
        Assert.False(SwitchListStepPrereq.ShouldWriteFacingPrep(2f));
    }

    /// <summary>
    /// Cab 22.37: Next to Forward Past C4S, live R, thr 100%, TB 91%, speed 0.
    /// Do not write F or ArmGo until fully stopped and idle.
    /// </summary>
    [Fact]
    public void AdvanceOnCouple_StopsGoAndAdvances_HoldsReverserAndArm_UntilFullyStopped()
    {
        var pullOut = new SwitchListStep(
            6,
            SwitchListStepKind.Transit,
            "SW",
            "SW-C4S",
            "Set Forward · Past switch → SW-C4S until CLEARED",
            bindNeedsReverse: false);
        Assert.False(SwitchListStepPrereq.ShouldWriteFacingPostCouple(4f, 1f, MotorStatus.Ok));
        Assert.False(SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
            pullOut,
            holdAfterCouple: true,
            fullyStoppedAndIdle: false,
            reverserMatches: false));
        Assert.False(SwitchListStepPrereq.ShouldWriteFacingPrep(2f));
        Assert.False(SwitchListRunner.AllowsManualNext(SwitchListRunMode.Go, hasNextStep: true));
        Assert.False(PidGoStop.ShouldClearStopAtCrawl(coupleHold: true, speedKmh: 0.5f));
        Assert.True(PidGoStop.ShouldClearStopAtCrawl(coupleHold: false, speedKmh: 0.5f));
    }

    [Fact]
    public void AdvanceOnCouple_WhenFullyStopped_FlipsReverser_ThenClearsHold()
    {
        var pullOut = new SwitchListStep(
            6,
            SwitchListStepKind.Transit,
            "SW",
            "SW-C4S",
            "Set Forward · Past switch → SW-C4S until CLEARED",
            bindNeedsReverse: false);
        Assert.True(SwitchListStepPrereq.ShouldWriteFacingPostCouple(0f, 0f, MotorStatus.Ok));
        Assert.False(SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
            pullOut,
            holdAfterCouple: true,
            fullyStoppedAndIdle: true,
            reverserMatches: false));
        Assert.True(SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
            pullOut,
            holdAfterCouple: true,
            fullyStoppedAndIdle: true,
            reverserMatches: true));
        Assert.False(SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep"),
            holdAfterCouple: true,
            fullyStoppedAndIdle: true,
            reverserMatches: true));
    }

    /// <summary>
    /// Cab 2.13.2.5.3: GO on coupled Prep re-armed the same row → instant
    /// stop-couple (GO did nothing). GO after couple-hold advances to the
    /// B4L pull-out and drops the hold so ArmGo / GO can start Forward.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_4_go_after_couple_hold_advances_to_pullout()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
                new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
            });
        PrepCreepSession.Observe(
            clearanceMeters: 0.4f,
            speedKmh: 0f,
            mechanicallyCoupled: true,
            spurPickupComplete: true);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
        Assert.True(PrepCreepSession.WantsCoupleStop);
        Assert.True(SwitchListYardChain.ShouldGoAdvanceAfterCoupleHold(
            SwitchListSession.CurrentStep,
            PrepCreepSession.HoldAfterCoupleStop,
            SwitchListSession.PeekNext != null));
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtCouple,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                SwitchListSession.CurrentStep,
                SwitchListSession.Steps,
                SwitchListSession.CurrentIndex,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleStop: PrepCreepSession.WantsCoupleStop,
                prepCoupleHold: PrepCreepSession.HoldAfterCoupleStop));

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.False(PrepCreepSession.HoldAfterCoupleStop);
        Assert.False(SwitchListYardChain.ShouldGoAdvanceAfterCoupleHold(
            SwitchListSession.CurrentStep,
            PrepCreepSession.HoldAfterCoupleStop,
            SwitchListSession.PeekNext != null));
        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                SwitchListSession.CurrentStep,
                SwitchListSession.Steps,
                SwitchListSession.CurrentIndex,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true));
        Assert.Equal("T2 switch-list: go-after-couple", SwitchListRunnerTelemetry.GoAfterCouple);
        Assert.Equal("T2 switch-list: couple-pull-out", SwitchListRunnerTelemetry.CouplePullOut);
    }

    [Fact]
    public void Smoke_13_2_1_couple_success_skips_non_prep_and_last_human()
    {
        SwitchListSession.Bind(
            "route:SW",
            new[]
            {
                new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Transit"),
                new SwitchListStep(2, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep"),
            });
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);

        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(7, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery"),
            });
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Delivery, SwitchListSession.CurrentStep!.Kind);

        SwitchListSession.Bind(
            "yard",
            new[]
            {
                new SwitchListStep(3, SwitchListStepKind.ReverseInto, "SW", "SW-C1O", "Reverse into"),
                new SwitchListStep(4, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep"),
            });
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(SwitchListStepKind.ReverseInto, SwitchListSession.CurrentStep!.Kind);
    }

    [Fact]
    public void Smoke_13_2_1_couple_gate_is_hold_not_next()
    {
        Assert.True(SwitchListRunner.ShouldLatchCoupleHold(
            SwitchListStepKind.Prep,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldLatchCoupleHold(
            SwitchListStepKind.Prep,
            coupleSuccess: false));
        Assert.False(SwitchListRunner.ShouldLatchCoupleHold(
            SwitchListStepKind.Transit,
            coupleSuccess: true));
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Manual,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: true));
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Go,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Manual,
            hasNextStep: false,
            coupleSuccess: true,
            spurPickupComplete: true));
    }

    /// <summary>
    /// Cab 22.38: knuckle + auto-move worked, then 25 km/h blew switch #6
    /// with no At switch. Leftover B4L CLEARED stayed on Past C4S (rem=0
    /// cruise). Couple-Next must drop that pin so C4S can latch At switch.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_22_38_couple_next_onto_c4s_drops_leftover_cleared()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-C4S",
                    "Set Forward · Past switch → SW-C4S until CLEARED",
                    bindNeedsReverse: false),
            });

        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.AtSwitch,
                fouling: true,
                canThrowAlign: false,
                canAdvanceNext: false,
                caption: "At switch"),
            pinJunctionId: "1003030",
            pinX: 0f,
            pinY: 0f,
            pinZ: 0f);
        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.Cleared,
                fouling: false,
                canThrowAlign: true,
                canAdvanceNext: true,
                caption: "CLEARED"),
            pinJunctionId: "1003030",
            pinX: 0f,
            pinY: 0f,
            pinZ: 0f);
        Assert.Equal(RouteClearancePhase.Cleared, RouteClearanceSession.Phase);
        Assert.True(RouteClearanceSession.HasPin);

        PrepSpurPickupSession.Observe(attachedJobCars: 5, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal("SW-C4S", SwitchListSession.CurrentStep!.DestTrackId);
        Assert.False(RouteClearanceSession.HasPin);
        Assert.Equal(RouteClearancePhase.Idle, RouteClearanceSession.Phase);
        Assert.False(RouteClearanceSession.SawAtSwitchThisLeg);

        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.AtSwitch,
                fouling: true,
                canThrowAlign: false,
                canAdvanceNext: false,
                caption: "At switch"),
            pinJunctionId: "1576058",
            pinX: 1f,
            pinY: 0f,
            pinZ: 0f);
        Assert.Equal(RouteClearancePhase.AtSwitch, RouteClearanceSession.Phase);
        Assert.True(RouteClearanceSession.SawAtSwitchThisLeg);
        Assert.Equal("1576058", RouteClearanceSession.PinJunctionId);
    }

    /// <summary>
    /// Cab 22.41: C4S autocouple Next'd onto Past B4L while look-at still
    /// SW-C4S car=3. First knuckle is not the spur.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_22_41_second_pickup_stays_on_prep_until_spur_complete()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
                new SwitchListStep(
                    8,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
            });
        PrepSpurPickupSession.Observe(attachedJobCars: 4, unattachedOnPrepSpur: 3);
        Assert.False(PrepSpurPickupSession.IsComplete);
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Go,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: false));
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);

        PrepSpurPickupSession.Observe(attachedJobCars: 7, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
    }

    [Fact]
    public void Smoke_SL55_B1S_quota_complete_advances_before_C4S_cars_are_on_consist()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
                new SwitchListStep(2, SwitchListStepKind.Transit, "SW", "SW-B4L", "pull out"),
                new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
            });
        PrepSpurPickupSession.Observe(
            expectedThisSpurJobCars: 2,
            attachedThisSpurJobCars: 2,
            unattachedOnPrepSpur: 0);
        Assert.True(PrepSpurPickupSession.IsComplete);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
    }

    [Fact]
    public void Smoke_C4S_packed_spur_uncouples_foreign_before_quota_advance()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
                new SwitchListStep(8, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past switch → SW-B4L"),
            });
        PrepSpurPickupSession.Observe(
            expectedThisSpurJobCars: 1,
            attachedThisSpurJobCars: 1,
            unattachedOnPrepSpur: 0,
            foreignFreightCars: 5);
        Assert.True(PrepSpurPickupSession.IsComplete);
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        PrepSpurPickupSession.Observe(
            expectedThisSpurJobCars: 1,
            attachedThisSpurJobCars: 1,
            unattachedOnPrepSpur: 0,
            foreignFreightCars: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
    }

    [Fact]
    public void Smoke_22_50_rolling_couple_then_rest_with_latched_knuckle_leaves_Prep()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(1, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S"),
                new SwitchListStep(2, SwitchListStepKind.Transit, "SW", "SW-B4L", "pull out"),
            });
        PrepSpurPickupSession.Clear();
        PrepCreepSession.LatchKnuckle();
        Assert.False(
            SwitchListSession.TryAdvanceOnCoupleSuccess(
                coupleSuccess: true,
                speedKmh: 2f,
                throttle01: 0f,
                motors: MotorStatus.Ok));
        Assert.Equal(
            SwitchListRunnerTelemetry.CoupleWaitRest,
            PrepCoupleExitGate.WaitAfterCoupleHold(
                atRest: false,
                spurPickupComplete: false,
                unattachedOnPrepSpur: 0));
        Assert.True(
            SwitchListSession.TryAdvanceOnCoupleSuccess(
                coupleSuccess: true,
                speedKmh: 0f,
                throttle01: 0f,
                motors: MotorStatus.Ok));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
    }

    /// <summary>
    /// Cab 22.41: motors Hot then Dead, then facing-prep F + ArmGo.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_22_41_no_facing_or_armgo_when_motors_dead()
    {
        var pullOut = new SwitchListStep(
            8,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Forward · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: false);
        Assert.False(MotorDisplay.AllowsGoWrites(MotorStatus.Dead));
        Assert.False(MotorDisplay.AllowsGoWrites(MotorStatus.Hot));
        Assert.True(MotorDisplay.AllowsGoWrites(MotorStatus.Ok));
        Assert.True(MotorDisplay.AllowsGoWrites(null));
        Assert.False(SwitchListStepPrereq.ShouldWriteFacingPostCouple(0f, 0f, MotorStatus.Dead));
        Assert.False(SwitchListStepPrereq.ShouldWriteFacingPostCouple(0f, 0f, MotorStatus.Hot));
        Assert.True(SwitchListStepPrereq.ShouldWriteFacingPostCouple(0f, 0f, MotorStatus.Ok));
        Assert.False(SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
            pullOut,
            holdAfterCouple: true,
            fullyStoppedAndIdle: true,
            reverserMatches: true,
            motorsHealthy: false));
        Assert.False(SwitchListYardChain.ShouldAutoArmGo(
            SwitchListRunMode.Manual,
            pullOut,
            inYardPrepScope: true,
            pinBlocksAlign: false,
            RouteClearancePhase.Idle,
            motorsHealthy: false));
        Assert.Equal(0f, PidSpeedTarget.ClampRequestForMotors(25f, MotorStatus.Dead));
        Assert.Equal(0f, PidSpeedTarget.ClampRequestForMotors(25f, MotorStatus.Hot));
        Assert.Equal(25f, PidSpeedTarget.ClampRequestForMotors(25f, MotorStatus.Ok));
    }

    /// <summary>
    /// Cab 2.16.14: the C4S past-switch is a move. Couple does not finish it.
    /// The following Prep finishes only when that spur's cars are latched,
    /// and that is the first time the next pin gets a direction.
    /// </summary>
    [Fact]
    public void Smoke_C4S_move_does_not_couple_prep_does()
    {
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-C4S",
                    "Set Forward · Past switch → SW-C4S until CLEARED"),
                new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Prep → SW-C4S"),
                new SwitchListStep(
                    8,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED"),
            });

        PrepSpurPickupSession.Observe(attachedJobCars: 1, unattachedOnPrepSpur: 0);
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.False(AutoCoupleAssist.StepAllowsCoupleAssist(
            switchListActive: true,
            SwitchListStepKind.Transit));

        Assert.True(SwitchListSession.TryAdvance());
        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        PrepSpurPickupSession.Observe(attachedJobCars: 1, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(2, SwitchListSession.CurrentIndex);
        Assert.Equal("SW-B4L", SwitchListSession.CurrentStep!.DestTrackId);
    }
}
