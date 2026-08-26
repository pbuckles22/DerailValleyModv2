using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.4 discrete T2 autocouple lines (no per-tick spam).</summary>
public class AutoCoupleTelemetryTests
{
    [Fact]
    public void Smoke_in_scan_emits_couple()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.Equal(
            AutoCoupleTelemetry.Couple,
            AutoCoupleTelemetry.NextLog(
                applied: true,
                linkComplete: false,
                AutoCoupleAction.Couple,
                ThreeGateAbortReason.None,
                ref cache));
    }

    [Fact]
    public void Smoke_repeat_couple_is_silent()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.NotNull(AutoCoupleTelemetry.NextLog(
            true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache));
        Assert.Null(AutoCoupleTelemetry.NextLog(
            true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache));
    }

    [Fact]
    public void Smoke_link_complete_emits_done()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.NotNull(AutoCoupleTelemetry.NextLog(
            true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoCoupleTelemetry.Done,
            AutoCoupleTelemetry.NextLog(
                applied: true,
                linkComplete: true,
                AutoCoupleAction.Couple,
                ThreeGateAbortReason.None,
                ref cache));
    }

    [Fact]
    public void Smoke_loose_finish_emits_finish_then_done()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.Equal(
            AutoCoupleTelemetry.Finish,
            AutoCoupleTelemetry.NextLog(
                true, false, AutoCoupleAction.Finish, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoCoupleTelemetry.Done,
            AutoCoupleTelemetry.NextLog(
                true, true, AutoCoupleAction.Finish, ThreeGateAbortReason.None, ref cache));
    }

    [Fact]
    public void Smoke_integrity_abort_after_couple_emits_abort()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.NotNull(AutoCoupleTelemetry.NextLog(
            true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoCoupleTelemetry.AbortIntegrity,
            AutoCoupleTelemetry.NextLog(
                applied: false,
                linkComplete: false,
                AutoCoupleAction.None,
                ThreeGateAbortReason.Integrity,
                ref cache));
    }

    [Fact]
    public void Idle_ticks_are_silent()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.Null(AutoCoupleTelemetry.NextLog(
            false, false, AutoCoupleAction.None, ThreeGateAbortReason.Safety, ref cache));
    }

    [Fact]
    public void Smoke_done_on_later_tick_without_write()
    {
        var cache = default(AutoCoupleLogCache);
        Assert.NotNull(AutoCoupleTelemetry.NextLog(
            true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoCoupleTelemetry.Done,
            AutoCoupleTelemetry.NextLog(
                applied: false,
                linkComplete: true,
                AutoCoupleAction.None,
                ThreeGateAbortReason.None,
                ref cache));
    }

    [Fact]
    public void Repeat_done_is_silent()
    {
        var cache = default(AutoCoupleLogCache);
        AutoCoupleTelemetry.NextLog(true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache);
        AutoCoupleTelemetry.NextLog(true, true, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache);
        Assert.Null(AutoCoupleTelemetry.NextLog(
            false, true, AutoCoupleAction.None, ThreeGateAbortReason.None, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_couple_holds()
    {
        var cache = default(AutoCoupleLogCache);
        AutoCoupleTelemetry.NextLog(true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            AutoCoupleTelemetry.NextLog(true, false, AutoCoupleAction.Couple, ThreeGateAbortReason.None, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
