using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: always-on Clock chip (story 6.1). World time logs on
/// minute change only — not every LateUpdate tick.
/// </summary>
public class ClockTelemetryTests
{
    [Fact]
    public void Smoke_yard_world_time_emits_T2_clock_init()
    {
        var cache = default(ClockCache);

        Assert.True(ClockTelemetry.Observe(known: true, hour: 14, minute: 30, ref cache));
        Assert.Equal(
            "T2 clock init: 14:30",
            ClockTelemetry.NextLog(hour: 14, minute: 30, ClockLogKind.Init));
    }

    [Fact]
    public void Smoke_minute_tick_emits_T2_clock_change()
    {
        var cache = default(ClockCache);
        ClockTelemetry.Observe(known: true, hour: 14, minute: 30, ref cache);

        Assert.True(ClockTelemetry.Observe(known: true, hour: 14, minute: 31, ref cache));
        Assert.Equal(
            "T2 clock change: 14:31",
            ClockTelemetry.NextLog(hour: 14, minute: 31, ClockLogKind.Change));
    }

    [Fact]
    public void Smoke_office_wall_clock_minute_tick_crosses_noon()
    {
        var cache = default(ClockCache);
        Assert.True(ClockTelemetry.Observe(known: true, hour: 11, minute: 57, ref cache));
        Assert.Equal(
            "T2 clock init: 11:57",
            ClockTelemetry.NextLog(hour: 11, minute: 57, ClockLogKind.Init));

        Assert.True(ClockTelemetry.Observe(known: true, hour: 12, minute: 1, ref cache));
        Assert.Equal(
            "T2 clock change: 12:01",
            ClockTelemetry.NextLog(hour: 12, minute: 1, ClockLogKind.Change));
    }

    [Fact]
    public void Same_minute_is_silent()
    {
        var cache = default(ClockCache);
        ClockTelemetry.Observe(known: true, hour: 9, minute: 5, ref cache);

        Assert.False(ClockTelemetry.Observe(known: true, hour: 9, minute: 5, ref cache));
    }

    [Fact]
    public void Unknown_after_known_emits_T2_clock_hide()
    {
        var cache = default(ClockCache);
        ClockTelemetry.Observe(known: true, hour: 6, minute: 0, ref cache);

        Assert.True(ClockTelemetry.Observe(known: false, hour: 0, minute: 0, ref cache));
        Assert.Equal("T2 clock hide", ClockTelemetry.NextLog(hour: 0, minute: 0, ClockLogKind.Hide));
    }

    [Fact]
    public void First_unknown_is_silent()
    {
        var cache = default(ClockCache);

        Assert.False(ClockTelemetry.Observe(known: false, hour: 0, minute: 0, ref cache));
    }

    [Fact]
    public void Unknown_stays_silent()
    {
        var cache = default(ClockCache);
        ClockTelemetry.Observe(known: false, hour: 0, minute: 0, ref cache);

        Assert.False(ClockTelemetry.Observe(known: false, hour: 0, minute: 0, ref cache));
    }
}

public class ClockDisplayTests
{
    [Fact]
    public void Smoke_office_wall_clock_matches_hud_hhmm()
    {
        Assert.Equal("Clock 11:57", ClockDisplay.Format(11, 57));
        Assert.Equal("Clock 12:01", ClockDisplay.Format(12, 1));
    }

    [Fact]
    public void Format_pads_hour_and_minute()
    {
        Assert.Equal("Clock 09:05", ClockDisplay.Format(9, 5));
        Assert.Equal("Clock 14:30", ClockDisplay.Format(14, 30));
    }

    [Fact]
    public void Format_from_DateTime()
    {
        Assert.Equal("Clock 06:00", ClockDisplay.Format(new DateTime(1, 1, 1, 6, 0, 0)));
        Assert.Equal("— Clock", ClockDisplay.Format((DateTime?)null));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(24, 0)]
    [InlineData(12, 60)]
    public void Format_rejects_out_of_range(int hour, int minute)
    {
        Assert.Equal("— Clock", ClockDisplay.Format(hour, minute));
    }
}
