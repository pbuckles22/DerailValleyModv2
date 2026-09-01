using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.2 Win 3 — geometry eligibility on live SW leave harvest. No Evaluate/FILO/Next.
/// Win 4 — symmetric dual through must not govern.
/// Win 5 — polarity remaining + same-rail behind-take (1396790).
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

    [Fact]
    public void Board1398162_Symmetric50Dual_MustSkipThroughGovernance()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1398162);
        Assert.True(PostedPathAheadGate.ShouldSkipSymmetricDualThrough(board, diverging: false));
    }

    [Fact]
    public void Board1402212_Asymmetric6040Dual_MustNotSkipThroughGovernance()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1402212);
        Assert.False(PostedPathAheadGate.ShouldSkipSymmetricDualThrough(board, diverging: false));
    }

    [Fact]
    public void Board1396790_Throat50_IsOffCorridor()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1396790);
        Assert.Equal(50f, board.ThroughKmh);
        Assert.True(board.IsDual);
        Assert.False(IsOnCorridor(board));
    }

    /// <summary>
    /// 9.1.2 Win 5 — far throat 50 cannot take: off-rail, reverse remaining is ahead,
    /// and |chord| exceeds TakeAheadMeters.
    /// </summary>
    [Fact]
    public void Board1396790_ReversePolarity_DoesNotQualifyBehindTake()
    {
        var board = HtpFixtures.RequireBoard(in _snap, 1396790);
        var sameRail = IsOnCorridor(board);
        var locoSeg = _segments[0];
        var remainingFwd = PostedLimitFilo.AlongMeters(
            _snap.NoseX,
            0f,
            _snap.NoseZ,
            _snap.FwdX,
            0f,
            _snap.FwdZ,
            board);
        var remainingRev = remainingFwd * PostedPathAheadGate.PathTravelPolarity(
            -_snap.FwdX,
            -_snap.FwdZ,
            locoSeg.HintX,
            locoSeg.HintZ);

        Assert.False(sameRail);
        Assert.Equal(
            -1f,
            PostedPathAheadGate.PathTravelPolarity(
                -_snap.FwdX,
                -_snap.FwdZ,
                locoSeg.HintX,
                locoSeg.HintZ));
        Assert.True(remainingFwd < -PostedBoardActiveRoster.TakeAheadMeters);
        Assert.True(remainingRev > 0f);
        Assert.False(PostedPathAheadGate.ShouldTakeBehind(remainingFwd, sameRail));
        Assert.False(PostedPathAheadGate.ShouldTakeBehind(remainingRev, sameRail));
    }

    private bool IsOnCorridor(in ParsedPostedBoard board) =>
        PostedPathAheadGate.IsOnAnyCorridor(
            board.X,
            board.Z,
            _segments,
            _segments.Length,
            PostedPathAheadGate.CorridorLateralMeters);
}
