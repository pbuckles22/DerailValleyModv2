using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Look-at cargo chip (story 6.2). Locos omit the segment.
/// </summary>
public class CargoDisplayTests
{
    [Fact]
    public void Format_omits_segment_for_locos()
    {
        Assert.Null(CargoDisplay.Format(isLoco: true, "SteelRails"));
    }

    [Fact]
    public void Format_empty_or_none_is_empty_cargo()
    {
        Assert.Equal("Empty Cargo", CargoDisplay.Format(isLoco: false, null));
        Assert.Equal("Empty Cargo", CargoDisplay.Format(isLoco: false, "None"));
        Assert.Equal("Empty Cargo", CargoDisplay.Format(isLoco: false, "EmptyGoorsk"));
    }

    [Fact]
    public void Format_humanizes_enum_names()
    {
        Assert.Equal("Cargo Steel Rails", CargoDisplay.Format(isLoco: false, "SteelRails"));
        Assert.Equal("Cargo Coal", CargoDisplay.Format(isLoco: false, "Coal"));
        Assert.Equal("Cargo Forestry Trailers", CargoDisplay.Format(isLoco: false, "ForestryTrailers"));
    }
}
