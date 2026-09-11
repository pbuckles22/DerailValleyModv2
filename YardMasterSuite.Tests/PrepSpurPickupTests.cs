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
}
