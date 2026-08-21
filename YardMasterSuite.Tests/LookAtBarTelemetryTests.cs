using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: SW-B3I look-at bar (story 6.2). Analog pipe must not spam
/// <c>T2 look-at bar</c>; car / cargo / track identity logs on change.
/// </summary>
public class LookAtBarTelemetryTests
{
    [Fact]
    public void Smoke_aim_at_freight_logs_car_and_cargo_once()
    {
        var cache = default(LookAtBarCache);
        var token = LookAtBarTelemetry.CarToken(isLoco: false, freightNumberFromLoco: 1);

        var msg = LookAtBarTelemetry.Observe(
            visible: true,
            carToken: token,
            cargoRaw: "None",
            trackId: "SW-B3I",
            ref cache);

        Assert.Equal("T2 look-at bar: car=1 cargo=Empty track=SW-B3I", msg);
    }

    [Fact]
    public void Smoke_shunter_yard_logs_na_then_car1_forestry()
    {
        var cache = default(LookAtBarCache);
        var loco = LookAtBarTelemetry.CarToken(isLoco: true, freightNumberFromLoco: null);
        Assert.Equal(
            "T2 look-at bar: car=NA cargo= track=SW-B3I",
            LookAtBarTelemetry.Observe(true, loco, null, "SW-B3I", ref cache));

        var car1 = LookAtBarTelemetry.CarToken(isLoco: false, freightNumberFromLoco: 1);
        Assert.Equal(
            "T2 look-at bar: car=1 cargo=Forestry Trailers track=SW-B3I",
            LookAtBarTelemetry.Observe(true, car1, "ForestryTrailers", "SW-B3I", ref cache));
        Assert.Null(LookAtBarTelemetry.Observe(true, car1, "ForestryTrailers", "SW-B3I", ref cache));

        var car2 = LookAtBarTelemetry.CarToken(isLoco: false, freightNumberFromLoco: 2);
        Assert.Equal(
            "T2 look-at bar: car=2 cargo=Forestry Trailers track=SW-B3I",
            LookAtBarTelemetry.Observe(true, car2, "ForestryTrailers", "SW-B3I", ref cache));
    }

    [Fact]
    public void Smoke_pipe_pressure_chatter_is_silent()
    {
        var cache = default(LookAtBarCache);
        var token = LookAtBarTelemetry.CarToken(isLoco: false, freightNumberFromLoco: 1);
        LookAtBarTelemetry.Observe(true, token, "None", "SW-B3I", ref cache);

        var again = LookAtBarTelemetry.Observe(true, token, "None", "SW-B3I", ref cache);

        Assert.Null(again);
    }

    [Fact]
    public void Smoke_look_away_logs_hide()
    {
        var cache = default(LookAtBarCache);
        LookAtBarTelemetry.Observe(true, 1, "None", "SW-B3I", ref cache);

        var hide = LookAtBarTelemetry.Observe(
            visible: false,
            carToken: 0,
            cargoRaw: null,
            trackId: null,
            ref cache);

        Assert.Equal("T2 look-at bar: hide", hide);
        Assert.Null(LookAtBarTelemetry.Observe(false, 0, null, null, ref cache));
    }

    [Fact]
    public void Sky_before_first_car_is_silent()
    {
        var cache = default(LookAtBarCache);

        Assert.Null(LookAtBarTelemetry.Observe(false, 0, null, null, ref cache));
    }

    [Fact]
    public void Cargo_change_logs_again()
    {
        var cache = default(LookAtBarCache);
        LookAtBarTelemetry.Observe(true, 1, "None", "SW-B3I", ref cache);

        var msg = LookAtBarTelemetry.Observe(true, 1, "Coal", "SW-B3I", ref cache);

        Assert.Equal("T2 look-at bar: car=1 cargo=Coal track=SW-B3I", msg);
    }

    [Fact]
    public void Loco_look_at_uses_na_and_omits_cargo()
    {
        var cache = default(LookAtBarCache);
        var token = LookAtBarTelemetry.CarToken(isLoco: true, freightNumberFromLoco: 1);

        var msg = LookAtBarTelemetry.Observe(true, token, "Coal", "SW-B3I", ref cache);

        Assert.Equal("T2 look-at bar: car=NA cargo= track=SW-B3I", msg);
    }

    [Fact]
    public void Unknown_freight_uses_xx()
    {
        var cache = default(LookAtBarCache);
        var token = LookAtBarTelemetry.CarToken(isLoco: false, freightNumberFromLoco: null);

        var msg = LookAtBarTelemetry.Observe(true, token, "None", "", ref cache);

        Assert.Equal("T2 look-at bar: car=XX cargo=Empty track=", msg);
    }

    [Fact]
    public void Smoke_look_at_job_car_logs_job_id()
    {
        var cache = default(LookAtBarCache);
        var token = LookAtBarTelemetry.CarToken(isLoco: false, freightNumberFromLoco: 1);

        var msg = LookAtBarTelemetry.Observe(
            visible: true,
            carToken: token,
            cargoRaw: "None",
            trackId: "SW-B3I",
            ref cache,
            jobId: "FH-123");

        Assert.Equal("T2 look-at bar: car=1 cargo=Empty track=SW-B3I job=FH-123", msg);
        Assert.Null(LookAtBarTelemetry.Observe(
            true, token, "None", "SW-B3I", ref cache, "FH-123"));
    }
}
