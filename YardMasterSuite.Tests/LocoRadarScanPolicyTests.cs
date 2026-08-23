using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16): FoT scan only on city enter / leave-loco / force — never a timer.
/// </summary>
public class LocoRadarScanPolicyTests
{
    [Fact]
    public void Decide_disabled_never_scans()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: false,
            forceScan: true,
            lastScannedCityId: null,
            currentCityId: "FF",
            lastOccupiedLocoId: 1,
            currentOccupiedLocoId: null,
            out var left);

        Assert.Equal(LocoRadarScanReason.None, reason);
        Assert.Null(left);
    }

    [Fact]
    public void Smoke_sitting_in_cab_same_city_does_not_rescan()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "FF",
            lastOccupiedLocoId: 42,
            currentOccupiedLocoId: 42,
            out var left);

        Assert.Equal(LocoRadarScanReason.None, reason);
        Assert.Null(left);
    }

    [Fact]
    public void Smoke_city_enter_scans_once()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "SM",
            lastOccupiedLocoId: 42,
            currentOccupiedLocoId: 42,
            out _);

        Assert.Equal(LocoRadarScanReason.CityEntered, reason);
    }

    [Fact]
    public void Decide_first_city_seen_scans()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: null,
            currentCityId: "HB",
            lastOccupiedLocoId: null,
            currentOccupiedLocoId: null,
            out _);

        Assert.Equal(LocoRadarScanReason.CityEntered, reason);
    }

    [Fact]
    public void Smoke_leave_loco_scans_and_marks_left()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "FF",
            lastOccupiedLocoId: 99,
            currentOccupiedLocoId: null,
            out var left);

        Assert.Equal(LocoRadarScanReason.LeftLoco, reason);
        Assert.Equal(99, left);
    }

    [Fact]
    public void Decide_switch_loco_scans_marks_departed()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: "FF",
            currentCityId: "FF",
            lastOccupiedLocoId: 10,
            currentOccupiedLocoId: 20,
            out var left);

        Assert.Equal(LocoRadarScanReason.LeftLoco, reason);
        Assert.Equal(10, left);
    }

    [Fact]
    public void Decide_force_beats_idle()
    {
        Assert.Equal(
            LocoRadarScanReason.Forced,
            LocoRadarScanPolicy.Decide(
                featureEnabled: true,
                forceScan: true,
                lastScannedCityId: "FF",
                currentCityId: "FF",
                lastOccupiedLocoId: 1,
                currentOccupiedLocoId: 1,
                out _));
    }

    [Fact]
    public void Smoke_empty_city_does_not_rescan_every_frame()
    {
        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: false,
            lastScannedCityId: null,
            currentCityId: null,
            lastOccupiedLocoId: null,
            currentOccupiedLocoId: null,
            out _);

        Assert.Equal(LocoRadarScanReason.None, reason);
    }

    [Fact]
    public void Smoke_second_game_load_invalidates_radar_cache()
    {
        Assert.True(LocoRadarScanPolicy.ShouldInvalidateCache(wasInWorld: true, inWorld: false));
        Assert.False(LocoRadarScanPolicy.ShouldInvalidateCache(wasInWorld: true, inWorld: true));
        Assert.False(LocoRadarScanPolicy.ShouldInvalidateCache(wasInWorld: false, inWorld: false));
    }

    [Fact]
    public void Smoke_second_game_load_same_city_still_scans()
    {
        Assert.True(LocoRadarScanPolicy.ShouldForceScanOnWorldEnter(wasInWorld: false, inWorld: true));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnWorldEnter(wasInWorld: true, inWorld: true));

        var reason = LocoRadarScanPolicy.Decide(
            featureEnabled: true,
            forceScan: true,
            lastScannedCityId: "SW",
            currentCityId: "SW",
            lastOccupiedLocoId: null,
            currentOccupiedLocoId: null,
            out _);

        Assert.Equal(LocoRadarScanReason.Forced, reason);
    }

    [Fact]
    public void Smoke_stale_radar_cache_with_no_live_cars_forces_rescan()
    {
        Assert.True(LocoRadarScanPolicy.ShouldForceScanWhenCacheDead(cachedCount: 2, liveCount: 0));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanWhenCacheDead(cachedCount: 2, liveCount: 1));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanWhenCacheDead(cachedCount: 0, liveCount: 0));
    }

    [Fact]
    public void Smoke_license_change_forces_radar_rescan()
    {
        Assert.True(LocoRadarScanPolicy.ShouldForceScanOnLicenseChange(2, 8));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnLicenseChange(6, 6));
    }
}
