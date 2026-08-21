using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PostedBoardTelemetryTests
{
    [Fact]
    public void FormatFot_names_raw_and_parsed_counts()
    {
        Assert.Equal("T2 boards fot: raw=80 parsed=12", PostedBoardTelemetry.FormatFot(80, 12));
    }

    [Fact]
    public void Observe_dedupes_same_posted_kmh()
    {
        var cache = default(PostedLimitCache);
        var snap = new PostedLimitSnapshot(60f, rosterCount: 8);
        Assert.True(PostedLimitTelemetry.Observe(in snap, ref cache, out _));
        Assert.False(PostedLimitTelemetry.Observe(in snap, ref cache, out _));
    }

    [Fact]
    public void Observe_publishes_when_next_changes()
    {
        var cache = default(PostedLimitCache);
        var sticky = new PostedLimitSnapshot(80f, rosterCount: 8, nextKmh: 50f, nextAlongMeters: 800f);
        Assert.True(PostedLimitTelemetry.Observe(in sticky, ref cache, out _));
        Assert.False(PostedLimitTelemetry.Observe(in sticky, ref cache, out _));

        var closer = new PostedLimitSnapshot(80f, rosterCount: 8, nextKmh: 50f, nextAlongMeters: 50f);
        Assert.True(PostedLimitTelemetry.Observe(in closer, ref cache, out _));
    }
}
