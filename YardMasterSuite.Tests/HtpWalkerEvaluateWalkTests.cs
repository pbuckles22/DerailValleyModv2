using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.3 Win 4 — walker PathSegmentAlong[] into existing Evaluate.
/// Same gold as 9.1.2 Win 6: Next 40 → Active 40 → Next 60; never Next=50.
/// Pose on walker abs. Do not re-prove routing to 1402212; no cab.
/// </summary>
public class HtpWalkerEvaluateWalkTests
{
    private readonly TrackGraphHarvestSnapshot _snap;
    private readonly PathSegmentAlong[] _segments;
    private readonly int _segmentCount;
    private readonly ParsedPostedBoard[] _boards;

    public HtpWalkerEvaluateWalkTests()
    {
        _snap = HtpFixtures.LoadGraphSw20260901();
        _segments = new PathSegmentAlong[CorePathfinder.MaxHops];
        _segmentCount = CorePathfinder.BuildPath(
            TrackGraphCore.Tracks(_snap),
            TrackGraphCore.Junctions(_snap),
            _snap.LocoX,
            _snap.LocoZ,
            _snap.ForwardX,
            _snap.ForwardZ,
            CorePathfinder.LookaheadMeters,
            _segments,
            _segments.Length);
        _boards = TrackGraphCore.Boards(_snap);
    }

    [Fact]
    public void Smoke_walker_path_evaluate_next_40_then_active_40_then_next_60_never_50()
    {
        Assert.True(_segmentCount > 0, "walker produced no segments");
        var funnel = WarmLockedAtLoco();

        PoseAtPathAbs(0f, out var sitX, out var sitZ, out var sitTx, out var sitTz);
        funnel.Evaluate(
            _boards,
            _segments,
            _segmentCount,
            sitX,
            _snap.LocoY,
            sitZ,
            sitTx,
            0f,
            sitTz,
            speedKmh: 20f);
        var sit = funnel.ToSnapshot();
        Assert.Null(sit.Kmh);
        Assert.Equal(40f, sit.NextKmh);
        Assert.NotEqual(50f, sit.NextKmh);

        var forty = HtpFixtures.RequireGraphBoard(in _snap, 1398156);
        var fortyAbs = PostedPathAheadGate.BoardAbsMeters(
            forty.X,
            forty.Z,
            _segments,
            _segmentCount);
        PoseAtPathAbs(fortyAbs + 8f, out var pastX, out var pastZ, out var pastTx, out var pastTz);
        funnel.Evaluate(
            _boards,
            _segments,
            _segmentCount,
            pastX,
            _snap.LocoY,
            pastZ,
            pastTx,
            0f,
            pastTz,
            speedKmh: 20f);
        var past = funnel.ToSnapshot();
        Assert.Equal(40f, past.Kmh);
        Assert.Equal(60f, past.NextKmh);
        Assert.NotEqual(50f, past.NextKmh);
        Assert.Null(SlotKmh(funnel, 50f));
    }

    private PostedLimitFunnel WarmLockedAtLoco()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            _boards,
            _snap.LocoX,
            _snap.LocoY,
            _snap.LocoZ,
            _snap.ForwardX,
            0f,
            _snap.ForwardZ);
        funnel.SetTravel(
            _snap.ForwardX,
            0f,
            _snap.ForwardZ,
            speedKmh: 20f,
            _snap.LocoX,
            _snap.LocoY,
            _snap.LocoZ);
        return funnel;
    }

    private void PoseAtPathAbs(
        float absMeters,
        out float x,
        out float z,
        out float travelX,
        out float travelZ)
    {
        if (absMeters <= 0f || _segmentCount <= 0)
        {
            x = _snap.LocoX;
            z = _snap.LocoZ;
            travelX = _snap.ForwardX;
            travelZ = _snap.ForwardZ;
            return;
        }

        var last = _segmentCount - 1;
        for (var i = 0; i < _segmentCount; i++)
        {
            var seg = _segments[i];
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
                travelX = _snap.ForwardX;
                travelZ = _snap.ForwardZ;
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
        travelX = _snap.ForwardX;
        travelZ = _snap.ForwardZ;
    }

    private static float? SlotKmh(PostedLimitFunnel funnel, float kmh)
    {
        for (var i = 0; i < funnel.Count; i++)
        {
            var whole = (int)Math.Round(funnel.BoardAt(i).ThroughKmh, MidpointRounding.AwayFromZero);
            if (whole == (int)Math.Round(kmh, MidpointRounding.AwayFromZero))
            {
                return funnel.BoardAt(i).ThroughKmh;
            }
        }

        return null;
    }
}
