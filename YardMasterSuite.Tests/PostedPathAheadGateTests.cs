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

    [Fact]
    public void ResolveAlong_and_LocoAbs_do_not_allocate()
    {
        var seg = new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 40f);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            var locoAbs = PostedPathAheadGate.LocoAbsMeters(0f, 0f, 20f + (i % 40), in seg);
            PostedPathAheadGate.ResolveAlong(15f, 12f, havePathAbs: true);
            PostedPathAheadGate.BoardRemaining(55f, locoAbs);
            PostedPathAheadGate.IsAlongJump(73f, 127f);
            PostedPathAheadGate.IsOnCorridor(0f, 20f, in seg);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
