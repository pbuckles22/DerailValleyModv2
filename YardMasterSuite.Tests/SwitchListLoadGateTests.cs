using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.13.2.5.22.13: SL-55 Load painted 7 rows (TT spin, no inbound/leave).
/// Engineer bible is 10 rows. Do not bind the stub.
/// </summary>
public class SwitchListLoadGateTests
{
    [Fact]
    public void Smoke_sl55_7_row_tt_spin_without_inbound_leave_must_not_bind()
    {
        var job = new JobSummary
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(7, steps!.Count);
        Assert.Contains("TT turn around", steps[0].Label);
        Assert.True(SwitchListLoadGate.ExpectsEngineerTenRow(job));
        Assert.False(SwitchListLoadGate.HasCompleteTurnAroundLegs(job));
        Assert.False(SwitchListLoadGate.ShouldBind(job, steps));
    }

    [Fact]
    public void Smoke_sl55_6_row_before_tt_inject_must_not_bind()
    {
        var job = new JobSummary
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.True(steps!.Count < SwitchListLoadGate.EngineerSl55StepCount);
        Assert.False(SwitchListLoadGate.ShouldBind(job, steps));
        Assert.Equal("T2 switch-list: wait inject TurnAround", SwitchListLoadGate.FormatWaitInject());
        Assert.Equal("T2 switch-list: wait graph", SwitchListLoadGate.FormatWaitGraph());
    }

    [Fact]
    public void Smoke_sl55_inject_tt_without_approach_fills_b4l_pivot()
    {
        var job = new JobSummary
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1775#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
            LoadTrackId = "SW-B4L",
            LoadCargoLabel = "Wood Chips",
        };
        Assert.True(SwitchListLoadGate.TryFillMissingEngineerLegs(job));
        Assert.Equal("SW-B4L", job.TurntablePivotTrackId);
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(13, steps!.Count);
        Assert.Equal(
            "Set Reverse · Past switch → SW-B4L until CLEARED",
            steps[0].Label);
        Assert.True(SwitchListLoadGate.ShouldBind(job, steps));
    }

    [Fact]
    public void Smoke_sl55_engineer_10_row_must_bind()
    {
        var job = Sl55Live();
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(13, steps!.Count);
        Assert.Equal(
            "Set Reverse · Past switch → SW-B4L until CLEARED",
            steps[0].Label);
        Assert.True(SwitchListLoadGate.ShouldBind(job, steps));
    }

    [Fact]
    public void Smoke_fh82_complete_tt_legs_may_bind_at_7()
    {
        var job = new JobSummary
        {
            JobId = "SW-FH-82",
            JobTypeLabel = "FH",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C1O",
            DestTrackId = "GF-D5I",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
        };
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        Assert.Equal(7, steps!.Count);
        Assert.False(SwitchListLoadGate.ExpectsEngineerTenRow(job));
        Assert.True(SwitchListLoadGate.ShouldBind(job, steps));
    }

    private static JobSummary Sl55Live() =>
        new()
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
            LoadTrackId = "SW-B4L",
            LoadCargoLabel = "Wood Chips",
        };
}
