using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// HTP CP4 — 7.4 couple success during Prep auto-advances the Switch List (**13.2.1**).
/// </summary>
[Collection("StaticSessions")]
public class HtpCoupleAutoAdvanceCp4Tests
{
    public HtpCoupleAutoAdvanceCp4Tests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_2_1_prep_couple_success_advances_step()
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

        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(coupleSuccess: true));
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Transit, SwitchListSession.CurrentStep!.Kind);
        Assert.Equal("T2 switch-list: couple-next", SwitchListRunnerTelemetry.CoupleNext);
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
            coupleSuccess: true));

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
    }

    /// <summary>
    /// Cab 2.13.2.5.2: couple-next cleared the Prep hold, yard-chain ArmGo
    /// shoved Reverse into a second cut. Stay stopped on the pull-out row.
    /// </summary>
    [Fact]
    public void Smoke_13_2_5_1_prep_couple_next_keeps_hold_so_ArmGo_does_not_shove()
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
        PrepCreepSession.LatchCoupleHold();
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
        PrepCreepSession.Observe(clearanceMeters: 0.4f, speedKmh: 0f, mechanicallyCoupled: true);
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
    public void Smoke_13_2_1_couple_gate_is_prep_plus_next_allowed()
    {
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.HumanHold,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Manual,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.HumanHold,
            hasNextStep: true,
            coupleSuccess: false));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.HumanHold,
            hasNextStep: false,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Transit,
            SwitchListRunMode.Manual,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.True(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Go,
            hasNextStep: true,
            coupleSuccess: true));
        Assert.False(SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
            SwitchListStepKind.Prep,
            SwitchListRunMode.Manual,
            hasNextStep: true,
            coupleSuccess: true,
            pickupComplete: false));
    }

    [Fact]
    public void Smoke_walk_back_onto_consist_does_not_skip_C4S_prep_off_spur()
    {
        // Player.log 22.23: step 7 Prep C4S, cars 1→3 after walking A1P / S961,
        // couple-next skipped to step 8 without picking C4S.
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(7, SwitchListStepKind.Prep, "SW", "SW-C4S", "Set Reverse · Prep → SW-C4S"),
                new SwitchListStep(
                    8,
                    SwitchListStepKind.Transit,
                    "SW",
                    "SW-B4L",
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
            });
        Assert.True(SwitchListRunner.ConsistGrewIntoCouple(1, 3));
        Assert.False(PrepSpurPickup.TrackIsPrepSpur("#Y-#S961#T", "SW-C4S"));
        Assert.False(PrepSpurPickup.TrackIsPrepSpur("SW-A1P", "SW-C4S"));
        Assert.True(PrepSpurPickup.TrackIsPrepSpur("SW-C4S", "SW-C4S"));
        Assert.False(SwitchListSession.TryAdvanceOnCoupleSuccess(
            coupleSuccess: true,
            onCurrentPrepDest: true,
            pickupComplete: false));
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal("SW-C4S", SwitchListSession.CurrentStep!.DestTrackId);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(
            coupleSuccess: true,
            onCurrentPrepDest: true,
            pickupComplete: true));
        Assert.Equal("SW-B4L", SwitchListSession.CurrentStep!.DestTrackId);
    }

    [Fact]
    public void Smoke_22_5_kiss_prep_consist_jump_couple_next_even_while_Go()
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
        Assert.True(SwitchListRunner.ConsistGrewIntoCouple(1, 3));
        Assert.True(SwitchListRunner.ConsistLengthJumped(7f, 44f));
        Assert.False(
            SwitchListRunner.IsPrepCoupleSuccess(
                autocoupleHook: false,
                mechanicallyCoupled: false,
                consistGrew: true,
                pickupComplete: false));
        Assert.True(
            SwitchListRunner.IsPrepCoupleSuccess(
                autocoupleHook: false,
                mechanicallyCoupled: false,
                consistGrew: true,
                pickupComplete: true));
        PrepSpurPickupSession.Observe(attachedJobCars: 2, unattachedOnPrepSpur: 0);
        Assert.True(SwitchListSession.TryAdvanceOnCoupleSuccess(
            coupleSuccess: true,
            pickupComplete: true));
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.Equal(1, SwitchListSession.CurrentIndex);
        Assert.Equal("SW-B4L", SwitchListSession.CurrentStep!.DestTrackId);
        Assert.False(SwitchListSession.CurrentStep.BindNeedsReverse);
        Assert.Equal("T2 switch-list: couple-next", SwitchListRunnerTelemetry.CoupleNext);
        Assert.True(PrepCreepSession.HoldAfterCoupleStop);
    }
}
