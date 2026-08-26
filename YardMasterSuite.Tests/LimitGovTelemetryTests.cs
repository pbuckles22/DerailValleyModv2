using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.5 discrete T2 limit-gov lines (no per-tick spam).</summary>
public class LimitGovTelemetryTests
{
    [Fact]
    public void Smoke_overspeed_emits_soft_cap()
    {
        var cache = default(LimitGovLogCache);
        Assert.Equal(
            LimitGovTelemetry.SoftCap,
            LimitGovTelemetry.NextLog(
                applied: true,
                ThreeGateAbortReason.None,
                limitRounded: 60,
                ref cache));
    }

    [Fact]
    public void Smoke_repeat_soft_cap_is_silent()
    {
        var cache = default(LimitGovLogCache);
        Assert.NotNull(LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache));
        Assert.Null(LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache));
    }

    [Fact]
    public void Smoke_board_change_while_capping_emits_again()
    {
        var cache = default(LimitGovLogCache);
        Assert.Equal(
            LimitGovTelemetry.SoftCap,
            LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache));
        Assert.Equal(
            LimitGovTelemetry.SoftCap,
            LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 40, ref cache));
    }

    [Fact]
    public void Smoke_cap_release_when_under_limit()
    {
        var cache = default(LimitGovLogCache);
        Assert.NotNull(LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache));
        Assert.Equal(
            LimitGovTelemetry.CapRelease,
            LimitGovTelemetry.NextLog(false, ThreeGateAbortReason.Safety, 60, ref cache));
    }

    [Fact]
    public void Smoke_integrity_abort_after_cap_emits_abort()
    {
        var cache = default(LimitGovLogCache);
        Assert.NotNull(LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache));
        Assert.Equal(
            LimitGovTelemetry.AbortIntegrity,
            LimitGovTelemetry.NextLog(false, ThreeGateAbortReason.Integrity, 60, ref cache));
    }

    [Fact]
    public void Repeat_release_is_silent()
    {
        var cache = default(LimitGovLogCache);
        LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache);
        LimitGovTelemetry.NextLog(false, ThreeGateAbortReason.Safety, 60, ref cache);
        Assert.Null(LimitGovTelemetry.NextLog(false, ThreeGateAbortReason.Safety, 60, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_cap_holds()
    {
        var cache = default(LimitGovLogCache);
        LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            LimitGovTelemetry.NextLog(true, ThreeGateAbortReason.None, 60, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
