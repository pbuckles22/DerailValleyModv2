using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 13.1.15 harvest: which writer moved throttle, or none when Cruise/GO are off.
/// </summary>
public class ThrottleWriterTelemetryTests
{
    [Fact]
    public void Smoke_13_1_15_writer_pid_when_governor_writes()
    {
        var cache = default(ThrottleWriterLogCache);
        Assert.Equal(
            "T2 writer: pid thr=27 spd=18 limit=40 risk=12",
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.Pid,
                thrPct: 27,
                spdKmh: 18,
                limitKmh: 40,
                riskPct: 12,
                cruiseOrGoOn: true,
                ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_writer_thermal_beats_pid()
    {
        Assert.Equal(
            ThrottleWriterKind.Thermal,
            ThrottleWriterTelemetry.Merge(ThrottleWriterKind.Pid, ThrottleWriterKind.Thermal));
        Assert.Equal(
            ThrottleWriterKind.DerailGov,
            ThrottleWriterTelemetry.Merge(ThrottleWriterKind.Thermal, ThrottleWriterKind.DerailGov));
    }

    [Fact]
    public void Smoke_13_1_15_repeat_writer_same_levers_silent()
    {
        var cache = default(ThrottleWriterLogCache);
        Assert.NotNull(
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.Thermal,
                40,
                22,
                60,
                8,
                cruiseOrGoOn: true,
                ref cache));
        Assert.Null(
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.Thermal,
                40,
                22,
                60,
                8,
                cruiseOrGoOn: true,
                ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_writer_none_when_throttle_drops_cruise_off()
    {
        var cache = default(ThrottleWriterLogCache);
        Assert.Null(
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.None,
                thrPct: 40,
                spdKmh: 20,
                limitKmh: 60,
                riskPct: 0,
                cruiseOrGoOn: false,
                ref cache));
        Assert.Equal(
            "T2 writer: none thr=9 spd=20 limit=60 risk=0",
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.None,
                thrPct: 9,
                spdKmh: 20,
                limitKmh: 60,
                riskPct: 0,
                cruiseOrGoOn: false,
                ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_writer_silent_when_cruise_on_and_no_governor()
    {
        var cache = default(ThrottleWriterLogCache);
        ThrottleWriterTelemetry.NextLog(
            ThrottleWriterKind.None, 40, 20, 60, 0, cruiseOrGoOn: true, ref cache);
        Assert.Null(
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.None, 9, 20, 60, 0, cruiseOrGoOn: true, ref cache));
    }

    [Fact]
    public void Smoke_13_1_15_missing_limit_is_dash()
    {
        var cache = default(ThrottleWriterLogCache);
        Assert.Equal(
            "T2 writer: derail-gov thr=0 spd=44 limit=— risk=70",
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.DerailGov,
                0,
                44,
                limitKmh: null,
                70,
                cruiseOrGoOn: false,
                ref cache));
    }

    [Fact]
    public void NextLog_does_not_allocate_when_writer_holds()
    {
        var cache = default(ThrottleWriterLogCache);
        ThrottleWriterTelemetry.NextLog(
            ThrottleWriterKind.Pid, 27, 18, 40, 12, true, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ThrottleWriterTelemetry.NextLog(
                ThrottleWriterKind.Pid, 27, 18, 40, 12, true, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Note_merges_per_frame_and_clears_next_frame()
    {
        ThrottleWriterNote.Reset();
        ThrottleWriterNote.Note(ThrottleWriterKind.Pid, frame: 10);
        ThrottleWriterNote.Note(ThrottleWriterKind.Thermal, frame: 10);
        Assert.Equal(ThrottleWriterKind.Thermal, ThrottleWriterNote.Peek(10));
        Assert.Equal(ThrottleWriterKind.None, ThrottleWriterNote.Peek(11));
    }
}
