using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class DerailRiskDisplayTests
{
    [Fact]
    public void PercentOfBuildUp_null_when_unusable()
    {
        Assert.Null(DerailRiskDisplay.PercentOfBuildUp(null, null));
        Assert.Null(DerailRiskDisplay.PercentOfBuildUp(10f, null));
        Assert.Null(DerailRiskDisplay.PercentOfBuildUp(10f, 0.009f));
    }

    [Fact]
    public void PercentOfBuildUp_is_ratio_of_game_threshold()
    {
        Assert.Equal(40f, DerailRiskDisplay.PercentOfBuildUp(40f, 100f));
        Assert.Equal(100f, DerailRiskDisplay.PercentOfBuildUp(100f, 100f));
        Assert.Equal(200f, DerailRiskDisplay.PercentOfBuildUp(200f, 100f));
    }

    [Fact]
    public void Smoke_cab_always_shows_green_when_safe()
    {
        Assert.Equal(
            $"<color={DerailRiskDisplay.OkColor}>Derail Risk 0 %</color>",
            DerailRiskDisplay.FormatHud(0f));
        Assert.Equal(
            $"<color={DerailRiskDisplay.OkColor}>Derail Risk 14 %</color>",
            DerailRiskDisplay.FormatHud(14.4f));
        Assert.Equal("— Derail Risk", DerailRiskDisplay.FormatHud(null));
        Assert.DoesNotContain("Stress", DerailRiskDisplay.FormatHud(0f));
    }

    [Fact]
    public void Smoke_curve_over_15_shows_yellow_derail_risk()
    {
        Assert.Equal(
            $"<color={DerailRiskDisplay.WarningColor}>Derail Risk 15 %</color>",
            DerailRiskDisplay.FormatHud(15f));
        Assert.Equal(
            $"<color={DerailRiskDisplay.WarningColor}>Derail Risk 79 %</color>",
            DerailRiskDisplay.FormatHud(79f));
        Assert.Equal(
            $"<color={DerailRiskDisplay.WarningColor}>Derail Risk 94 %</color>",
            DerailRiskDisplay.FormatHud(94f));
    }

    [Fact]
    public void Smoke_nuclear_95_is_red()
    {
        Assert.Equal(
            $"<color={DerailRiskDisplay.CriticalColor}>Derail Risk 95 %</color>",
            DerailRiskDisplay.FormatHud(95f));
        Assert.Equal(
            $"<color={DerailRiskDisplay.CriticalColor}>Derail Risk 120 %</color>",
            DerailRiskDisplay.FormatHud(120f));
    }

    [Fact]
    public void Smoke_loco_de2_L061_trip_at_threshold_is_red_100()
    {
        var percent = DerailRiskDisplay.PercentOfBuildUp(0.6003609f, 0.6f);
        Assert.NotNull(percent);
        Assert.Equal(
            $"<color={DerailRiskDisplay.CriticalColor}>Derail Risk 100 %</color>",
            DerailRiskDisplay.FormatHud(percent));
    }

    [Fact]
    public void Format_plain_stays_on_when_safe()
    {
        Assert.Equal("— Derail Risk", DerailRiskDisplay.Format(null));
        Assert.Equal("Derail Risk 10 %", DerailRiskDisplay.Format(10f));
        Assert.Equal("Derail Risk 22 %", DerailRiskDisplay.Format(22.4f));
    }

    [Fact]
    public void BucketPercent_keeps_safe_band()
    {
        Assert.Equal(int.MinValue, DerailRiskDisplay.BucketPercent(null));
        Assert.Equal(0, DerailRiskDisplay.BucketPercent(0f));
        Assert.Equal(14, DerailRiskDisplay.BucketPercent(14.4f));
        Assert.Equal(15, DerailRiskDisplay.BucketPercent(15f));
        Assert.Equal("0", DerailRiskDisplay.FormatPercentToken(0f));
        Assert.Equal("12", DerailRiskDisplay.FormatPercentToken(12.4f));
        Assert.Equal("—", DerailRiskDisplay.FormatPercentToken(null));
    }

    [Fact]
    public void Smoke_wagon_88_beats_lead_12()
    {
        float? worst = null;
        DerailRiskDisplay.ConsiderMax(ref worst, 12f);
        DerailRiskDisplay.ConsiderMax(ref worst, null);
        DerailRiskDisplay.ConsiderMax(ref worst, 88f);
        DerailRiskDisplay.ConsiderMax(ref worst, 40f);
        Assert.Equal(88f, worst);
        Assert.Equal(
            $"<color={DerailRiskDisplay.WarningColor}>Derail Risk 88 %</color>",
            DerailRiskDisplay.FormatHud(worst));
    }

    [Fact]
    public void ConsiderMax_skips_null_and_keeps_first()
    {
        float? worst = null;
        DerailRiskDisplay.ConsiderMax(ref worst, null);
        Assert.Null(worst);
        DerailRiskDisplay.ConsiderMax(ref worst, 0f);
        Assert.Equal(0f, worst);
    }

    [Fact]
    public void ConsiderMax_does_not_allocate()
    {
        float? worst = 10f;
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            DerailRiskDisplay.ConsiderMax(ref worst, 12.4f);
            DerailRiskDisplay.ConsiderMax(ref worst, null);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void PercentOfBuildUp_does_not_allocate()
    {
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            DerailRiskDisplay.PercentOfBuildUp(40f, 100f);
            DerailRiskDisplay.BucketPercent(12.4f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
