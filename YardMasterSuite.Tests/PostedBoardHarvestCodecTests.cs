using System;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class PostedBoardHarvestCodecTests
{
    [Fact]
    public void Roundtrip_sw_origin_path_and_boards()
    {
        var segs = new[]
        {
            new PathSegmentAlong(0f, 546.7f, 147.4f, 591.4f, 0.492f, -0.871f, 120f),
            new PathSegmentAlong(120f, 600f, 147f, 500f, 0.5f, -0.8f, 80f),
        };
        var boards = new[]
        {
            new ParsedPostedBoard(
                1398156, 550.1f, 147f, 600.2f, 0.1f, -0.9f, 0.9f, 0.1f, 40f, 40f, false, false),
            new ParsedPostedBoard(
                1398162, 555f, 147f, 595f, 0.2f, -0.9f, 0.9f, 0.2f, 50f, 50f, true, true),
            new ParsedPostedBoard(
                1402212, 700f, 148f, 400f, 0.3f, -0.8f, 0.8f, 0.3f, 60f, 40f, true, true),
        };

        var text = PostedBoardHarvestCodec.Format(
            origin: "SW",
            noseX: 546.7f,
            noseZ: 591.4f,
            fwdX: 0.492f,
            fwdZ: -0.871f,
            segments: segs,
            segmentCount: segs.Length,
            boards: boards,
            boardCount: boards.Length);

        Assert.StartsWith(PostedBoardHarvestCodec.Header, text);
        Assert.Contains("origin SW", text, StringComparison.Ordinal);
        Assert.Contains("pathN 2", text, StringComparison.Ordinal);
        Assert.Contains("boardN 3", text, StringComparison.Ordinal);
        Assert.Contains("dualN 2", text, StringComparison.Ordinal);

        Assert.True(PostedBoardHarvestCodec.TryParse(text, out var snap));
        Assert.Equal("SW", snap.Origin);
        Assert.Equal(2, snap.PathN);
        Assert.Equal(3, snap.BoardN);
        Assert.Equal(2, snap.DualN);
        Assert.Equal(546.7f, snap.NoseX, precision: 1);
        Assert.Equal(2, snap.Segments.Count);
        Assert.Equal(120f, snap.Segments[0].LengthMeters, precision: 1);
        Assert.Equal(3, snap.Boards.Count);
        Assert.Equal(1398156, snap.Boards[0].InstanceId);
        Assert.Equal(40f, snap.Boards[0].ThroughKmh);
        Assert.False(snap.Boards[0].IsDual);
        Assert.Equal(1398162, snap.Boards[1].InstanceId);
        Assert.True(snap.Boards[1].IsDual);
        Assert.True(snap.Boards[1].JunctionNearby);
        Assert.Equal(60f, snap.Boards[2].ThroughKmh);
        Assert.Equal(40f, snap.Boards[2].DivergeKmh);
    }

    [Fact]
    public void FacingN_counts_boards_facing_travel()
    {
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 100f),
        };
        // Board faces −Z while travel is +Z → forwardDot ≈ −1 → faces us.
        var boards = new[]
        {
            new ParsedPostedBoard(
                1, 1f, 0f, 50f, 0f, -1f, 1f, 0f, 40f, 40f, false, false),
            new ParsedPostedBoard(
                2, 1f, 0f, 60f, 0f, 1f, 1f, 0f, 60f, 60f, false, false),
        };

        var text = PostedBoardHarvestCodec.Format(
            origin: "SW",
            noseX: 0f,
            noseZ: 0f,
            fwdX: 0f,
            fwdZ: 1f,
            segments: segs,
            segmentCount: 1,
            boards: boards,
            boardCount: 2);

        Assert.True(PostedBoardHarvestCodec.TryParse(text, out var snap));
        Assert.Equal(1, snap.FacingN);
    }

    [Fact]
    public void ShouldWrite_once_when_maps_path_and_roster_ready()
    {
        Assert.True(PostedBoardHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            pathSegmentCount: 4,
            boardCount: 9));
        Assert.False(PostedBoardHarvestPolicy.ShouldWrite(
            alreadyWritten: true,
            mapsLeg: true,
            pathSegmentCount: 4,
            boardCount: 9));
        Assert.False(PostedBoardHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: false,
            pathSegmentCount: 4,
            boardCount: 9));
        Assert.False(PostedBoardHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            pathSegmentCount: 0,
            boardCount: 9));
        Assert.False(PostedBoardHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            pathSegmentCount: 4,
            boardCount: 0));
    }

    [Fact]
    public void TryParse_rejects_garbage()
    {
        Assert.False(PostedBoardHarvestCodec.TryParse(null, out _));
        Assert.False(PostedBoardHarvestCodec.TryParse("YMS-HARVEST 1\n", out _));
    }

    [Fact]
    public void TryParse_fail_closed_on_truncated_or_mismatched_counts()
    {
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ bad\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN x\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN 0\nboardN x\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN 0\nboardN 0\ndualN x\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN 0\nboardN 0\ndualN 0\nfacingN x\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN 1\nboardN 0\ndualN 0\nfacingN 0\nseg 1 2\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN 0\nboardN 1\ndualN 0\nfacingN 0\nboard 1 2\n", out _));
        Assert.False(PostedBoardHarvestCodec.TryParse(
            "YMS-BOARDS 1\nnoseXZ 1 2\nfwdXZ 0 1\npathN 1\nboardN 0\ndualN 0\nfacingN 0\n", out _));

        var emptyOk =
            "YMS-BOARDS 1\norigin SW\nnoseXZ 1 2\nfwdXZ 0 1\npathN 0\nboardN 0\ndualN 0\nfacingN 0\n";
        Assert.True(PostedBoardHarvestCodec.TryParse(emptyOk, out var snap));
        Assert.Equal("SW", snap.Origin);
        Assert.Equal(0, snap.PathN);
    }

    [Fact]
    public void Format_clamps_null_arrays_and_FacesAway()
    {
        var text = PostedBoardHarvestCodec.Format(
            null, 1f, 2f, 0f, 1f, null, 5, null, 5);
        Assert.Contains("pathN 0", text);
        Assert.Contains("boardN 0", text);

        var facing = new ParsedPostedBoard(
            1, 0f, 0f, 0f, 0f, -1f, 1f, 0f, 40f, 0f, false, false);
        Assert.True(PostedBoardHarvestCodec.FacesTravel(in facing, 0f, 1f));
        Assert.False(PostedBoardHarvestCodec.FacesAway(in facing, 0f, 1f));
        var away = new ParsedPostedBoard(
            2, 0f, 0f, 0f, 0f, 1f, 1f, 0f, 40f, 0f, true, true);
        Assert.True(PostedBoardHarvestCodec.FacesAway(in away, 0f, 1f));
    }
}
