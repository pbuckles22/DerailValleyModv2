using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class PrepSpurPickupTests
{
    public PrepSpurPickupTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_2_5_22_2_B1S_job_cars_attached_completes_Prep_even_if_tip_open()
    {
        Assert.False(PrepSpurPickup.IsComplete(attachedThisJobCars: 0, remainingThisJobOnThisPrep: 5));
        Assert.False(PrepSpurPickup.IsComplete(attachedThisJobCars: 3, remainingThisJobOnThisPrep: 2));
        Assert.False(PrepSpurPickup.IsComplete(attachedThisJobCars: 0, remainingThisJobOnThisPrep: 0));
        Assert.True(PrepSpurPickup.IsComplete(attachedThisJobCars: 5, remainingThisJobOnThisPrep: 0));
        Assert.True(PrepSpurPickup.TrackIsPrepSpur("SW-B1S", "SW-B1S"));
        Assert.True(PrepSpurPickup.TrackIsPrepSpur("B1S", "SW-B1S"));
        Assert.False(PrepSpurPickup.TrackIsPrepSpur("SW-C4S", "SW-B1S"));
    }

    [Fact]
    public void Smoke_13_2_5_22_2_C4S_unattached_does_not_complete_B1S_Prep()
    {
        Assert.True(PrepSpurPickup.IsComplete(attachedThisJobCars: 5, remainingThisJobOnThisPrep: 0));
        Assert.False(PrepSpurPickup.IsComplete(attachedThisJobCars: 5, remainingThisJobOnThisPrep: 3));
    }

    [Fact]
    public void Smoke_22_48_other_job_empties_on_lane_do_not_count_as_this_job_remaining()
    {
        Assert.False(
            PrepSpurPickup.CountsAsRemainingThisPrep(
                isThisJobsCar: false,
                standingOnThisPrepSpur: true,
                alreadyAttached: false));
        Assert.True(
            PrepSpurPickup.CountsAsRemainingThisPrep(
                isThisJobsCar: true,
                standingOnThisPrepSpur: true,
                alreadyAttached: false));
        Assert.False(
            PrepSpurPickup.CountsAsRemainingThisPrep(
                isThisJobsCar: true,
                standingOnThisPrepSpur: true,
                alreadyAttached: true));
        Assert.True(PrepSpurPickup.IsComplete(attachedThisJobCars: 2, remainingThisJobOnThisPrep: 0));
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
                unattachedOnPrepSpur: 0));
    }

    [Fact]
    public void Smoke_SL55_two_spur_quota_B1S_complete_does_not_wait_for_C4S_cars()
    {
        Assert.True(
            PrepSpurPickup.IsComplete(
                expectedThisSpurJobCars: 2,
                attachedThisSpurJobCars: 2,
                remainingThisJobOnThisPrep: 0));
        Assert.False(
            PrepSpurPickup.IsComplete(
                expectedThisSpurJobCars: 2,
                attachedThisSpurJobCars: 1,
                remainingThisJobOnThisPrep: 1));
        Assert.False(
            PrepSpurPickup.IsComplete(
                expectedThisSpurJobCars: 2,
                attachedThisSpurJobCars: 2,
                remainingThisJobOnThisPrep: 1));
        Assert.False(
            PrepSpurPickup.CountsAsRemainingThisPrep(
                isThisJobsCar: true,
                standingOnThisPrepSpur: true,
                alreadyAttached: false,
                assignedToThisSpur: false));
        PrepSpurPickupSession.Observe(
            expectedThisSpurJobCars: 3,
            attachedThisSpurJobCars: 3,
            unattachedOnPrepSpur: 0);
        Assert.True(PrepSpurPickupSession.IsComplete);
        Assert.Equal(3, PrepSpurPickupSession.ExpectedThisSpurJobCars);
        PrepSpurPickupSession.Clear();
    }

    [Fact]
    public void Smoke_tag_first_spur_count_verify_each_car_to_job_label()
    {
        Assert.True(PrepSpurPickup.CarVerifiesToTag("SW-SL-55", "SW-SL-55"));
        Assert.False(PrepSpurPickup.CarVerifiesToTag("SW-FH-82", "SW-SL-55"));
        Assert.False(PrepSpurPickup.CarVerifiesToTag(null, "SW-SL-55"));
        Assert.True(
            PrepSpurPickup.CountsAsThisSpurNeed(
                tagVerified: true,
                standingOnThisPrepSpur: true,
                attached: false,
                taskStartsOnThisPrep: false));
        Assert.False(
            PrepSpurPickup.CountsAsThisSpurNeed(
                tagVerified: false,
                standingOnThisPrepSpur: true,
                attached: false,
                taskStartsOnThisPrep: true));
        Assert.True(
            PrepSpurPickup.CountsAsThisSpurHave(
                tagVerified: true,
                attached: true,
                standingOnThisPrepSpur: false,
                taskStartsOnThisPrep: true));
        Assert.False(
            PrepSpurPickup.CountsAsThisSpurHave(
                tagVerified: false,
                attached: true,
                standingOnThisPrepSpur: true,
                taskStartsOnThisPrep: true));
        Assert.False(
            PrepSpurPickup.CountsAsRemainingThisPrep(
                isThisJobsCar: true,
                standingOnThisPrepSpur: true,
                alreadyAttached: false,
                assignedToThisSpur: true,
                tagVerified: false));
        Assert.True(
            PrepSpurPickup.IsComplete(
                expectedThisSpurJobCars: 2,
                attachedThisSpurJobCars: 2,
                remainingThisJobOnThisPrep: 0));
    }
}
