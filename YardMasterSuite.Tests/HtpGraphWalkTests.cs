using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.3 Win 2–3 — Core walks the sit-still SW graph. Path includes harvest 60.
/// Win 5 — pooled track ids + junction fp for Unity tick (no cab here).
/// </summary>
public class HtpGraphWalkTests
{
    [Fact]
    public void Thrown_junction_follows_selected_branch()
    {
        var tracks = new[]
        {
            new CoreTrack(1, 0f, 0f, 10f, 0f, 10f),
            new CoreTrack(2, 10f, 0f, 10f, 8f, 8f),
            new CoreTrack(3, 10f, 0f, 25f, 0f, 15f),
        };
        var junctions = new[]
        {
            new CoreJunction(id: 9, stemId: 1, leftId: 2, rightId: 3, selectedBranch: 1),
        };
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var ids = new int[CorePathfinder.MaxHops];
        var visited = new int[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            tracks,
            junctions,
            locoX: 1f,
            locoZ: 0f,
            forwardX: 1f,
            forwardZ: 0f,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length,
            ids,
            visited);
        Assert.True(n >= 2);
        Assert.Equal(1, ids[0]);
        Assert.Equal(3, ids[1]);
        Assert.True(CorePathfinder.PathContainsTrack(ids, n, 1));
        Assert.True(CorePathfinder.PathContainsTrack(ids, n, 3));
        Assert.False(CorePathfinder.PathContainsTrack(ids, n, 2));
        Assert.True(PostedPathAheadGate.IsOnAnyCorridor(25f, 0f, segs, n));
        Assert.False(PostedPathAheadGate.IsOnAnyCorridor(10f, 20f, segs, n));
    }

    [Fact]
    public void Win5_core_junction_fp_changes_when_selected_branch_throws()
    {
        var thrown = new[]
        {
            new CoreJunction(id: 1003218, stemId: 1, leftId: 2, rightId: 3, selectedBranch: 1),
        };
        var stored = new[]
        {
            new CoreJunction(id: 1003218, stemId: 1, leftId: 2, rightId: 3, selectedBranch: 0),
        };
        var scratch = new JunctionBranchState[4];
        var thrownN = TrackGraphCore.CopyBranches(thrown, thrown.Length, scratch);
        var thrownFp = PostedPathAheadGate.JunctionBranchFingerprint(scratch, thrownN);
        var storedN = TrackGraphCore.CopyBranches(stored, stored.Length, scratch);
        var storedFp = PostedPathAheadGate.JunctionBranchFingerprint(scratch, storedN);
        Assert.True(PostedPathAheadGate.ShouldRebuildForThrow(storedFp, thrownFp, hasPath: true));
    }

    [Fact]
    public void Win5_pooled_unused_slots_do_not_steal_closest_track()
    {
        var pool = new CoreTrack[8];
        pool[0] = new CoreTrack(1, 500f, 500f, 510f, 500f, 10f);
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var ids = new int[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            pool,
            Array.Empty<CoreJunction>(),
            locoX: 500f,
            locoZ: 500f,
            forwardX: 1f,
            forwardZ: 0f,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length,
            ids,
            visitedScratch: null,
            trackCount: 1,
            juncCount: 0);
        Assert.Equal(1, n);
        Assert.Equal(1, ids[0]);
    }

    [Fact]
    public void BuildPath_StartTrackId_Wins_Over_Closer_Parallel()
    {
        var tracks = new[]
        {
            new CoreTrack(1, 0f, 0f, 40f, 0f, 40f),
            new CoreTrack(2, 0f, 2f, 40f, 2f, 40f),
        };
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var ids = new int[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            tracks,
            Array.Empty<CoreJunction>(),
            locoX: 10f,
            locoZ: 1.8f,
            forwardX: 1f,
            forwardZ: 0f,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length,
            ids,
            visitedScratch: null,
            trackCount: 0,
            juncCount: 0,
            startTrackId: 1);
        Assert.Equal(1, n);
        Assert.Equal(1, ids[0]);
    }

    [Fact]
    /// <summary>
    /// Entry distances accumulate Bezier arc, matching the live walk, and the
    /// chord is kept beside it for projection. This test previously locked the
    /// opposite (chord as length), which is what hid the cab freeze: a chord
    /// offset added to arc entry distances can never reach the next hop.
    /// </summary>
    public void BuildPath_EntryAbs_Uses_Bezier_Length_And_Keeps_Chord()
    {
        var tracks = new[]
        {
            new CoreTrack(1, 0f, 0f, 100f, 0f, 500f),
            new CoreTrack(2, 100f, 0f, 140f, 0f, 40f),
        };
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            tracks,
            Array.Empty<CoreJunction>(),
            locoX: 50f,
            locoZ: 0f,
            forwardX: 1f,
            forwardZ: 0f,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);
        Assert.True(n >= 2);
        Assert.InRange(segs[0].LengthMeters, 499f, 501f);
        Assert.InRange(segs[0].ChordLengthMeters, 99f, 101f);

        // Half the chord covered is half the arc covered: 250 m, not 50 m.
        Assert.InRange(segs[0].EntryDistanceMeters, -255f, -245f);
        Assert.InRange(segs[1].EntryDistanceMeters, 245f, 255f);
    }

    [Fact]
    public void Sit_still_sw_graph_walk_reaches_harvest_sixty_1402212()
    {
        var snap = HtpFixtures.LoadGraphSw20260901();
        var tracks = TrackGraphCore.Tracks(snap);
        var junctions = TrackGraphCore.Junctions(snap);
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            tracks,
            junctions,
            snap.LocoX,
            snap.LocoZ,
            snap.ForwardX,
            snap.ForwardZ,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);
        Assert.True(n > 0, "walker produced no segments");

        HarvestedGraphBoard? forty = null;
        HarvestedGraphBoard? sixty = null;
        HarvestedGraphBoard? throat = null;
        for (var i = 0; i < snap.Boards.Count; i++)
        {
            var b = snap.Boards[i];
            if (b.Id == 1398156)
            {
                forty = b;
            }

            if (b.Id == 1402212)
            {
                sixty = b;
            }

            if (b.Id == 1398162)
            {
                throat = b;
            }
        }

        Assert.True(forty.HasValue);
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(forty.Value.X, forty.Value.Z, segs, n),
            "walk must include harvest 40 1398156");
        Assert.True(sixty.HasValue);
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(sixty.Value.X, sixty.Value.Z, segs, n),
            "walk must include harvest 60 1402212");
        Assert.True(throat.HasValue);
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(throat.Value.X, throat.Value.Z, segs, n),
            "walk passes the 50/50 throat (skip is Evaluate, not routing)");
    }
}
