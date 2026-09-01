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
    public void FormatFiloWarm_matches_v1_shape()
    {
        Assert.Equal(
            "T2 limit filo: warm · spawn · plus=4 minus=3 raw=80 parsed=12 fotMs=9",
            PostedBoardTelemetry.FormatFiloWarm("spawn", 4, 3, 80, 12, 9));
    }

    [Fact]
    public void FormatFiloReverse_and_lock_match_v1_shape()
    {
        Assert.Equal(
            "T2 limit filo: reverse swap · plus=3 minus=4",
            PostedBoardTelemetry.FormatFiloReverse(3, 4));
        Assert.Equal(
            "T2 limit filo: direction lock · n=5",
            PostedBoardTelemetry.FormatFiloLock(5));
        Assert.Equal(
            "T2 limit filo: head +40@135 -40@-2",
            PostedBoardTelemetry.FormatFiloHead(40f, 135f, 40f, -2f));
    }

    [Fact]
    public void FormatAhead_lists_nearest_four_and_optional_skip()
    {
        var nearest = new[]
        {
            new AheadBoard(40f, 12f),
            new AheadBoard(40f, 135f),
        };
        Assert.Equal(
            "T2 limit-ahead: sticky=120 speed=0 next=40 135m src=path n=2 40@12 40@135 skip=40@12 left",
            PostedBoardTelemetry.FormatAhead(
                120f,
                0f,
                40f,
                135f,
                nearest,
                2,
                skipKmh: 40f,
                skipAlongMeters: 12f,
                skipReason: "left",
                alongSrc: "path"));
    }

    [Fact]
    public void FormatFiloTake_path_rebuild_and_along_jump_match_smoke()
    {
        Assert.Equal(
            "T2 limit filo: take 40@-2 src=chord",
            PostedBoardTelemetry.FormatFiloTake(40f, -2f, "chord"));
        Assert.Equal(
            "T2 limit filo: take 40@12 src=path",
            PostedBoardTelemetry.FormatFiloTake(40f, 12f, "path"));
        Assert.Equal(
            "T2 limit filo: take 60@80 src=path",
            PostedBoardTelemetry.FormatFiloTake(60f, 80f, "path"));
        Assert.Equal(
            "T2 limit filo: path rebuild · lost · hops=12 fotMs=2",
            PostedBoardTelemetry.FormatFiloPathRebuild("lost", 12, 2));
        Assert.Equal(
            "T2 limit filo: along jump 73→127",
            PostedBoardTelemetry.FormatFiloAlongJump(73f, 127f));
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

    [Fact]
    public void Smoke_observe_ignores_roster_count_only_change()
    {
        var cache = default(PostedLimitCache);
        var a = new PostedLimitSnapshot(50f, rosterCount: 3, nextKmh: 40f, nextAlongMeters: 100f);
        Assert.True(PostedLimitTelemetry.Observe(in a, ref cache, out _));

        var refillOnly = new PostedLimitSnapshot(50f, rosterCount: 5, nextKmh: 40f, nextAlongMeters: 100f);
        Assert.False(PostedLimitTelemetry.Observe(in refillOnly, ref cache, out _));
    }
}
