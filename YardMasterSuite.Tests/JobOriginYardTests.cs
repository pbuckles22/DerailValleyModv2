using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest 2026-08-24: SW-SU-72 at SW office showed Preview OUT;
/// SW-SL-55 at the same desk showed ~900 m. Wipe station is job-id origin, not dest.
/// </summary>
public class JobOriginYardTests
{
    [Fact]
    public void Smoke_sw_su_ticket_at_sw_office_uses_job_id_origin_not_chain_dest()
    {
        Assert.Equal("SW", JobOriginYard.FromJobId("SW-SU-72"));
        Assert.Equal("SW", JobOriginYard.FromJobId("SW-SL-55"));
        Assert.Equal("SW", JobOriginYard.Resolve("SW-SU-72", chainOriginYardId: "SU"));
        Assert.Equal("SW", JobOriginYard.Resolve("SW-SL-55", chainOriginYardId: "SW"));
    }

    [Fact]
    public void Resolve_falls_back_to_chain_when_id_has_no_origin()
    {
        Assert.Equal("SU", JobOriginYard.Resolve(null, "SU"));
        Assert.Equal("SU", JobOriginYard.Resolve("  ", "SU"));
        Assert.Null(JobOriginYard.FromJobId(null));
        Assert.Null(JobOriginYard.FromJobId("NOSPLIT"));
    }
}
