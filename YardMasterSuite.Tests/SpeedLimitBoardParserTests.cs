using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitBoardParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("200")]
    public void ParseKmh_unknown_for_invalid(string? text)
    {
        Assert.Null(SpeedLimitBoardParser.ParseKmh(text));
    }

    [Theory]
    [InlineData("6", 60f)]
    [InlineData("8", 80f)]
    [InlineData("12", 120f)]
    [InlineData("1", 10f)]
    public void ParseKmh_digits_times_ten(string text, float expected)
    {
        Assert.Equal(expected, SpeedLimitBoardParser.ParseKmh(text));
    }

    [Theory]
    [InlineData("30", 30f)]
    [InlineData("60", 60f)]
    [InlineData("80", 80f)]
    [InlineData("100", 100f)]
    [InlineData("120", 120f)]
    public void ParseKmh_full_kmh_passthrough(string text, float expected)
    {
        Assert.Equal(expected, SpeedLimitBoardParser.ParseKmh(text));
    }

    [Fact]
    public void ParseKmh_slash_through_and_non_speed_second_line()
    {
        Assert.Equal(80f, SpeedLimitBoardParser.ParseKmh("8\nextra"));
        Assert.Equal(60f, SpeedLimitBoardParser.ParseKmh("6/4"));
    }

    [Fact]
    public void ParseDual_and_Pick_through_vs_diverge()
    {
        var dual = SpeedLimitBoardParser.ParseDual("6/4");
        Assert.NotNull(dual);
        Assert.True(dual!.Value.IsDual);
        Assert.Equal(60f, dual.Value.ThroughKmh);
        Assert.Equal(40f, dual.Value.DivergeKmh);
        Assert.Equal(60f, SpeedLimitBoardParser.Pick(dual.Value, diverging: false));
        Assert.Equal(40f, SpeedLimitBoardParser.Pick(dual.Value, diverging: true));
        Assert.Equal(60f, SpeedLimitBoardParser.Pick(dual.Value, selectedBranch: 0));
        Assert.Equal(40f, SpeedLimitBoardParser.Pick(dual.Value, selectedBranch: 1));
    }

    [Theory]
    [InlineData("3 4", 30f, 40f)]
    [InlineData("3\n4", 30f, 40f)]
    [InlineData("3\r\n4", 30f, 40f)]
    public void ParseDual_space_and_newline_as_switch(string text, float through, float diverge)
    {
        var dual = SpeedLimitBoardParser.ParseDual(text);
        Assert.NotNull(dual);
        Assert.True(dual!.Value.IsDual);
        Assert.Equal(through, dual.Value.ThroughKmh);
        Assert.Equal(diverge, dual.Value.DivergeKmh);
        Assert.True(SpeedLimitBoardParser.IsSwitchSign(text));
    }

    [Theory]
    [InlineData("4 -1.9", 40f)]
    [InlineData("6\n+1.2", 60f)]
    [InlineData("6 +2", 60f)]
    public void ParseDual_ignores_grade_annotation(string text, float through)
    {
        var dual = SpeedLimitBoardParser.ParseDual(text);
        Assert.NotNull(dual);
        Assert.False(dual!.Value.IsDual);
        Assert.Equal(through, dual.Value.ThroughKmh);
        Assert.False(SpeedLimitBoardParser.IsSwitchSign(text));
    }

    [Fact]
    public void IsSwitchSign_detects_dual_slash()
    {
        Assert.True(SpeedLimitBoardParser.IsSwitchSign("6/4"));
        Assert.False(SpeedLimitBoardParser.IsSwitchSign("6"));
        Assert.False(SpeedLimitBoardParser.IsSwitchSign("6\n+1.2"));
    }
}
