using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.2 discrete T2 thermal lines (no per-tick spam).</summary>
public class ThermalTelemetryTests
{
    [Fact]
    public void Smoke_warning_hot_emits_soft_cap_75()
    {
        var cache = default(ThermalCapLogCache);
        Assert.Equal(
            ThermalTelemetry.SoftCapWarning,
            ThermalTelemetry.NextLog(
                applied: true,
                ThreeGateAbortReason.None,
                ThermalTelemetry.CapKind(MotorCabTempBand.Warning),
                ref cache));
    }

    [Fact]
    public void Smoke_critical_hot_emits_soft_cap_55()
    {
        var cache = default(ThermalCapLogCache);
        Assert.Equal(
            ThermalTelemetry.SoftCapCritical,
            ThermalTelemetry.NextLog(
                applied: true,
                ThreeGateAbortReason.None,
                ThermalTelemetry.CapKind(MotorCabTempBand.Critical),
                ref cache));
        Assert.Equal(
            ThermalTelemetry.SoftCapCritical,
            ThermalTelemetry.LineForKind(ThermalTelemetry.CapKind(MotorCabTempBand.WarningAndCritical)));
    }

    [Fact]
    public void Smoke_hot_null_band_emits_soft_cap_hot()
    {
        var cache = default(ThermalCapLogCache);
        Assert.Equal(
            ThermalTelemetry.SoftCapHot,
            ThermalTelemetry.NextLog(
                applied: true,
                ThreeGateAbortReason.None,
                ThermalTelemetry.CapKind(null),
                ref cache));
    }

    [Fact]
    public void Smoke_repeat_soft_cap_is_silent()
    {
        var cache = default(ThermalCapLogCache);
        var kind = ThermalTelemetry.CapKind(MotorCabTempBand.Warning);
        Assert.NotNull(ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache));
        Assert.Null(ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache));
    }

    [Fact]
    public void Smoke_cap_release_when_cool()
    {
        var cache = default(ThermalCapLogCache);
        var kind = ThermalTelemetry.CapKind(MotorCabTempBand.Warning);
        Assert.NotNull(ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache));
        Assert.Equal(
            ThermalTelemetry.CapRelease,
            ThermalTelemetry.NextLog(false, ThreeGateAbortReason.Safety, kind, ref cache));
    }

    [Fact]
    public void Smoke_integrity_abort_after_cap_emits_abort()
    {
        var cache = default(ThermalCapLogCache);
        var kind = ThermalTelemetry.CapKind(MotorCabTempBand.Warning);
        Assert.NotNull(ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache));
        Assert.Equal(
            ThermalTelemetry.AbortIntegrity,
            ThermalTelemetry.NextLog(false, ThreeGateAbortReason.Integrity, kind, ref cache));
    }

    [Fact]
    public void Repeat_release_is_silent()
    {
        var cache = default(ThermalCapLogCache);
        var kind = ThermalTelemetry.CapKind(MotorCabTempBand.Warning);
        ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache);
        ThermalTelemetry.NextLog(false, ThreeGateAbortReason.Safety, kind, ref cache);
        Assert.Null(ThermalTelemetry.NextLog(false, ThreeGateAbortReason.Safety, kind, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_cap_holds()
    {
        var cache = default(ThermalCapLogCache);
        var kind = ThermalTelemetry.CapKind(MotorCabTempBand.Warning);
        ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ThermalTelemetry.NextLog(true, ThreeGateAbortReason.None, kind, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
