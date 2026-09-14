using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — inventory identity on pickup/swap/drop (live cars poll separately).</summary>
public class JobCarArScanPolicyTests
{
    [Fact]
    public void Decide_empty_stays_keep()
    {
        Assert.Equal(
            JobCarArScanReason.Keep,
            JobCarArScanPolicy.Decide(null, null));
    }

    [Fact]
    public void Decide_pickup_scans()
    {
        Assert.Equal(
            JobCarArScanReason.Scan,
            JobCarArScanPolicy.Decide(null, "MF-SL-1"));
    }

    [Fact]
    public void Decide_same_job_keeps()
    {
        Assert.Equal(
            JobCarArScanReason.Keep,
            JobCarArScanPolicy.Decide("MF-SL-1", "MF-SL-1"));
    }

    [Fact]
    public void Decide_swap_scans()
    {
        Assert.Equal(
            JobCarArScanReason.Scan,
            JobCarArScanPolicy.Decide("MF-SL-1", "HB-FH-2"));
    }

    [Fact]
    public void Decide_drop_clears()
    {
        Assert.Equal(
            JobCarArScanReason.Clear,
            JobCarArScanPolicy.Decide("MF-SL-1", null));
    }

    [Fact]
    public void Smoke_hold_without_take_scans_bound_switch_list_job()
    {
        Assert.Equal("SW-SL-55", JobCarArScanPolicy.ResolveScanJobId(null, "SW-SL-55"));
        Assert.Equal("SW-FH-82", JobCarArScanPolicy.ResolveScanJobId("SW-FH-82", "SW-SL-55"));
        Assert.Null(JobCarArScanPolicy.ResolveScanJobId(null, null));
        Assert.Equal(
            JobCarArScanReason.Scan,
            JobCarArScanPolicy.Decide(null, JobCarArScanPolicy.ResolveScanJobId(null, "SW-SL-55")));
    }

    [Fact]
    public void Decide_does_not_allocate()
    {
        JobCarArScanPolicy.Decide(null, "SW-SU-72");
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            JobCarArScanPolicy.Decide("SW-SU-72", "SW-SU-72");
            JobCarArScanPolicy.Decide("SW-SU-72", null);
            JobCarArScanPolicy.Decide(null, "SW-SU-72");
            JobCarArScanPolicy.ResolveScanJobId(null, "SW-SL-55");
            JobCarArScanPolicy.ResolveScanJobId("SW-FH-82", "SW-SL-55");
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
