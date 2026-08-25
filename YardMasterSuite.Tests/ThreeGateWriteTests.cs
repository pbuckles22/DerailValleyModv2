using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>Tier 1 — 7.1 Three-Gate is the only write path; fail closed.</summary>
public class ThreeGateWriteTests
{
    [Fact]
    public void Smoke_off_train_keydown_integrity_aborts_without_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: false),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Integrity, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_missing_control_state_registry_aborts_without_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: false),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.StateRegistry, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_blocked_control_safety_aborts_without_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: true, controlNotBlocked: false),
            () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Safety, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_pause_overlay_safety_aborts_without_write()
    {
        var calls = 0;
        var result = ThreeGate.TryApply(
            ThreeGateWrite.Integrity(worldActive: true, actorPresent: true),
            ThreeGateWrite.StateRegistry(controlPresent: true),
            ThreeGateWrite.Safety(overlayClear: false, controlNotBlocked: true),
            () =>
            {
                calls++;
                return true;
            });

        Assert.False(result.Applied);
        Assert.Equal(ThreeGateAbortReason.Safety, result.AbortReason);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Smoke_reverser_apply_emits_T2_three_gate_apply()
    {
        var cache = default(ThreeGateLogCache);
        var result = ThreeGateResult.Ok();
        Assert.Equal(
            "T2 three-gate: apply write=reverser",
            ThreeGateTelemetry.NextLog(result, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache));
    }

    [Fact]
    public void Smoke_off_train_emits_T2_three_gate_abort_integrity()
    {
        var cache = default(ThreeGateLogCache);
        var result = ThreeGateResult.Abort(ThreeGateAbortReason.Integrity);
        Assert.Equal(
            "T2 three-gate: abort Integrity write=reverser",
            ThreeGateTelemetry.NextLog(result, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache));
    }

    [Fact]
    public void Smoke_repeat_integrity_abort_is_silent()
    {
        var cache = default(ThreeGateLogCache);
        var result = ThreeGateResult.Abort(ThreeGateAbortReason.Integrity);
        Assert.NotNull(ThreeGateTelemetry.NextLog(result, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache));
        Assert.Null(ThreeGateTelemetry.NextLog(result, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache));
    }

    [Fact]
    public void Hold_rewrite_does_not_relog_apply()
    {
        var cache = default(ThreeGateLogCache);
        var applied = ThreeGateResult.Ok();
        Assert.NotNull(ThreeGateTelemetry.NextLog(applied, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache));
        Assert.Null(ThreeGateTelemetry.NextLog(applied, ThreeGateTelemetry.WriteReverser, logApply: false, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_abort_holds()
    {
        var cache = default(ThreeGateLogCache);
        var result = ThreeGateResult.Abort(ThreeGateAbortReason.Integrity);
        ThreeGateTelemetry.NextLog(result, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ThreeGateTelemetry.NextLog(result, ThreeGateTelemetry.WriteReverser, logApply: true, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
