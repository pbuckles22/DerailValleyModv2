using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ActiveJobHudLineTests
{
    [Fact]
    public void Smoke_taken_job_bar_shows_job_go_bonus()
    {
        Assert.Equal(
            "Job SM-FH-12  |  GO  |  Bonus 14:32",
            ActiveJobHudLine.Format("Job SM-FH-12", "GO", "Bonus 14:32"));
    }

    [Fact]
    public void Format_taken_job_is_job_and_bonus_only()
    {
        Assert.Equal(
            "Job SM-FH-12  |  Bonus 14:32",
            ActiveJobHudLine.Format("Job SM-FH-12", "Bonus 14:32"));
    }

    [Fact]
    public void FormatJobId_extra_count()
    {
        Assert.Equal("— Job", ActiveJobHudLine.FormatJobId(null, 0));
        Assert.Equal("Job SM-FH-12", ActiveJobHudLine.FormatJobId("SM-FH-12", 0));
        Assert.Equal("Job SM-FH-12 (+2)", ActiveJobHudLine.FormatJobId("SM-FH-12", 2));
    }

    [Fact]
    public void FormatPrep_omits_when_empty()
    {
        Assert.Null(ActiveJobHudLine.FormatPrep(null, null));
    }

    [Fact]
    public void IsCancelledState_abandoned_or_expired_only()
    {
        Assert.True(ActiveJobHudLine.IsCancelledState("Abandoned"));
        Assert.True(ActiveJobHudLine.IsCancelledState("Expired"));
        Assert.False(ActiveJobHudLine.IsCancelledState("InProgress"));
        Assert.False(ActiveJobHudLine.IsCancelledState("Completed"));
        Assert.False(ActiveJobHudLine.IsCancelledState(null));
    }
}
