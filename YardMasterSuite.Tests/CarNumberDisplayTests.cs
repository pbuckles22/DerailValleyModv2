using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Look-at car index (story 6.2). Loco is N/A; freight is 1-based from the loco.
/// </summary>
public class CarNumberDisplayTests
{
    [Fact]
    public void Format_loco_is_na()
    {
        Assert.Equal("Car N/A", CarNumberDisplay.Format(isLoco: true, freightNumberFromLoco: null));
        Assert.Equal("Car N/A", CarNumberDisplay.Format(isLoco: true, freightNumberFromLoco: 1));
    }

    [Fact]
    public void Format_shows_xx_when_not_on_usable_train()
    {
        Assert.Equal("Car XX", CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: null));
    }

    [Theory]
    [InlineData(1, "Car 1")]
    [InlineData(7, "Car 7")]
    public void Format_shows_freight_number(int number, string expected)
    {
        Assert.Equal(expected, CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: number));
    }

    [Fact]
    public void Smoke_shunter_yard_freight_number_excludes_loco()
    {
        // SW-B3I: shunter + two freight (Cars 3).
        var flags = new[] { true, false, false };

        Assert.Null(CarNumberDisplay.FreightNumberFromLoco(0, 0, flags));
        Assert.Equal(1, CarNumberDisplay.FreightNumberFromLoco(0, 1, flags));
        Assert.Equal(2, CarNumberDisplay.FreightNumberFromLoco(0, 2, flags));
        Assert.Equal("Car N/A", CarNumberDisplay.Format(isLoco: true, freightNumberFromLoco: null));
        Assert.Equal("Car 1", CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: 1));
        Assert.Equal("Car 2", CarNumberDisplay.Format(isLoco: false, freightNumberFromLoco: 2));
    }

    [Fact]
    public void FreightNumberFromLoco_works_when_loco_not_at_end()
    {
        var flags = new[] { false, false, true, false };
        Assert.Equal(2, CarNumberDisplay.FreightNumberFromLoco(2, 0, flags));
        Assert.Equal(1, CarNumberDisplay.FreightNumberFromLoco(2, 1, flags));
        Assert.Equal(1, CarNumberDisplay.FreightNumberFromLoco(2, 3, flags));
    }
}
