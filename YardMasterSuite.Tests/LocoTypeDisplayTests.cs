using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Look-at loco-type chip (story 6.2). Freight must not show as Loco FlatbedEmpty.
/// </summary>
public class LocoTypeDisplayTests
{
    [Fact]
    public void Format_null_or_blank_omits_segment()
    {
        Assert.Null(LocoTypeDisplay.Format(null));
        Assert.Null(LocoTypeDisplay.Format(""));
        Assert.Null(LocoTypeDisplay.Format("   "));
    }

    [Fact]
    public void Format_strips_Loco_prefix_from_game_id()
    {
        Assert.Equal("Loco DE6", LocoTypeDisplay.Format("LocoDE6"));
        Assert.Equal("Loco DE2", LocoTypeDisplay.Format("LocoDE2"));
    }

    [Fact]
    public void Smoke_freight_omits_loco_type_chip()
    {
        Assert.Null(LocoTypeDisplay.Format(isLoco: false, "FlatbedEmpty"));
        Assert.Equal("Loco S060", LocoTypeDisplay.Format(isLoco: true, "S060"));
        Assert.Equal("Loco Shunter", LocoTypeDisplay.Format(isLoco: true, "LocoShunter"));
    }
}
