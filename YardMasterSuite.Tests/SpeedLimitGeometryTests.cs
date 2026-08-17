using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// SignPlacer radius → posted-speed ladder (story 4.3). Same table as v1 / DVRouteManager.
/// </summary>
public class SpeedLimitGeometryTests
{
    [Theory]
    [InlineData(40f, 10f)]
    [InlineData(50f, 20f)]
    [InlineData(69f, 20f)]
    [InlineData(70f, 30f)]
    [InlineData(94f, 30f)]
    [InlineData(95f, 40f)]
    [InlineData(129f, 40f)]
    [InlineData(130f, 50f)]
    [InlineData(169f, 50f)]
    [InlineData(170f, 60f)]
    [InlineData(229f, 60f)]
    [InlineData(230f, 70f)]
    [InlineData(359f, 70f)]
    [InlineData(360f, 80f)]
    [InlineData(699f, 80f)]
    [InlineData(700f, 90f)]
    [InlineData(899f, 90f)]
    [InlineData(900f, 100f)]
    [InlineData(1199f, 100f)]
    [InlineData(1200f, 120f)]
    [InlineData(5000f, 120f)]
    public void MaxSpeedForMinRadius_matches_sign_placer_table(float minRadius, float expectedKmh)
    {
        Assert.Equal(expectedKmh, SpeedLimitGeometry.MaxSpeedForMinRadius(minRadius));
    }

    [Fact]
    public void MaxSpeedForMinRadius_unknown_when_non_finite_or_non_positive()
    {
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(float.NaN));
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(float.PositiveInfinity));
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(0f));
        Assert.Null(SpeedLimitGeometry.MaxSpeedForMinRadius(-1f));
    }
}
