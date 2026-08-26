using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.3 discrete T2 autobrake lines (no per-tick spam).</summary>
public class AutoBrakeTelemetryTests
{
    [Fact]
    public void Smoke_engine_off_emits_applying()
    {
        var cache = default(AutoBrakeLogCache);
        Assert.Equal(
            AutoBrakeTelemetry.Applying,
            AutoBrakeTelemetry.NextLog(
                applying: true,
                sessionNeedsWork: true,
                ThreeGateAbortReason.None,
                ref cache));
    }

    [Fact]
    public void Smoke_repeat_applying_is_silent()
    {
        var cache = default(AutoBrakeLogCache);
        Assert.NotNull(AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache));
        Assert.Null(AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache));
    }

    [Fact]
    public void Smoke_apply_done_when_air_full_and_throttle_idle()
    {
        var cache = default(AutoBrakeLogCache);
        Assert.NotNull(AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoBrakeTelemetry.ApplyDone,
            AutoBrakeTelemetry.NextLog(false, sessionNeedsWork: false, ThreeGateAbortReason.None, ref cache));
    }

    [Fact]
    public void Smoke_integrity_abort_after_applying_emits_abort()
    {
        var cache = default(AutoBrakeLogCache);
        Assert.NotNull(AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoBrakeTelemetry.AbortIntegrity,
            AutoBrakeTelemetry.NextLog(false, true, ThreeGateAbortReason.Integrity, ref cache));
    }

    [Fact]
    public void Smoke_engine_start_abort_is_safety()
    {
        var cache = default(AutoBrakeLogCache);
        Assert.NotNull(AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache));
        Assert.Equal(
            AutoBrakeTelemetry.AbortSafety,
            AutoBrakeTelemetry.NextLog(false, true, ThreeGateAbortReason.Safety, ref cache));
    }

    [Fact]
    public void Idle_ticks_are_silent()
    {
        var cache = default(AutoBrakeLogCache);
        Assert.Null(AutoBrakeTelemetry.NextLog(false, true, ThreeGateAbortReason.Safety, ref cache));
    }

    [Fact]
    public void Repeat_done_is_silent()
    {
        var cache = default(AutoBrakeLogCache);
        AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache);
        AutoBrakeTelemetry.NextLog(false, false, ThreeGateAbortReason.None, ref cache);
        Assert.Null(AutoBrakeTelemetry.NextLog(false, false, ThreeGateAbortReason.None, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_apply_holds()
    {
        var cache = default(AutoBrakeLogCache);
        AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            AutoBrakeTelemetry.NextLog(true, true, ThreeGateAbortReason.None, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
