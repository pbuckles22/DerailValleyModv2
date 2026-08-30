using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PidSpeedTelemetryTests
{
    [Fact]
    public void Smoke_9_1_hold_emits_once()
    {
        var cache = default(PidSpeedLogCache);
        Assert.Equal(
            PidSpeedTelemetry.Hold,
            PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache));
        Assert.Null(PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache));
    }

    [Fact]
    public void Smoke_9_1_idle_after_hold()
    {
        var cache = default(PidSpeedLogCache);
        Assert.Equal(PidSpeedTelemetry.Hold, PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache));
        Assert.Equal(PidSpeedTelemetry.Idle, PidSpeedTelemetry.NextLog(PidSpeedMode.Idle, ref cache));
    }

    [Fact]
    public void Smoke_9_1_yield_derail_is_distinct()
    {
        var cache = default(PidSpeedLogCache);
        Assert.Equal(
            PidSpeedTelemetry.YieldDerail,
            PidSpeedTelemetry.NextLog(PidSpeedMode.YieldDerail, ref cache));
        Assert.Equal(
            PidSpeedMode.YieldDerail,
            PidSpeedTelemetry.Mode(armed: true, derailIntervening: true));
        Assert.Equal(
            PidSpeedMode.Hold,
            PidSpeedTelemetry.Mode(armed: true, derailIntervening: false));
        Assert.Equal(
            PidSpeedMode.Idle,
            PidSpeedTelemetry.Mode(armed: false, derailIntervening: true));
        Assert.Equal(
            PidSpeedMode.Gear,
            PidSpeedTelemetry.Mode(armed: true, derailIntervening: false, gearPending: true));
    }

    [Fact]
    public void Smoke_9_1_gear_emits_once()
    {
        var cache = default(PidSpeedLogCache);
        Assert.Equal(PidSpeedTelemetry.Gear, PidSpeedTelemetry.NextLog(PidSpeedMode.Gear, ref cache));
        Assert.Null(PidSpeedTelemetry.NextLog(PidSpeedMode.Gear, ref cache));
        Assert.Equal(PidSpeedTelemetry.Hold, PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache));
    }

    [Fact]
    public void Smoke_9_1_brakes_then_hold()
    {
        var cache = default(PidSpeedLogCache);
        Assert.Equal(
            PidSpeedMode.ReleaseAir,
            PidSpeedTelemetry.Mode(
                armed: true,
                derailIntervening: false,
                gearPending: false,
                brakePending: true));
        Assert.Equal(
            PidSpeedTelemetry.ReleaseAir,
            PidSpeedTelemetry.NextLog(PidSpeedMode.ReleaseAir, ref cache));
        Assert.Null(PidSpeedTelemetry.NextLog(PidSpeedMode.ReleaseAir, ref cache));
        Assert.Equal(PidSpeedTelemetry.Hold, PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache));
    }

    [Fact]
    public void Smoke_9_1_thr_on_after_hold()
    {
        var cache = default(PidSpeedThrCache);
        Assert.Null(PidSpeedTelemetry.NextThr(want: false, ref cache));
        Assert.Equal(PidSpeedTelemetry.ThrOn, PidSpeedTelemetry.NextThr(want: true, ref cache));
        Assert.Null(PidSpeedTelemetry.NextThr(want: true, ref cache));
        Assert.Equal(PidSpeedTelemetry.ThrOff, PidSpeedTelemetry.NextThr(want: false, ref cache));
        Assert.True(PidSpeedTelemetry.WantsThrottle(
            armed: true,
            gearPending: false,
            brakePending: false,
            desiredThrottle: PidSpeedHold.MinNotch));
        Assert.False(PidSpeedTelemetry.WantsThrottle(true, false, brakePending: true, 1f));
        Assert.False(PidSpeedTelemetry.WantsThrottle(armed: false, false, false, 1f));
    }

    [Fact]
    public void Smoke_9_1_10_thr_off_apply_logs_zero_thr_and_indy_27()
    {
        var cache = default(PidSpeedApplyCache);
        Assert.Equal(
            "T2 pid: apply thr=0 indy=27",
            PidSpeedTelemetry.NextApply(0f, PidSpeedHold.OverspeedIndependent, ref cache));
        Assert.Null(
            PidSpeedTelemetry.NextApply(0f, PidSpeedHold.OverspeedIndependent, ref cache));
        Assert.Equal(
            PidSpeedTelemetry.SkipOverlay,
            PidSpeedTelemetry.NextSkip(overlay: true, ref cache));
        Assert.Null(PidSpeedTelemetry.NextSkip(overlay: true, ref cache));
        Assert.Equal(
            PidSpeedTelemetry.SkipGate,
            PidSpeedTelemetry.NextSkip(overlay: false, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_mode_holds()
    {
        var cache = default(PidSpeedLogCache);
        var thr = default(PidSpeedThrCache);
        var apply = default(PidSpeedApplyCache);
        PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache);
        PidSpeedTelemetry.NextThr(want: true, ref thr);
        PidSpeedTelemetry.NextApply(0.09f, 0f, ref apply);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            PidSpeedTelemetry.NextLog(PidSpeedMode.Hold, ref cache);
            PidSpeedTelemetry.NextThr(want: true, ref thr);
            PidSpeedTelemetry.NextApply(0.09f, 0f, ref apply);
            PidSpeedTelemetry.NextSkip(overlay: false, ref apply);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
