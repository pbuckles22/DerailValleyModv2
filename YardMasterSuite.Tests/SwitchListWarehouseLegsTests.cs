using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListWarehouseLegsTests
{
    [Fact]
    public void Smoke_SL_55_booklet_load_at_B4L_is_spot_then_load_then_leave()
    {
        Assert.True(SwitchListWarehouseLegs.HasWarehouse("SW-B4L"));
        Assert.True(SwitchListWarehouseLegs.ShouldSpotLoader("SW-B4L", "SW-C4S"));
        Assert.False(SwitchListWarehouseLegs.ShouldSpotLoader("SW-B4L", "SW-B4L"));
        Assert.Equal("Wood Chips", SwitchListWarehouseLegs.FormatCargoLabel("WoodChips"));
        Assert.Equal(
            "Load Wood Chips at SW-B4L",
            SwitchListWarehouseLegs.LoadLabel("SW-B4L", "Wood Chips", unload: false));
        Assert.True(SwitchListWarehouseLegs.IsLoaderSpot("Into loader → SW-B4L"));
        Assert.True(SwitchListRunner.StepRequiresHuman(SwitchListStepKind.Load));
        Assert.False(SwitchListRunner.StepSupportsGo(SwitchListStepKind.Load));
    }
}
