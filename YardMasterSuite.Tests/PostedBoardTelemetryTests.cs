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
}
