using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 22.45: Prep GO is 25 until close, then 3 until knuckle.
/// </summary>
[Collection("StaticSessions")]
public class PrepCoupleExitGateTests
{
    public PrepCoupleExitGateTests() => YmsRouteSessions.ClearAll();
    private static SwitchListStep Prep() =>
        new(3, SwitchListStepKind.Prep, "SW", "SW-B1S", "Set Reverse · Prep → SW-B1S");

    [Fact]
    public void Smoke_prep_far_from_cars_requests_yard_cruise_not_3()
    {
        var prep = Prep();
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, 300f, null, null, null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForStep(prep));
    }

    [Fact]
    public void Smoke_prep_rear_in_couple_band_requests_3()
    {
        Assert.Equal(
            PrepCreepPolicy.CreepRequestKmh,
            PidSpeedTarget.RequestForYardStep(Prep(), 12f, 10f, null, null));
    }

    [Fact]
    public void Smoke_prep_does_not_kiss_stop_at_3m_when_rear_3()
    {
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                Prep(),
                remToAimMeters: 3f,
                speedKmh: 3f));
    }

    [Fact]
    public void Smoke_13_2_5_22_43_prep_cruises_3_until_knuckle_not_kiss_rem()
    {
        var prep = Prep();
        Assert.Equal(3f, PrepCreepPolicy.CreepRequestKmh);
        Assert.Equal(
            PrepCreepPolicy.CreepRequestKmh,
            PidSpeedTarget.RequestForYardStep(prep, 80f, 3.1f, null, null));
        Assert.Equal(
            PrepCreepPolicy.CreepRequestKmh,
            PidSpeedTarget.RequestForYardStep(prep, 80f, 1.8f, null, null));
        Assert.Equal(
            PrepCreepPolicy.CreepRequestKmh,
            PidSpeedTarget.RequestForYardStep(prep, 40f, 1.5f, null, null));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, prep, 8f, 3f));
        Assert.False(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 3.1f,
                speedKmh: 3f,
                mechanicallyCoupled: false));
        Assert.False(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 1.8f,
                speedKmh: 0f,
                mechanicallyCoupled: false));
        Assert.False(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 1.5f,
                speedKmh: 3f,
                mechanicallyCoupled: false));
        Assert.True(
            PrepCreepPolicy.ShouldStopGoForCouple(
                SwitchListRunMode.Go,
                prep,
                clearanceMeters: 0.4f,
                speedKmh: 3f,
                mechanicallyCoupled: false));
        Assert.False(PrepCoupleExitGate.InTouchWindow(3.1f));
        Assert.False(PrepCoupleExitGate.InTouchWindow(1.5f));
        Assert.True(PrepCoupleExitGate.InTouchWindow(0.4f));
        Assert.True(
            PrepCoupleExitGate.ShouldStopGoAfterKnuckle(
                SwitchListRunMode.Go,
                prep,
                mechanicallyCoupled: true,
                spurPickupComplete: false,
                unattachedOnPrepSpur: 0));
        Assert.True(
            PrepCoupleExitGate.ShouldStopGoAfterKnuckle(
                SwitchListRunMode.Go,
                prep,
                mechanicallyCoupled: true,
                spurPickupComplete: false,
                unattachedOnPrepSpur: 3));
    }

    [Fact]
    public void Smoke_13_2_5_22_43_next_after_knuckle_rest_idle_motors()
    {
        Assert.False(PrepCoupleExitGate.ReadyToNext(
            SwitchListStepKind.Prep,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: true,
            speedKmh: 3f,
            throttle01: 0f,
            motors: MotorStatus.Ok));
        Assert.False(PrepCoupleExitGate.ReadyToNext(
            SwitchListStepKind.Prep,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: true,
            speedKmh: 0f,
            throttle01: 0.2f,
            motors: MotorStatus.Ok));
        Assert.False(PrepCoupleExitGate.ReadyToNext(
            SwitchListStepKind.Prep,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: true,
            speedKmh: 0f,
            throttle01: 0f,
            motors: MotorStatus.Dead));
        Assert.False(PrepCoupleExitGate.ReadyToNext(
            SwitchListStepKind.Prep,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: false,
            speedKmh: 0f,
            throttle01: 0f,
            motors: MotorStatus.Ok));
        Assert.True(PrepCoupleExitGate.ReadyToNext(
            SwitchListStepKind.Prep,
            hasNextStep: true,
            coupleSuccess: true,
            spurPickupComplete: false,
            speedKmh: 0f,
            throttle01: 0f,
            motors: MotorStatus.Ok,
            tipCoupled: true,
            unattachedOnPrepSpur: 0));
    }

    [Fact]
    public void Smoke_22_50_rolling_couple_wait_is_rest_not_spur_when_quota_unknown()
    {
        Assert.Equal(
            SwitchListRunnerTelemetry.CoupleWaitRest,
            PrepCoupleExitGate.WaitAfterCoupleHold(
                atRest: false,
                spurPickupComplete: false,
                unattachedOnPrepSpur: 0));
        Assert.Equal(
            SwitchListRunnerTelemetry.CoupleWaitSpur,
            PrepCoupleExitGate.WaitAfterCoupleHold(
                atRest: true,
                spurPickupComplete: false,
                unattachedOnPrepSpur: 3));
        Assert.Equal(
            SwitchListRunnerTelemetry.CoupleWaitRest,
            PrepCoupleExitGate.WaitAfterCoupleHold(
                atRest: true,
                spurPickupComplete: true,
                unattachedOnPrepSpur: 0));
    }

    [Fact]
    public void Smoke_22_50_kiss_neutral_rev_latches_knuckle_so_stop_can_leave_Prep()
    {
        PrepCreepSession.Clear();
        PrepSpurPickupSession.Clear();
        Assert.False(PrepCreepSession.TipCoupled);
        PrepCreepSession.LatchKnuckle();
        PrepCreepSession.Observe(null, 0f, mechanicallyCoupled: false, spurPickupComplete: false);
        Assert.True(PrepCreepSession.TipCoupled);
        Assert.True(
            PrepCoupleExitGate.ReadyToNext(
                SwitchListStepKind.Prep,
                hasNextStep: true,
                coupleSuccess: true,
                spurPickupComplete: false,
                speedKmh: 0f,
                throttle01: 0f,
                motors: MotorStatus.Ok,
                tipCoupled: PrepCreepSession.TipCoupled,
                unattachedOnPrepSpur: 0));
        PrepCreepSession.Clear();
    }

    [Fact]
    public void Smoke_C4S_packed_spur_holds_next_while_foreign_freight_on_hook()
    {
        Assert.False(
            PrepCoupleExitGate.ReadyToNext(
                SwitchListStepKind.Prep,
                hasNextStep: true,
                coupleSuccess: true,
                spurPickupComplete: true,
                speedKmh: 0f,
                throttle01: 0f,
                motors: MotorStatus.Ok,
                tipCoupled: true,
                unattachedOnPrepSpur: 0,
                foreignFreightOnConsist: 5));
        Assert.Equal(
            SwitchListRunnerTelemetry.CoupleWaitForeign,
            PrepCoupleExitGate.WaitAfterCoupleHold(
                atRest: true,
                spurPickupComplete: true,
                unattachedOnPrepSpur: 0,
                foreignFreightOnConsist: 5));
        Assert.True(
            PrepCoupleExitGate.ReadyToNext(
                SwitchListStepKind.Prep,
                hasNextStep: true,
                coupleSuccess: true,
                spurPickupComplete: true,
                speedKmh: 0f,
                throttle01: 0f,
                motors: MotorStatus.Ok,
                tipCoupled: true,
                unattachedOnPrepSpur: 0,
                foreignFreightOnConsist: 0));
    }

    /// <summary>
    /// Cab 2.16.23: first Prep grew 1→3 cars, logged couple-hold, then
    /// yard-req v=25 with the laser gone and reverse throttle ran to 100.
    /// </summary>
    [Fact]
    public void Smoke_prep_grow_arms_stop_go_and_hold_requests_0_not_25()
    {
        var prep = Prep();
        Assert.True(PrepCoupleExitGate.ShouldLatchHoldOnConsistGrow(
            SwitchListStepKind.Prep,
            fromCarCount: 1,
            toCarCount: 3));
        SwitchListSession.Bind("SW-SL-55", new[] { prep });
        Assert.Equal(
            SwitchListRunnerResult.Ok,
            SwitchListRunnerSession.TrySetGo(
                SwitchListSession.CurrentStep,
                hasPlan: true,
                pinForAlign: false,
                RouteClearancePhase.Idle));
        Assert.Equal(SwitchListRunMode.Go, SwitchListRunnerSession.Mode);
        Assert.False(PrepCreepSession.WantsCoupleStop);
        PrepCreepSession.LatchCoupleHold();
        Assert.True(PrepCreepSession.TryStopGoIfNeeded(SwitchListSession.CurrentStep));
        Assert.Equal(SwitchListRunMode.Manual, SwitchListRunnerSession.Mode);
        Assert.Equal(
            0f,
            YardKissPolicy.RequestKmh(prep, holdAfterCouple: true));
        Assert.Equal(
            0f,
            PidSpeedTarget.RequestForYardStep(
                prep,
                null,
                null,
                null,
                null,
                holdAfterCouple: true));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            YardKissPolicy.RequestKmh(prep));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, null, null, null, null));
    }
}
