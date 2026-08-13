using System.Text;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Personal compass: Unity +Z = north, 16-point rose, no degrees (story 3.1).
/// </summary>
public class HeadingDisplayTests
{
    [Fact]
    public void Plus_Z_forward_is_north()
    {
        var degrees = HeadingDisplay.FromForward(x: 0f, z: 1f);

        Assert.Equal(0, HeadingDisplay.ToPointIndex(degrees));
        Assert.Equal("N", HeadingDisplay.PointName(0));
    }

    [Fact]
    public void Plus_X_forward_is_east()
    {
        var degrees = HeadingDisplay.FromForward(x: 1f, z: 0f);

        Assert.Equal(4, HeadingDisplay.ToPointIndex(degrees));
        Assert.Equal("E", HeadingDisplay.PointName(4));
    }

    [Fact]
    public void Northeast_45_is_NE()
    {
        var degrees = HeadingDisplay.FromForward(x: 1f, z: 1f);

        Assert.Equal(2, HeadingDisplay.ToPointIndex(degrees));
        Assert.Equal("NE", HeadingDisplay.PointName(2));
    }

    [Fact]
    public void Zero_vector_is_unknown()
    {
        Assert.Null(HeadingDisplay.FromForward(0f, 0f));
        Assert.Equal(HeadingDisplay.UnknownIndex, HeadingDisplay.ToPointIndex(null));
        Assert.Null(HeadingDisplay.PointName(HeadingDisplay.UnknownIndex));
    }

    [Fact]
    public void AppendLabel_writes_Heading_point()
    {
        var sb = new StringBuilder();
        HeadingDisplay.AppendLabel(sb, 2);

        Assert.Equal("Heading NE", sb.ToString());
    }

    [Fact]
    public void AppendLabel_unknown_is_em_dash_Heading()
    {
        var sb = new StringBuilder();
        HeadingDisplay.AppendLabel(sb, HeadingDisplay.UnknownIndex);

        Assert.Equal("— Heading", sb.ToString());
    }
}
