using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class JobConsistStatusEvalTests
{
    [Theory]
    [InlineData(3, 0, 0, JobConsistStatus.Missing)]
    [InlineData(3, 0, 2, JobConsistStatus.Missing)]
    [InlineData(0, 0, 0, JobConsistStatus.Missing)]
    [InlineData(3, 3, 0, JobConsistStatus.Ready)]
    [InlineData(3, 3, 1, JobConsistStatus.Hold)]
    [InlineData(3, 2, 0, JobConsistStatus.Hold)]
    [InlineData(3, 1, 1, JobConsistStatus.Hold)]
    public void Evaluate_maps_counts(
        int expected,
        int attached,
        int foreign,
        JobConsistStatus want)
    {
        Assert.Equal(want, JobConsistStatusEval.Evaluate(expected, attached, foreign));
    }

    /// <summary>
    /// Simulator gate adjacency for <b>13.2.6</b>: consist ⊆ task cars → Ready
    /// (Prep complete / Validate arm input). Missing car → stay Hold/Missing.
    /// </summary>
    [Fact]
    public void Smoke_13_2_6_prep_complete_when_all_task_cars_attached_no_foreign()
    {
        Assert.Equal(JobConsistStatus.Ready, JobConsistStatusEval.Evaluate(2, 2, 0));
        Assert.Equal(JobConsistStatus.Hold, JobConsistStatusEval.Evaluate(2, 1, 0));
        Assert.Equal(JobConsistStatus.Missing, JobConsistStatusEval.Evaluate(2, 0, 0));
    }

    /// <summary>
    /// Simulator gate adjacency for <b>13.3</b>: match → Ready (haul GO may arm);
    /// foreign or incomplete → fail-closed Hold/Missing (no GO).
    /// </summary>
    [Fact]
    public void Smoke_13_3_validate_ready_only_when_match_no_foreign()
    {
        Assert.Equal(JobConsistStatus.Ready, JobConsistStatusEval.Evaluate(3, 3, 0));
        Assert.Equal(JobConsistStatus.Hold, JobConsistStatusEval.Evaluate(3, 3, 1));
        Assert.Equal(JobConsistStatus.Hold, JobConsistStatusEval.Evaluate(3, 2, 0));
    }

    [Fact]
    public void Evaluate_clamps_negatives_and_over_attached()
    {
        Assert.Equal(JobConsistStatus.Missing, JobConsistStatusEval.Evaluate(-1, -2, -3));
        Assert.Equal(JobConsistStatus.Ready, JobConsistStatusEval.Evaluate(2, 9, 0));
    }
}

public class JobConsistStatusDisplayTests
{
    [Fact]
    public void Format_plain_and_hud_colors()
    {
        Assert.Equal("GO", JobConsistStatusDisplay.Format(JobConsistStatus.Ready));
        Assert.Equal("HOLD", JobConsistStatusDisplay.Format(JobConsistStatus.Hold));
        Assert.Equal("RED", JobConsistStatusDisplay.Format(JobConsistStatus.Missing));

        Assert.Contains(JobConsistStatusDisplay.GoColor, JobConsistStatusDisplay.FormatHud(JobConsistStatus.Ready));
        Assert.Contains(JobConsistStatusDisplay.HoldColor, JobConsistStatusDisplay.FormatHud(JobConsistStatus.Hold));
        Assert.Contains(JobConsistStatusDisplay.RedColor, JobConsistStatusDisplay.FormatHud(JobConsistStatus.Missing));
        Assert.Contains("GO", JobConsistStatusDisplay.FormatHud(JobConsistStatus.Ready));
    }
}
