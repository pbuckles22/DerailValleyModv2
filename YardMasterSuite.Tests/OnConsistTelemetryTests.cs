using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>T2 on-consist arm/disarm — not every notch (**5.3** slice / 6.13 stack).</summary>
public class OnConsistTelemetryTests
{
    [Fact]
    public void Smoke_stand_on_last_car_emits_T2_on_consist_armed()
    {
        var cache = default(OnConsistCache);
        Assert.True(OnConsistTelemetry.Observe(armed: true, ref cache));
        Assert.Equal(
            "T2 on-consist: armed (cab bindings → front loco)",
            OnConsistTelemetry.NextLog(wasSeeded: false, wasArmed: false, armed: true));
    }

    [Fact]
    public void Smoke_step_off_train_emits_T2_on_consist_disarmed()
    {
        var cache = default(OnConsistCache);
        OnConsistTelemetry.Observe(true, ref cache);
        Assert.True(OnConsistTelemetry.Observe(armed: false, ref cache));
        Assert.Equal(
            "T2 on-consist: disarmed",
            OnConsistTelemetry.NextLog(wasSeeded: true, wasArmed: true, armed: false));
    }

    [Fact]
    public void Same_armed_is_silent()
    {
        var cache = default(OnConsistCache);
        OnConsistTelemetry.Observe(true, ref cache);
        Assert.False(OnConsistTelemetry.Observe(true, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_armed_holds()
    {
        var cache = default(OnConsistCache);
        OnConsistTelemetry.Observe(true, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            OnConsistTelemetry.Observe(true, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
