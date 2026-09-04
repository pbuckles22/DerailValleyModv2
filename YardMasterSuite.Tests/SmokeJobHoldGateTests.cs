using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SmokeJobHoldGateTests
{
    [Fact]
    public void Smoke_13_4_prefers_fh_then_sw_for_bootstrap()
    {
        Assert.Equal(
            1,
            SmokeJobHoldGate.PickPreferredIndex(new[] { "HB-FH-01", "SW-FH-82", "SW-SU-10" }));
        Assert.Equal(
            1,
            SmokeJobHoldGate.PickPreferredIndex(new[] { "SW-SU-10", "CS-FH-1" }));
        Assert.Equal(0, SmokeJobHoldGate.PickPreferredIndex(new[] { "CS-SL-1" }));
        Assert.Equal(-1, SmokeJobHoldGate.PickPreferredIndex(System.Array.Empty<string?>()));
    }

    [Fact]
    public void Format_lines_name_the_job()
    {
        Assert.Equal("T2 smoke-job: taken job=SW-FH-82", SmokeJobHoldGate.FormatTaken("SW-FH-82"));
        Assert.Equal("T2 smoke-job: wait available", SmokeJobHoldGate.FormatWait());
        Assert.Equal("T2 smoke-job: hold job=SW-FH-82", SmokeJobHoldGate.FormatHeld("SW-FH-82"));
        Assert.True(SmokeJobHoldGate.Enabled);
    }
}
