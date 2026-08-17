using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MonitorHudStackLayoutTests
{
    [Fact]
    public void StackBottom_always_on_only()
    {
        Assert.Equal(40f, MonitorHudStackLayout.StackBottomGuiY(false, false, false));
    }

    [Fact]
    public void StackBottom_loco_plus_always_on()
    {
        Assert.Equal(72f, MonitorHudStackLayout.StackBottomGuiY(true, false, false));
    }

    [Fact]
    public void StackBottom_all_optional_bars()
    {
        Assert.Equal(136f, MonitorHudStackLayout.StackBottomGuiY(true, true, true));
    }

    [Fact]
    public void StickyRowTop_adds_gap()
    {
        Assert.Equal(48f, ArStickyRowPlacement.StickyRowTopGuiY(40f));
        Assert.Equal(50f, ArStickyRowPlacement.StickyRowTopGuiY(40f, 10f));
    }
}

public class HudCenterLayoutTests
{
    [Fact]
    public void CenteredBarX_centers_when_room()
    {
        Assert.Equal(450f, HudCenterLayout.CenteredBarX(100f, 1000f, 12f));
    }

    [Fact]
    public void CenteredBarX_clamps_to_pad_when_too_wide()
    {
        Assert.Equal(12f, HudCenterLayout.CenteredBarX(1000f, 500f, 12f));
    }
}

public class ArStickyRowPlacementTests
{
    [Fact]
    public void PinScreenYToStickyRow_converts_gui_center_to_unity_screen_y()
    {
        float screenY = 999f;
        ArStickyRowPlacement.PinScreenYToStickyRow(100f, 600f, ref screenY);
        Assert.Equal(500f, screenY);
    }

    [Fact]
    public void MarkerTopGuiY_aligns_to_sticky_strip()
    {
        Assert.Equal(48f, ArStickyRowPlacement.MarkerTopGuiY(48f, 60f));
    }
}
