using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// T2 mark logs on set/clear / bearing change — not every meter (**6.11**).
/// </summary>
public class ParkMarkTelemetryTests
{
    [Fact]
    public void Smoke_home_emits_T2_mark_init_here()
    {
        var cache = default(ParkMarkCache);
        Assert.True(ParkMarkTelemetry.Observe(
            hasMark: true,
            markX: 10f,
            markZ: 20f,
            playerX: 10.2f,
            playerZ: 20.1f,
            ref cache));
        Assert.Equal(
            "T2 mark init: Marked here",
            ParkMarkTelemetry.NextLog(null, ParkMarkTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_shift_home_emits_T2_mark_change_cleared()
    {
        var cache = default(ParkMarkCache);
        ParkMarkTelemetry.Observe(true, 0f, 0f, 0.1f, 0.1f, ref cache);
        var prior = ParkMarkTelemetry.Snapshot(ref cache);

        Assert.True(ParkMarkTelemetry.Observe(false, null, null, 0f, 0f, ref cache));
        Assert.Equal(
            "T2 mark change: — Marked",
            ParkMarkTelemetry.NextLog(prior, ParkMarkTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Same_bearing_and_meters_is_silent()
    {
        var cache = default(ParkMarkCache);
        ParkMarkTelemetry.Observe(true, 0f, 0f, 100f, 0f, ref cache);
        Assert.False(ParkMarkTelemetry.Observe(true, 0f, 0f, 100f, 0f, ref cache));
    }

    [Fact]
    public void Meter_step_publishes_hud_but_not_T2()
    {
        var cache = default(ParkMarkCache);
        ParkMarkTelemetry.Observe(true, 0f, 0f, 100f, 0f, ref cache);
        var prior = ParkMarkTelemetry.Snapshot(ref cache);

        Assert.True(ParkMarkTelemetry.Observe(true, 0f, 0f, 101f, 0f, ref cache));
        Assert.Null(ParkMarkTelemetry.NextLog(prior, ParkMarkTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Observe_does_not_allocate_when_buckets_hold()
    {
        var cache = default(ParkMarkCache);
        ParkMarkTelemetry.Observe(true, 0f, 0f, 100f, 0f, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ParkMarkTelemetry.Observe(true, 0f, 0f, 100f, 0f, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
