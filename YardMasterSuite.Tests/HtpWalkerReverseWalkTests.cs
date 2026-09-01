using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.3 Win 5 — reverse/sit CorePathfinder. Cab windshield 60 must be on the
/// walker path; Next 40 remaining must move with the loco. Gate math stays put.
/// </summary>
public class HtpWalkerReverseWalkTests
{
    private readonly TrackGraphHarvestSnapshot _snap;
    private readonly CoreTrack[] _tracks;
    private readonly CoreJunction[] _junctions;
    private readonly ParsedPostedBoard[] _boards;
    private readonly HarvestedGraphBoard _forty;
    private readonly HarvestedGraphBoard _sixty;

    public HtpWalkerReverseWalkTests()
    {
        _snap = HtpFixtures.LoadGraphSw20260901();
        _tracks = TrackGraphCore.Tracks(_snap);
        _junctions = TrackGraphCore.Junctions(_snap);
        _boards = TrackGraphCore.Boards(_snap);
        _forty = HtpFixtures.RequireGraphBoard(in _snap, 1398156);
        _sixty = HtpFixtures.RequireGraphBoard(in _snap, 1402212);
    }

    [Fact]
    public void BuildPath_Stationary_Resolves_Start_Hop_Not_Zero()
    {
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            _snap.LocoX,
            _snap.LocoZ,
            forwardX: 0f,
            forwardZ: 0f,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);
        Assert.True(n > 0, "sit still must still pick a start hop");
    }

    [Fact]
    public void BuildPath_Reverse_Traversal_Finds_Windshield_60()
    {
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = BuildReverse(segs);
        Assert.True(n > 0, "reverse walker produced no segments");
        var last = segs[n - 1];
        var covered = last.EntryDistanceMeters + last.LengthMeters;
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n),
            "reverse walk must include windshield 60 1402212; covered="
                + covered.ToString("0")
                + " n="
                + n.ToString());
        Assert.True(
            covered > 1600f,
            "reverse walk must accumulate past the harvest 60 (1962 m class); covered="
                + covered.ToString("0"));
    }

    [Fact]
    public void Evaluate_Does_Not_Freeze_Next_40()
    {
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = BuildReverse(segs);
        Assert.True(n > 0, "reverse walker produced no segments");
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_forty.X, _forty.Z, segs, n),
            "reverse path must include harvest 40 1398156");
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n),
            "reverse path must include harvest 60 1402212");

        var fortyAbs = PostedPathAheadGate.BoardAbsMeters(_forty.X, _forty.Z, segs, n);
        var sixtyAbs = PostedPathAheadGate.BoardAbsMeters(_sixty.X, _sixty.Z, segs, n);
        PoseAt(segs, n, 0f, out var sitX, out var sitZ, out var sitTx, out var sitTz);
        var funnel = WarmLocked(sitTx, sitTz);

        funnel.Evaluate(_boards, segs, n, sitX, _snap.LocoY, sitZ, sitTx, 0f, sitTz, 20f);
        var sit = funnel.ToSnapshot();
        Assert.Equal(40f, sit.NextKmh);
        Assert.NotNull(sit.NextAlongMeters);
        var sitRemaining = sit.NextAlongMeters!.Value;

        PoseAt(segs, n, fortyAbs - 80f, out var midX, out var midZ, out var midTx, out var midTz);
        funnel.Evaluate(_boards, segs, n, midX, _snap.LocoY, midZ, midTx, 0f, midTz, 20f);
        var mid = funnel.ToSnapshot();
        Assert.Equal(40f, mid.NextKmh);
        Assert.NotNull(mid.NextAlongMeters);
        Assert.True(
            mid.NextAlongMeters.Value < sitRemaining - 20f,
            "Next 40 remaining must drop as loco abs updates; sit="
                + sitRemaining.ToString("0")
                + " mid="
                + mid.NextAlongMeters.Value.ToString("0"));
        Assert.InRange(mid.NextAlongMeters.Value, 20f, 140f);

        PoseAt(segs, n, sixtyAbs - 15f, out var nearX, out var nearZ, out var nearTx, out var nearTz);
        funnel.Evaluate(_boards, segs, n, nearX, _snap.LocoY, nearZ, nearTx, 0f, nearTz, 20f);
        var near = funnel.ToSnapshot();
        Assert.NotEqual(40f, near.NextKmh);
        Assert.Equal(60f, near.NextKmh ?? near.Kmh);
    }

    /// <summary>
    /// Cab 2.9.1.33: rebuild while rolling took 40@-97 then Evaluate n=0 (60 gone).
    /// Rebuilding from a pose still before the windshield 40 must keep 60 and
    /// must not take 40 yet.
    /// </summary>
    [Fact]
    public void Rebuild_Before_Forty_Does_Not_Take_Early_And_Keeps_Sixty()
    {
        var sitSegs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var sitN = BuildReverse(sitSegs);
        var fortyAbs = PostedPathAheadGate.BoardAbsMeters(_forty.X, _forty.Z, sitSegs, sitN);
        PoseAt(sitSegs, sitN, fortyAbs - 100f, out var x, out var z, out var tx, out var tz);
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var ids = new int[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            x,
            z,
            tx,
            tz,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length,
            ids,
            visitedScratch: null);
        Assert.True(n > 0, "rebuild produced no segments");
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n),
            "rolling rebuild must keep windshield 60; n=" + n.ToString());
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_forty.X, _forty.Z, segs, n),
            "rolling rebuild must keep windshield 40");

        var funnel = WarmLocked(tx, tz);
        funnel.Evaluate(_boards, segs, n, x, _snap.LocoY, z, tx, 0f, tz, 20f);
        var snap = funnel.ToSnapshot();
        Assert.Null(snap.Kmh);
        Assert.Equal(40f, snap.NextKmh);
        Assert.NotNull(snap.NextAlongMeters);
        Assert.True(
            snap.NextAlongMeters.Value > 20f,
            "must not take 40 while still ~100 m before it; remaining="
                + snap.NextAlongMeters.Value.ToString("0"));
    }

    /// <summary>
    /// Cab flip through 60: n=1 on the leave rail, 60 never queued. Start on
    /// harvest leave 980710 must still see 60 then take it.
    /// </summary>
    [Fact]
    public void Rebuild_On_Leave_Track_980710_Finds_Sixty_Then_Take()
    {
        CoreTrack? leave = null;
        for (var i = 0; i < _tracks.Length; i++)
        {
            if (_tracks[i].Id == 980710)
            {
                leave = _tracks[i];
                break;
            }
        }

        Assert.True(leave.HasValue);
        var fx = leave.Value.OutX - leave.Value.InX;
        var fz = leave.Value.OutZ - leave.Value.InZ;
        var mag = (float)Math.Sqrt((fx * fx) + (fz * fz));
        Assert.True(mag > 1e-4f);
        fx /= mag;
        fz /= mag;
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var ids = new int[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            _sixty.X,
            _sixty.Z,
            fx,
            fz,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length,
            ids,
            visitedScratch: null,
            trackCount: 0,
            juncCount: 0,
            startTrackId: 980710);
        Assert.True(n > 0);
        Assert.Equal(980710, ids[0]);
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n),
            "leave-track walk must include 60");

        var hintLen = (float)Math.Sqrt((segs[0].HintX * segs[0].HintX) + (segs[0].HintZ * segs[0].HintZ));
        Assert.True(hintLen > 1e-8f);
        var tx = segs[0].HintX / hintLen;
        var tz = segs[0].HintZ / hintLen;
        var funnel = WarmLocked(tx, tz);
        funnel.Evaluate(
            _boards,
            segs,
            n,
            _sixty.X,
            _snap.LocoY,
            _sixty.Z,
            tx,
            0f,
            tz,
            20f);
        var snap = funnel.ToSnapshot();
        Assert.Equal(60f, snap.Kmh ?? snap.NextKmh);
    }

    /// <summary>
    /// Cab 2.9.1.34: sit has 60@1960; rebuild at the windshield 40 drops it
    /// (Evaluate n=0 after take). Rolling walk from 1398156 must still reach
    /// 1402212.
    /// </summary>
    [Fact]
    public void BuildPath_From_Pose_At_Forty_Still_Reaches_1960_Sixty()
    {
        var dx = _sixty.X - _forty.X;
        var dz = _sixty.Z - _forty.Z;
        var mag = (float)Math.Sqrt((dx * dx) + (dz * dz));
        Assert.True(mag > 1e-4f);
        dx /= mag;
        dz /= mag;
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            _forty.X,
            _forty.Z,
            dx,
            dz,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);
        Assert.True(n > 0, "rebuild at 40 produced no segments");
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n),
            "rolling path from 40 must include 60 1402212; n=" + n.ToString());
    }

    /// <summary>
    /// After take 40 on a rolling rebuild, Next must be 60 (never 50).
    /// </summary>
    [Fact]
    public void Evaluate_After_Take_40_Queues_Next_60()
    {
        var dx = _sixty.X - _forty.X;
        var dz = _sixty.Z - _forty.Z;
        var mag = (float)Math.Sqrt((dx * dx) + (dz * dz));
        Assert.True(mag > 1e-4f);
        dx /= mag;
        dz /= mag;
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            _forty.X,
            _forty.Z,
            dx,
            dz,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);
        Assert.True(n > 0);
        Assert.True(PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n));
        var fortyAbs = PostedPathAheadGate.BoardAbsMeters(_forty.X, _forty.Z, segs, n);
        PoseAt(segs, n, fortyAbs + 8f, out var x, out var z, out var tx, out var tz);
        var funnel = WarmLocked(tx, tz);
        funnel.Evaluate(_boards, segs, n, x, _snap.LocoY, z, tx, 0f, tz, 20f);
        var snap = funnel.ToSnapshot();
        Assert.Equal(40f, snap.Kmh);
        Assert.Equal(60f, snap.NextKmh);
        Assert.NotEqual(50f, snap.NextKmh);
    }

    /// <summary>
    /// Cab n=1 on leave stem 980710: PreferProbe must not walk inbound.
    /// Mid-hop still has windshield 60 on corridor.
    /// </summary>
    [Fact]
    public void BuildPath_On_Leave_Hop_n1_Still_Has_Sixty()
    {
        CoreTrack? leave = null;
        for (var i = 0; i < _tracks.Length; i++)
        {
            if (_tracks[i].Id == 980710)
            {
                leave = _tracks[i];
                break;
            }
        }

        Assert.True(leave.HasValue);
        var t = leave.Value;
        var locoX = (t.InX + t.OutX) * 0.5f;
        var locoZ = (t.InZ + t.OutZ) * 0.5f;
        var fx = t.OutX - t.InX;
        var fz = t.OutZ - t.InZ;
        var mag = (float)Math.Sqrt((fx * fx) + (fz * fz));
        Assert.True(mag > 1e-4f);
        fx /= mag;
        fz /= mag;
        var segs = new PathSegmentAlong[CorePathfinder.MaxHops];
        var ids = new int[CorePathfinder.MaxHops];
        var n = CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            locoX,
            locoZ,
            fx,
            fz,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length,
            ids,
            visitedScratch: null,
            trackCount: 0,
            juncCount: 0,
            startTrackId: 980710);
        Assert.True(n > 0);
        Assert.Equal(980710, ids[0]);
        Assert.True(
            PostedPathAheadGate.IsOnAnyCorridor(_sixty.X, _sixty.Z, segs, n),
            "mid-curve leave hop must still include 60; n=" + n.ToString());
    }

    private int BuildReverse(PathSegmentAlong[] segs) =>
        CorePathfinder.BuildPath(
            _tracks,
            _junctions,
            _snap.LocoX,
            _snap.LocoZ,
            -_snap.ForwardX,
            -_snap.ForwardZ,
            CorePathfinder.LookaheadMeters,
            segs,
            segs.Length);

    private PostedLimitFunnel WarmLocked(float travelX, float travelZ)
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            _boards,
            _snap.LocoX,
            _snap.LocoY,
            _snap.LocoZ,
            travelX,
            0f,
            travelZ);
        funnel.SetTravel(
            travelX,
            0f,
            travelZ,
            speedKmh: 20f,
            _snap.LocoX,
            _snap.LocoY,
            _snap.LocoZ);
        return funnel;
    }

    private void PoseAt(
        PathSegmentAlong[] segs,
        int n,
        float absMeters,
        out float x,
        out float z,
        out float travelX,
        out float travelZ)
    {
        if (n <= 0)
        {
            x = _snap.LocoX;
            z = _snap.LocoZ;
            travelX = -_snap.ForwardX;
            travelZ = -_snap.ForwardZ;
            return;
        }

        if (absMeters <= 0f)
        {
            var hop0 = segs[0];
            x = _snap.LocoX;
            z = _snap.LocoZ;
            var h0 = (float)Math.Sqrt((hop0.HintX * hop0.HintX) + (hop0.HintZ * hop0.HintZ));
            if (h0 > 1e-8f)
            {
                travelX = hop0.HintX / h0;
                travelZ = hop0.HintZ / h0;
            }
            else
            {
                travelX = -_snap.ForwardX;
                travelZ = -_snap.ForwardZ;
            }

            return;
        }

        var last = n - 1;
        for (var i = 0; i < n; i++)
        {
            var seg = segs[i];
            var end = seg.EntryDistanceMeters + seg.LengthMeters;
            if (absMeters > end && i < last)
            {
                continue;
            }

            var along = absMeters - seg.EntryDistanceMeters;
            var hintLen = (float)Math.Sqrt((seg.HintX * seg.HintX) + (seg.HintZ * seg.HintZ));
            if (hintLen < 1e-8f)
            {
                x = seg.EntryX;
                z = seg.EntryZ;
                travelX = -_snap.ForwardX;
                travelZ = -_snap.ForwardZ;
                return;
            }

            x = seg.EntryX + ((seg.HintX / hintLen) * along);
            z = seg.EntryZ + ((seg.HintZ / hintLen) * along);
            travelX = seg.HintX / hintLen;
            travelZ = seg.HintZ / hintLen;
            return;
        }

        x = _snap.LocoX;
        z = _snap.LocoZ;
        travelX = -_snap.ForwardX;
        travelZ = -_snap.ForwardZ;
    }
}
