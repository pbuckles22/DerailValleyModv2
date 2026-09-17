using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 22.45: Prep GO is 25 until close, then 3 until knuckle.
/// </summary>
public class PrepCoupleExitGateTests
{
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
            PidSpeedTarget.RequestForYardStep(Prep(), 12f, 15f, null, null));
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
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, prep, 3.1f, 25f));
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
        Assert.False(
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
}
