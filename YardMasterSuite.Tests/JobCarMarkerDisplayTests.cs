using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class JobCarMarkerDisplayTests
{
    [Fact]
    public void ShortSpurLabel_strips_yard_prefix()
    {
        Assert.Equal("C2S", JobCarMarkerDisplay.ShortSpurLabel("MF-C2S"));
        Assert.Equal("B6S", JobCarMarkerDisplay.ShortSpurLabel(" MF-B6S "));
        Assert.Equal("C2S", JobCarMarkerDisplay.ShortSpurLabel("C2S"));
        Assert.Equal("#Y-12", JobCarMarkerDisplay.ShortSpurLabel("#Y-12"));
        Assert.Null(JobCarMarkerDisplay.ShortSpurLabel(null));
        Assert.Null(JobCarMarkerDisplay.ShortSpurLabel("  "));
    }

    [Fact]
    public void FormatCaption_job_track_and_count()
    {
        Assert.Equal(
            "MF-SL-35 · C2S · 3 142m",
            JobCarMarkerDisplay.FormatCaption("MF-SL-35", "C2S", 3, 142.4f));
        Assert.Equal("SW-FH-65 · 4 13m", JobCarMarkerDisplay.FormatCaption("SW-FH-65", 4, 13.2f));
        Assert.Equal("C2S · 2 40m", JobCarMarkerDisplay.FormatCaption(null, "C2S", 2, 40f));
        Assert.Equal("4 cars 40m", JobCarMarkerDisplay.FormatCaption(null, 4, 40f));
    }

    [Theory]
    [InlineData(true, JobConsistStatus.Ready, 4, false)]
    [InlineData(true, JobConsistStatus.Hold, 4, true)]
    [InlineData(true, JobConsistStatus.Missing, 4, true)]
    [InlineData(true, JobConsistStatus.Ready, 0, false)]
    [InlineData(false, JobConsistStatus.Missing, 4, true)]
    [InlineData(false, JobConsistStatus.Ready, 4, true)]
    public void ShouldShowAr_hides_when_taken_and_ready(
        bool jobTaken,
        JobConsistStatus status,
        int expectedCars,
        bool want)
    {
        Assert.Equal(want, JobCarMarkerDisplay.ShouldShowAr(jobTaken, status, expectedCars));
    }

    [Fact]
    public void Smoke_taken_job_yard_shows_purple_job_car_ar()
    {
        Assert.True(
            JobCarMarkerDisplay.ShouldShowAr(true, JobConsistStatus.Missing, 3));
        Assert.Equal(ArWaypointKind.JobCar, (ArWaypointKind)4);
        Assert.Equal("JC", ArMarkerDisplay.Glyph(ArWaypointKind.JobCar));
        Assert.True(ArMarkerDisplay.IsImguiFontSafe(ArMarkerDisplay.Glyph(ArWaypointKind.JobCar)));
        Assert.Equal(
            "SW-SU-72 · C2S · 3 910m",
            JobCarMarkerDisplay.FormatCaption("SW-SU-72", "C2S", 3, 910f));
    }

    [Fact]
    public void Smoke_no_job_in_hand_hides_job_car_ar()
    {
        Assert.False(JobCarMarkerDisplay.ShouldShowAr(false, JobConsistStatus.Missing, 0));
        Assert.Equal(JobCarArScanReason.Clear, JobCarArScanPolicy.Decide("SW-SU-72", null));
    }

    [Fact]
    public void Smoke_taken_go_hides_job_car_ar()
    {
        Assert.False(
            JobCarMarkerDisplay.ShouldShowAr(true, JobConsistStatus.Ready, 4));
    }

    [Fact]
    public void ShouldShowAr_does_not_allocate()
    {
        JobCarMarkerDisplay.ShouldShowAr(true, JobConsistStatus.Hold, 4);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            JobCarMarkerDisplay.ShouldShowAr(true, JobConsistStatus.Missing, 4);
            JobCarMarkerDisplay.ShouldShowAr(true, JobConsistStatus.Ready, 4);
            JobCarMarkerDisplay.ShouldShowAr(false, JobConsistStatus.Missing, 4);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
