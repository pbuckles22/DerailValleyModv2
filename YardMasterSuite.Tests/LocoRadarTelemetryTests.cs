using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16): discrete T2 loco-radar scan line (not per frame).
/// </summary>
public class LocoRadarTelemetryTests
{
    [Fact]
    public void Smoke_scan_emits_T2_loco_radar()
    {
        Assert.Equal(
            "T2 loco-radar: scan reason=CityEntered city=SW excl=1 unlic=0 cands=2 n=2 fotMs=12",
            LocoRadarTelemetry.FormatScan(
                LocoRadarScanReason.CityEntered,
                city: "SW",
                leftLocoId: null,
                excluded: 1,
                unlicensed: 0,
                candidates: 2,
                count: 2,
                fotMs: 12));
    }

    [Fact]
    public void FormatScan_includes_left_id_on_leave()
    {
        Assert.Equal(
            "T2 loco-radar: scan reason=LeftLoco city=FF left=99 excl=0 unlic=0 cands=1 n=1 fotMs=8",
            LocoRadarTelemetry.FormatScan(
                LocoRadarScanReason.LeftLoco,
                city: "FF",
                leftLocoId: 99,
                excluded: 0,
                unlicensed: 0,
                candidates: 1,
                count: 1,
                fotMs: 8));
    }

    [Fact]
    public void Smoke_empty_save_load_scan_counts_excl_and_unlic()
    {
        Assert.Equal(
            "T2 loco-radar: scan reason=Forced city=SW excl=1 unlic=2 cands=0 n=0 fotMs=74",
            LocoRadarTelemetry.FormatScan(
                LocoRadarScanReason.Forced,
                city: "SW",
                leftLocoId: null,
                excluded: 1,
                unlicensed: 2,
                candidates: 0,
                count: 0,
                fotMs: 74));
    }

    [Fact]
    public void FormatScan_dash_city_when_missing()
    {
        Assert.Equal(
            "T2 loco-radar: scan reason=Forced city=— excl=0 unlic=0 cands=0 n=0 fotMs=3",
            LocoRadarTelemetry.FormatScan(
                LocoRadarScanReason.Forced,
                city: null,
                leftLocoId: null,
                excluded: 0,
                unlicensed: 0,
                candidates: 0,
                count: 0,
                fotMs: 3));
    }
}
