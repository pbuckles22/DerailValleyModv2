using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16): save-load on a freight car — LastLoco null, usable consist loco drives cyan LOCO.
/// </summary>
public class ArLocoMarkerSourceTests
{
    [Fact]
    public void Smoke_save_load_on_freight_car_uses_usable_loco_when_last_null()
    {
        Assert.Equal(ArLocoMarkerPick.UsableLoco, ArLocoMarkerSource.Pick(hasLastLoco: false, hasUsableLoco: true));
    }

    [Fact]
    public void Last_loco_wins_when_both_present()
    {
        Assert.Equal(ArLocoMarkerPick.LastLoco, ArLocoMarkerSource.Pick(hasLastLoco: true, hasUsableLoco: true));
    }

    [Fact]
    public void None_when_neither_present()
    {
        Assert.Equal(ArLocoMarkerPick.None, ArLocoMarkerSource.Pick(hasLastLoco: false, hasUsableLoco: false));
    }

    [Fact]
    public void Smoke_last_loco_known_skips_per_frame_usable_probe()
    {
        Assert.False(ArLocoMarkerSource.ShouldProbeUsableLoco(hasLastLoco: true));
        Assert.True(ArLocoMarkerSource.ShouldProbeUsableLoco(hasLastLoco: false));
    }
}
