using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.3 Win 2–3 — Core walks the sit-still SW graph. Path includes harvest 60.
/// No cab. Do not re-prove Evaluate 40→60 here.
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
        var n = CorePathfinder.BuildPath(
            tracks,
            junctions,
            locoX: 1f,
            locoZ: 0f,
            forwardX: 1f,
            forwardZ: 0f,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);
        Assert.True(n >= 2);
        Assert.True(PostedPathAheadGate.IsOnAnyCorridor(25f, 0f, segs, n));
        Assert.False(PostedPathAheadGate.IsOnAnyCorridor(10f, 20f, segs, n));
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
