using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.2 Win 3 — geometry eligibility on live SW leave harvest. No Evaluate/FILO/Next.
/// </summary>
public class HtpEligibilityWalkTests
{
    private readonly PostedBoardHarvestSnapshot _snap;
    private readonly PathSegmentAlong[] _segments;

    public HtpEligibilityWalkTests()
    {
        _snap = HtpFixtures.LoadBoardsSw20260831();
        _segments = new PathSegmentAlong[_snap.Segments.Count];
        for (var i = 0; i < _snap.Segments.Count; i++)
        {
            _segments[i] = _snap.Segments[i];
        }
    }

    [Fact]
    public void Smoke_boards_sw_harvest_header_matches_gather()
    {
        Assert.Equal("SW", _snap.Origin);
        Assert.Equal(9, _snap.PathN);
        Assert.Equal(20, _snap.BoardN);
        Assert.Equal(3, _snap.DualN);
        Assert.Equal(5, _snap.FacingN);
        Assert.Equal(9, _snap.Segments.Count);
        Assert.Equal(20, _snap.Boards.Count);
    }

    [Fact]
    public void Board1398156_Exit40_IsOnCorridor_At12Meters()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1398156);
        Assert.Equal(40f, board.ThroughKmh);
        Assert.True(IsOnCorridor(board));
    }

    [Fact]
    public void Board1396774_Ghost60_IsOffCorridor()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1396774);
        Assert.Equal(60f, board.ThroughKmh);
        Assert.False(IsOnCorridor(board));
    }

    [Fact]
    public void Board1398162_Throat50_IsOnCorridor_AndIsDual()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1398162);
        Assert.True(IsOnCorridor(board));
        Assert.True(board.IsDual);
        Assert.True(board.JunctionNearby);
        Assert.Equal(50f, board.ThroughKmh);
        Assert.Equal(50f, board.DivergeKmh);
    }

    [Fact]
    public void Board1402212_Far60_IsOnCorridor_AndIsDual()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1402212);
        Assert.True(IsOnCorridor(board));
        Assert.True(board.IsDual);
        Assert.Equal(60f, board.ThroughKmh);
        Assert.Equal(40f, board.DivergeKmh);
    }

    private bool IsOnCorridor(in ParsedPostedBoard board) =>
        PostedPathAheadGate.IsOnAnyCorridor(
            board.X,
            board.Z,
            _segments,
            _segments.Length,
            PostedPathAheadGate.CorridorLateralMeters);
}
