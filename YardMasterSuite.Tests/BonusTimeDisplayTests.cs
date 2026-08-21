using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BonusTimeDisplayTests
{
    [Fact]
    public void RemainingSeconds_null_when_no_limit()
    {
        Assert.Null(BonusTimeDisplay.RemainingSeconds(null, 10f));
        Assert.Null(BonusTimeDisplay.RemainingSeconds(0f, 10f));
        Assert.Null(BonusTimeDisplay.RemainingSeconds(-1f, 10f));
    }

    [Fact]
    public void RemainingSeconds_subtracts_elapsed()
    {
        Assert.Equal(50f, BonusTimeDisplay.RemainingSeconds(100f, 50f));
        Assert.Equal(100f, BonusTimeDisplay.RemainingSeconds(100f, null));
    }

    [Fact]
    public void Format_clock_and_placeholder()
    {
        Assert.Equal("— Bonus", BonusTimeDisplay.Format(null));
        Assert.Equal("Bonus 0:00", BonusTimeDisplay.Format(0f));
        Assert.Equal("Bonus 1:05", BonusTimeDisplay.Format(65f));
        Assert.Equal("Bonus 1:02:03", BonusTimeDisplay.Format(3723f));
    }

    [Fact]
    public void Format_rich_warn_critical()
    {
        Assert.Contains(BonusTimeDisplay.WarningColor, BonusTimeDisplay.Format(120f, richText: true));
        Assert.Contains(BonusTimeDisplay.CriticalColor, BonusTimeDisplay.Format(30f, richText: true));
        Assert.Equal("Bonus 10:00", BonusTimeDisplay.Format(600f, richText: true));
    }
}
