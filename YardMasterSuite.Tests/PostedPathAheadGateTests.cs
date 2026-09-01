using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PostedPathAheadGateTests
{
    [Fact]
    public void PathStillValid_joint_hop_is_lookup_not_rebuild()
    {
        Assert.True(PostedPathAheadGate.PathStillValid(hasPath: true, locoTrackId: 42, locoTrackOnPath: true));
        Assert.False(PostedPathAheadGate.PathStillValid(hasPath: true, locoTrackId: 99, locoTrackOnPath: false));
        Assert.False(PostedPathAheadGate.PathStillValid(hasPath: false, locoTrackId: 42, locoTrackOnPath: true));
    }

    [Fact]
    public void ShouldRebuildForThrow_only_when_path_junction_branch_changes()
    {
        Assert.False(PostedPathAheadGate.ShouldRebuildForThrow(100, 100, hasPath: true));
        Assert.True(PostedPathAheadGate.ShouldRebuildForThrow(100, 200, hasPath: true));
        Assert.False(PostedPathAheadGate.ShouldRebuildForThrow(100, 200, hasPath: false));
        Assert.False(PostedPathAheadGate.ShouldRebuildForThrow(100, 0, hasPath: true));
    }

    [Fact]
    public void JunctionBranchFingerprint_xor_unique_junctions()
    {
        var a = new[]
        {
            new JunctionBranchState(10, 0),
            new JunctionBranchState(20, 1),
        };
        var b = new[]
        {
            new JunctionBranchState(10, 0),
            new JunctionBranchState(20, 0),
        };
        Assert.NotEqual(
            PostedPathAheadGate.JunctionBranchFingerprint(a, a.Length),
            PostedPathAheadGate.JunctionBranchFingerprint(b, b.Length));
    }

    [Fact]
    public void Smoke_rolling_loco_track_change_does_not_change_throw_fp()
    {
        var corridor = new[]
        {
            new JunctionBranchState(500, 1),
        };
        var fp = PostedPathAheadGate.JunctionBranchFingerprint(corridor, corridor.Length);
        Assert.False(PostedPathAheadGate.ShouldRebuildForThrow(fp, fp, hasPath: true));
    }

    [Fact]
    public void YardPollDue_at_one_second_not_every_frame()
    {
        Assert.True(PostedPathAheadGate.YardPollDue(now: 0f, lastPollAt: -1f));
        Assert.False(PostedPathAheadGate.YardPollDue(now: 0.5f, lastPollAt: 0f));
        Assert.True(PostedPathAheadGate.YardPollDue(now: 1.1f, lastPollAt: 0f));
    }

    [Fact]
    public void LocoAbsMeters_projects_on_travel_hint()
    {
        var seg = new PathSegmentAlong(
            entryDistanceMeters: -10f,
            entryX: 0f,
            entryY: 0f,
            entryZ: 0f,
            hintX: 0f,
            hintZ: 1f,
            lengthMeters: 100f);
        Assert.Equal(5f, PostedPathAheadGate.LocoAbsMeters(0f, 0f, 15f, in seg));
    }

    [Fact]
    public void BoardRemaining_shrinks_as_loco_rolls_forward()
    {
        Assert.Equal(12f, PostedPathAheadGate.BoardRemaining(boardAbsMeters: 50f, locoAbsMeters: 38f));
        Assert.Equal(-5f, PostedPathAheadGate.BoardRemaining(boardAbsMeters: 50f, locoAbsMeters: 55f));
    }

    [Fact]
    public void Smoke_sw_shack_40_next_metres_from_cached_abs()
    {
        var boardAbs = 52f;
        var sit = PostedPathAheadGate.BoardRemaining(boardAbs, locoAbsMeters: 40f);
        var roll = PostedPathAheadGate.BoardRemaining(boardAbs, locoAbsMeters: 48f);
        Assert.Equal(12f, sit);
        Assert.Equal(4f, roll);
    }

    [Fact]
    public void Smoke_sw_fh_82_overshoot_segment_end_lets_remaining_cross_zero()
    {
        var seg = new PathSegmentAlong(
            entryDistanceMeters: 0f,
            entryX: 0f,
            entryY: 0f,
            entryZ: 0f,
            hintX: 0f,
            hintZ: 1f,
            lengthMeters: 40f);
        var boardAbs = 55f;
        var atEnd = PostedPathAheadGate.BoardRemaining(
            boardAbs,
            PostedPathAheadGate.LocoAbsMeters(0f, 0f, 40f, in seg));
        Assert.Equal(15f, atEnd);

        var pastSign = PostedPathAheadGate.BoardRemaining(
            boardAbs,
            PostedPathAheadGate.LocoAbsMeters(0f, 0f, 56f, in seg));
        Assert.True(pastSign < 0f);
    }

    [Fact]
    public void Smoke_sw_fh_82_world_pass_wins_when_path_remaining_frozen()
    {
        Assert.Equal(
            -2f,
            PostedPathAheadGate.ResolveAlong(pathRemaining: 15f, chordAlong: -2f, havePathAbs: true));
        Assert.Equal(
            15f,
            PostedPathAheadGate.ResolveAlong(pathRemaining: 15f, chordAlong: 18f, havePathAbs: true));
        Assert.Equal(
            12f,
            PostedPathAheadGate.ResolveAlong(pathRemaining: 99f, chordAlong: 12f, havePathAbs: false));
    }

    [Fact]
    public void Smoke_sw_fh_82_left_corridor_rebuilds_path_not_fot()
    {
        Assert.True(PostedPathAheadGate.ShouldRebuildForPathLoss(hadPath: true, pathStillValid: false));
        Assert.False(PostedPathAheadGate.ShouldRebuildForPathLoss(hadPath: true, pathStillValid: true));
        Assert.False(PostedPathAheadGate.ShouldRebuildForPathLoss(hadPath: false, pathStillValid: false));
        Assert.True(
            PostedPathAheadGate.ShouldRetryPath(
                hasFiloWarm: true,
                hasPath: false,
                locoTrackId: 42,
                lastRetryTrackId: 7));
        Assert.False(
            PostedPathAheadGate.ShouldRetryPath(
                hasFiloWarm: true,
                hasPath: false,
                locoTrackId: 42,
                lastRetryTrackId: 42));
    }

    [Fact]
    public void Smoke_sw_fh_82_next_73_to_127_is_an_along_jump()
    {
        Assert.True(PostedPathAheadGate.IsAlongJump(73f, 127f));
        Assert.False(PostedPathAheadGate.IsAlongJump(73f, 61f));
        Assert.False(PostedPathAheadGate.IsAlongJump(0f, 135f));
    }

    [Fact]
    public void Smoke_9_1_parallel_board_is_not_on_corridor()
    {
        var rail = new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f);
        Assert.True(PostedPathAheadGate.IsOnCorridor(0f, 200f, in rail));
        Assert.False(PostedPathAheadGate.IsOnCorridor(20f, 200f, in rail));
        Assert.False(PostedPathAheadGate.IsOnAnyCorridor(20f, 200f, new[] { rail }, 1));
    }

    /// <summary>
    /// Win 1 / 9.1.2: exit 40 (1398156) sits ~11.7 m off a long chord — old 8 m gate skipped it.
    /// </summary>
    [Fact]
    public void Win1_corridor_12m_accepts_11_7m_lateral_rejects_beyond()
    {
        var rail = new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 400f);
        Assert.Equal(12f, PostedPathAheadGate.CorridorLateralMeters);
        Assert.True(PostedPathAheadGate.IsOnCorridor(11.7f, 200f, in rail));
        Assert.True(PostedPathAheadGate.IsOnCorridor(-11.7f, 200f, in rail));
        Assert.True(PostedPathAheadGate.IsOnCorridor(8.1f, 200f, in rail));
        Assert.False(PostedPathAheadGate.IsOnCorridor(12.1f, 200f, in rail));
        Assert.False(PostedPathAheadGate.IsOnCorridor(-12.1f, 200f, in rail));
    }

    /// <summary>9.1.2 Win 4 — symmetric 50/50 junction dual must not govern through travel.</summary>
    [Fact]
    public void Win4_symmetric_dual_through_skips_asymmetric_does_not()
    {
        var symmetric = new ParsedPostedBoard(
            1398162, 0f, 0f, 0f, 0f, -1f, 1f, 0f, 50f, 50f, isDual: true, junctionNearby: true);
        var asymmetric = new ParsedPostedBoard(
            1402212, 0f, 0f, 0f, 0f, -1f, 1f, 0f, 60f, 40f, isDual: true, junctionNearby: true);
        var single = new ParsedPostedBoard(
            1398156, 0f, 0f, 0f, 0f, -1f, 1f, 0f, 40f, 40f, isDual: false, junctionNearby: false);

        Assert.True(PostedPathAheadGate.ShouldSkipSymmetricDualThrough(symmetric, diverging: false));
        Assert.False(PostedPathAheadGate.ShouldSkipSymmetricDualThrough(symmetric, diverging: true));
        Assert.False(PostedPathAheadGate.ShouldSkipSymmetricDualThrough(asymmetric, diverging: false));
        Assert.False(PostedPathAheadGate.ShouldSkipSymmetricDualThrough(single, diverging: false));
    }

    /// <summary>
    /// 9.1.2 Win 5 — remaining = (boardAbs − locoAbs) × sign(travel · segment hint).
    /// </summary>
    [Fact]
    public void Win5_path_travel_polarity_flips_remaining_on_reverse()
    {
        Assert.Equal(1f, PostedPathAheadGate.PathTravelPolarity(0f, 1f, 0f, 1f));
        Assert.Equal(-1f, PostedPathAheadGate.PathTravelPolarity(0f, -1f, 0f, 1f));
        Assert.Equal(12f, PostedPathAheadGate.BoardRemaining(50f, 38f, 0f, 1f, 0f, 1f));
        Assert.Equal(-12f, PostedPathAheadGate.BoardRemaining(50f, 38f, 0f, -1f, 0f, 1f));
        Assert.Equal(12f, PostedPathAheadGate.BoardRemaining(50f, 38f));
    }

    /// <summary>
    /// 9.1.2 Win 5 — behind take needs same rail and TakeAheadMeters (~250 m).
    /// </summary>
    [Fact]
    public void Win5_behind_take_requires_same_rail_within_TakeAheadMeters()
    {
        Assert.Equal(250f, PostedBoardActiveRoster.TakeAheadMeters);
        Assert.True(PostedPathAheadGate.ShouldTakeBehind(0f, sameRail: true));
        Assert.True(PostedPathAheadGate.ShouldTakeBehind(-12f, sameRail: true));
        Assert.True(
            PostedPathAheadGate.ShouldTakeBehind(
                -PostedBoardActiveRoster.TakeAheadMeters,
                sameRail: true));
        Assert.False(
            PostedPathAheadGate.ShouldTakeBehind(
                -PostedBoardActiveRoster.TakeAheadMeters - 1f,
                sameRail: true));
        Assert.False(PostedPathAheadGate.ShouldTakeBehind(-12f, sameRail: false));
        Assert.False(PostedPathAheadGate.ShouldTakeBehind(12f, sameRail: true));
    }

    /// <summary>
    /// 9.1.2 Win 6 — swap/discard the current segment before LocoAbsMeters clamps
    /// alongOnTrack &lt; 0 to 0, so reverse remaining does not freeze.
    /// </summary>
    [Fact]
    public void Win6_select_segment_swaps_before_clamp_unfreezes_reverse()
    {
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 10f),
            new PathSegmentAlong(10f, 0f, 0f, 10f, 0f, 1f, 10f),
        };
        Assert.Equal(0, PostedPathAheadGate.SelectSegmentIndex(0f, 4f, segs, 2));
        Assert.Equal(1, PostedPathAheadGate.SelectSegmentIndex(0f, 15f, segs, 2));
        Assert.Equal(15f, PostedPathAheadGate.LocoAbsOnPath(0f, 0f, 15f, segs, 2));

        var beforeFirst = PostedPathAheadGate.LocoAbsOnPath(0f, 0f, -5f, segs, 2);
        Assert.Equal(-5f, beforeFirst);
        Assert.True(beforeFirst < PostedPathAheadGate.LocoAbsMeters(0f, 0f, -5f, in segs[0]));
    }

    [Fact]
    public void Win6_board_abs_projects_onto_on_corridor_segment()
    {
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 40f),
            new PathSegmentAlong(40f, 0f, 0f, 40f, 0f, 1f, 40f),
        };
        Assert.Equal(55f, PostedPathAheadGate.BoardAbsMeters(0f, 55f, segs, 2));
        Assert.Equal(12f, PostedPathAheadGate.BoardAbsMeters(0f, 12f, segs, 2));
    }

    [Fact]
    public void ResolveAlong_and_LocoAbs_do_not_allocate()
    {
        var seg = new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 40f);
        var segs = new[] { seg };
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            var locoAbs = PostedPathAheadGate.LocoAbsMeters(0f, 0f, 20f + (i % 40), in seg);
            PostedPathAheadGate.ResolveAlong(15f, 12f, havePathAbs: true);
            PostedPathAheadGate.BoardRemaining(55f, locoAbs);
            PostedPathAheadGate.BoardRemaining(55f, locoAbs, 0f, 1f, 0f, 1f);
            PostedPathAheadGate.PathTravelPolarity(0f, 1f, 0f, 1f);
            PostedPathAheadGate.ShouldTakeBehind(-1f, sameRail: true);
            PostedPathAheadGate.IsAlongJump(73f, 127f);
            PostedPathAheadGate.IsOnCorridor(0f, 20f, in seg);
            PostedPathAheadGate.AlongOnTrack(0f, 20f + (i % 40), in seg);
            PostedPathAheadGate.SelectSegmentIndex(0f, 20f + (i % 40), segs, 1);
            PostedPathAheadGate.LocoAbsOnPath(0f, 0f, 20f + (i % 40), segs, 1);
            PostedPathAheadGate.BoardAbsMeters(0f, 25f, segs, 1);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
