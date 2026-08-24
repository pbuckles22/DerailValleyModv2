using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>6.20 Preview Regular-edge math (v1 Bundle D).</summary>
public class PreviewEdgeDisplayTests
{
    [Fact]
    public void MetersRemaining_and_radius_helpers()
    {
        Assert.Equal(100f, PreviewEdgeDisplay.RadiusFromSqr(10_000f));
        Assert.Equal(30f, PreviewEdgeDisplay.DistanceFromSqr(900f));
        Assert.Equal(40f, PreviewEdgeDisplay.MetersRemaining(30f, 100f));
        Assert.Equal(-40f, PreviewEdgeDisplay.MetersRemaining(110f, 100f));
        Assert.Null(PreviewEdgeDisplay.MetersRemaining(null, 100f));
    }

    [Fact]
    public void Smoke_cab_inside_regular_edge_applies_30m_buffer()
    {
        Assert.Equal(30f, PreviewEdgeDisplay.SafetyBufferMeters);
        Assert.Equal(-25f, PreviewEdgeDisplay.MetersRemaining(95f, 100f));
    }

    [Fact]
    public void Format_preview_in_out_and_colors()
    {
        Assert.Equal("— Preview", PreviewEdgeDisplay.Format(null));
        Assert.Equal("Preview OUT", PreviewEdgeDisplay.Format(-1f));
        Assert.Equal("Preview 450m", PreviewEdgeDisplay.Format(450.4f));
        Assert.Contains(PreviewEdgeDisplay.WarningColor, PreviewEdgeDisplay.Format(100f, richText: true));
        Assert.Contains(PreviewEdgeDisplay.CriticalColor, PreviewEdgeDisplay.Format(10f, richText: true));
        Assert.Contains(PreviewEdgeDisplay.CriticalColor, PreviewEdgeDisplay.Format(-1f, richText: true));
    }

    [Fact]
    public void Smoke_hold_overview_formats_preview_180m()
    {
        Assert.Equal("Preview 180m", PreviewEdgeDisplay.Format(180f));
        Assert.Equal("Preview 180m", ActiveJobHudLine.FormatPreview("Preview 180m"));
    }

    [Fact]
    public void MostUrgent_picks_smallest_remaining()
    {
        float? best = null;
        PreviewEdgeDisplay.ConsiderUrgent(ref best, 400f);
        PreviewEdgeDisplay.ConsiderUrgent(ref best, 180f);
        PreviewEdgeDisplay.ConsiderUrgent(ref best, null);
        Assert.Equal(180f, best);
    }
}
