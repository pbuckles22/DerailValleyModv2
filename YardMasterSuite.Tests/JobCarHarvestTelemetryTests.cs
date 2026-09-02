using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 13.1.15 harvest: change-only <c>T2 job-car-ar: n=K ids=…</c> for extra purple pins.
/// </summary>
public class JobCarHarvestTelemetryTests
{
    [Fact]
    public void Smoke_13_1_15_preview_pins_emit_n_and_ids()
    {
        var cache = default(JobCarPinLogCache);
        var ids = new[] { "SW-C1O", "#Y-#S1313#T", "#Y-#S281#T", "---" };
        Assert.Equal(
            "T2 job-car-ar: n=4 ids=SW-C1O,#Y-#S1313#T,#Y-#S281#T,---",
            JobCarTelemetry.NextPins(4, ids, ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_repeat_pin_ids_are_silent()
    {
        var cache = default(JobCarPinLogCache);
        var ids = new[] { "SW-C1O", "---" };
        Assert.NotNull(JobCarTelemetry.NextPins(2, ids, ref cache));
        Assert.Null(JobCarTelemetry.NextPins(2, ids, ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_pin_count_change_emits_again()
    {
        var cache = default(JobCarPinLogCache);
        var two = new[] { "SW-C1O", "---" };
        var one = new[] { "SW-C1O" };
        Assert.Equal(
            "T2 job-car-ar: n=2 ids=SW-C1O,---",
            JobCarTelemetry.NextPins(2, two, ref cache));
        Assert.Equal(
            "T2 job-car-ar: n=1 ids=SW-C1O",
            JobCarTelemetry.NextPins(1, one, ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_zero_pins_emits_dash_ids()
    {
        var cache = default(JobCarPinLogCache);
        Assert.Equal(
            "T2 job-car-ar: n=0 ids=—",
            JobCarTelemetry.NextPins(0, Array.Empty<string>(), ref cache));
    }

    [Fact]
    public void NextPins_does_not_allocate_when_ids_hold()
    {
        var cache = default(JobCarPinLogCache);
        var ids = new[] { "SW-C1O", "---" };
        JobCarTelemetry.NextPins(2, ids, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            JobCarTelemetry.NextPins(2, ids, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
