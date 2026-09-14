using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PrepSpurPickupTests
{
    [Fact]
    public void Smoke_13_2_5_22_2_B1S_job_cars_attached_completes_Prep_even_if_tip_open()
    {
        Assert.False(PrepSpurPickup.IsComplete(attachedJobCars: 0, unattachedOnPrepSpur: 5));
        Assert.False(PrepSpurPickup.IsComplete(attachedJobCars: 3, unattachedOnPrepSpur: 2));
        Assert.False(PrepSpurPickup.IsComplete(attachedJobCars: 0, unattachedOnPrepSpur: 0));
        Assert.True(PrepSpurPickup.IsComplete(attachedJobCars: 5, unattachedOnPrepSpur: 0));
        Assert.True(PrepSpurPickup.TrackIsPrepSpur("SW-B1S", "SW-B1S"));
        Assert.True(PrepSpurPickup.TrackIsPrepSpur("B1S", "SW-B1S"));
        Assert.False(PrepSpurPickup.TrackIsPrepSpur("SW-C4S", "SW-B1S"));
    }

    [Fact]
    public void Smoke_13_2_5_22_2_C4S_unattached_does_not_complete_B1S_Prep()
    {
        Assert.True(PrepSpurPickup.IsComplete(attachedJobCars: 5, unattachedOnPrepSpur: 0));
        Assert.False(PrepSpurPickup.IsComplete(attachedJobCars: 5, unattachedOnPrepSpur: 3));
    }

    [Fact]
    public void Smoke_SL55_B1S_cars_on_hook_do_not_complete_C4S_prep()
    {
        // Cab: #5 knuckled B1S, #6 CLEARED, #7 Prep C4S must wait for C4S
        // knuckles. Leftover attached-all=5 + unattached C4S unseen=0 is not exit.
        Assert.True(SwitchListStepExit.PrepKnuckleComplete(5, 0));
        Assert.False(SwitchListStepExit.PrepKnuckleComplete(0, 2));
        Assert.False(SwitchListStepExit.PrepKnuckleComplete(0, 0));
        Assert.True(SwitchListStepExit.PrepKnuckleComplete(2, 0));
        Assert.False(
            SwitchListStepExit.AllowsAutoNextPrep(
                SwitchListStepKind.Prep,
                hasNextStep: true,
                pickupComplete: false));
        Assert.True(
            SwitchListStepExit.AllowsAutoNextPrep(
                SwitchListStepKind.Prep,
                hasNextStep: true,
                pickupComplete: true));
        Assert.False(
            SwitchListStepExit.AllowsAutoNextPrep(
                SwitchListStepKind.Transit,
                hasNextStep: true,
                pickupComplete: true));
    }

    [Fact]
    public void Smoke_SL55_step5_knuckle_latches_even_after_cars_leave_B1S_track()
    {
        // Player.log 22.25: cars=3, autocouple done, handbrake-release, no
        // couple-next. DV reparents hoppers off B1S as soon as they join.
        Assert.True(
            PrepSpurPickup.ShouldLatchNewlyAttached(
                attached: true,
                wasAttachedAtPrepEnter: false));
        Assert.False(
            PrepSpurPickup.ShouldLatchNewlyAttached(
                attached: true,
                wasAttachedAtPrepEnter: true));
        Assert.False(
            PrepSpurPickup.ShouldLatchNewlyAttached(
                attached: false,
                wasAttachedAtPrepEnter: false));
        Assert.True(SwitchListStepExit.PrepKnuckleComplete(2, 0));
        Assert.Equal(
            "T2 switch-list: prep-wait knuckle attached=0 unattached=0",
            SwitchListRunnerTelemetry.FormatPrepWaitKnuckle(0, 0));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            PrepSpurPickup.ShouldLatchNewlyAttached(true, false);
            PrepSpurPickup.ShouldLatchNewlyAttached(true, true);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
