using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class FluidDisplayTests
{
    [Fact]
    public void PercentFromAmount_is_amount_over_capacity()
    {
        Assert.Null(FluidDisplay.PercentFromAmount(null, 100f));
        Assert.Null(FluidDisplay.PercentFromAmount(50f, 0f));
        Assert.Equal(80f, FluidDisplay.PercentFromAmount(80f, 100f));
        Assert.Equal(100f, FluidDisplay.PercentFromAmount(120f, 100f));
    }

    [Fact]
    public void PercentFromNormalized_scales_0_1_to_percent()
    {
        Assert.Null(FluidDisplay.PercentFromNormalized(null));
        Assert.Equal(80f, FluidDisplay.PercentFromNormalized(0.8f));
    }

    [Fact]
    public void Format_plain_shows_whole_percent()
    {
        Assert.Equal("— Fuel", FluidDisplay.FormatFuel(null));
        Assert.Equal("— Oil", FluidDisplay.FormatOil(null));
        Assert.Equal("Fuel 80 %", FluidDisplay.FormatFuel(80.4f));
        Assert.Equal("Oil 5 %", FluidDisplay.FormatOil(5.4f));
    }

    [Fact]
    public void FormatHud_pairs_yellow_when_either_fluid_is_low()
    {
        Assert.Equal("Fuel 80 %", FluidDisplay.FormatFuelHud(80f, 90f));
        Assert.Equal("Oil 90 %", FluidDisplay.FormatOilHud(80f, 90f));
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Fuel 80 %</color>",
            FluidDisplay.FormatFuelHud(80f, 19f));
        Assert.Equal(
            $"<color={FluidDisplay.WarningColor}>Oil 19 %</color>",
            FluidDisplay.FormatOilHud(80f, 19f));
    }

    [Fact]
    public void FormatHud_pairs_red_when_either_fluid_is_critical()
    {
        Assert.Equal(
            $"<color={FluidDisplay.CriticalColor}>Fuel 80 %</color>",
            FluidDisplay.FormatFuelHud(80f, 4f));
        Assert.Equal(
            $"<color={FluidDisplay.CriticalColor}>Oil 4 %</color>",
            FluidDisplay.FormatOilHud(80f, 4f));
    }

    [Fact]
    public void BucketPercent_whole_percent_or_unknown()
    {
        Assert.Equal(int.MinValue, FluidDisplay.BucketPercent(null));
        Assert.Equal(80, FluidDisplay.BucketPercent(80.4f));
        Assert.Equal("80", FluidDisplay.FormatPercentToken(80.4f));
        Assert.Equal("—", FluidDisplay.FormatPercentToken(null));
    }
}
