using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.21): discrete T2 job-car-ar lines on pickup / drop / GO hide.
/// </summary>
public class JobCarTelemetryTests
{
    [Fact]
    public void Smoke_take_job_emits_T2_job_car_ar_scan()
    {
        Assert.Equal(
            "T2 job-car-ar: scan job=SW-SU-72 taken=1 n=2",
            JobCarTelemetry.FormatScan("SW-SU-72", jobTaken: true, count: 2));
    }

    [Fact]
    public void Smoke_preview_ticket_scan_is_not_taken()
    {
        Assert.Equal(
            "T2 job-car-ar: scan job=SW-SU-72 taken=0 n=1",
            JobCarTelemetry.FormatScan("SW-SU-72", jobTaken: false, count: 1));
    }

    [Fact]
    public void Smoke_no_job_in_hand_emits_T2_job_car_ar_clear()
    {
        Assert.Equal(
            "T2 job-car-ar: clear (no job in hand)",
            JobCarTelemetry.FormatClear());
    }

    [Fact]
    public void Smoke_taken_go_emits_T2_job_car_ar_hide()
    {
        Assert.Equal(
            "T2 job-car-ar: hide job=SW-SU-72 reason=ready",
            JobCarTelemetry.FormatHide("SW-SU-72"));
    }
}
