using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.2 Win 6 — Evaluate = Maps authority on live SW leave harvest.
/// Next 40 → Active 40 → Next 60; never Next=50. No cab.
/// </summary>
public class HtpLimitWalkTests
{
    private readonly PostedBoardHarvestSnapshot _snap;
    private readonly PathSegmentAlong[] _segments;
    private readonly ParsedPostedBoard[] _boards;

    public HtpLimitWalkTests()
    {
        _snap = HtpFixtures.LoadBoardsSw20260831();
        _segments = new PathSegmentAlong[_snap.Segments.Count];
        for (var i = 0; i < _snap.Segments.Count; i++)
        {
            _segments[i] = _snap.Segments[i];
        }

        _boards = new ParsedPostedBoard[_snap.Boards.Count];
        for (var i = 0; i < _snap.Boards.Count; i++)
        {
            _boards[i] = _snap.Boards[i];
        }
    }

    [Fact]
    public void Smoke_sw_leave_next_40_then_active_40_then_next_60_never_50()
    {
        var funnel = WarmLockedAtNose();

        PoseAtPathAbs(0f, out var sitX, out var sitZ, out var sitTx, out var sitTz);
        funnel.Evaluate(
            _boards,
            _segments,
            _segments.Length,
            sitX,
            0f,
            sitZ,
            sitTx,
            0f,
            sitTz,
            speedKmh: 20f);
        var sit = funnel.ToSnapshot();
        Assert.Null(sit.Kmh);
        Assert.Equal(40f, sit.NextKmh);
        Assert.NotEqual(50f, sit.NextKmh);

        var fortyAbs = PostedPathAheadGate.BoardAbsMeters(
            HtpFixtures.RequireBoard(in _snap, 1398156).X,
            HtpFixtures.RequireBoard(in _snap, 1398156).Z,
            _segments,
            _segments.Length);
        PoseAtPathAbs(fortyAbs + 8f, out var pastX, out var pastZ, out var pastTx, out var pastTz);
        funnel.Evaluate(
            _boards,
            _segments,
            _segments.Length,
            pastX,
            0f,
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

    [Fact]
    public void Win6_evaluate_sets_onPath_via_IsOnAnyCorridor_1396790_does_not_take()
    {
        var ghost = HtpFixtures.RequireBoard(in _snap, 1396790);
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            new[] { ghost },
            _snap.NoseX,
            0f,
            _snap.NoseZ,
            _snap.FwdX,
            0f,
            _snap.FwdZ);
        funnel.SetTravel(
            _snap.FwdX,
            0f,
            _snap.FwdZ,
            speedKmh: 20f,
            _snap.NoseX,
            0f,
            _snap.NoseZ);

        funnel.Evaluate(
            new[] { ghost },
            _segments,
            _segments.Length,
            _snap.NoseX,
            0f,
            _snap.NoseZ,
            _snap.FwdX,
            0f,
            _snap.FwdZ,
            speedKmh: 20f);

        Assert.Null(funnel.StickyKmh);
        Assert.Equal(0, funnel.Count);
    }

    private PostedLimitFunnel WarmLockedAtNose()
    {
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            _boards,
            _snap.NoseX,
            0f,
            _snap.NoseZ,
            _snap.FwdX,
            0f,
            _snap.FwdZ);
        funnel.SetTravel(
            _snap.FwdX,
            0f,
            _snap.FwdZ,
            speedKmh: 20f,
            _snap.NoseX,
            0f,
            _snap.NoseZ);
        return funnel;
    }

    private void PoseAtPathAbs(
        float absMeters,
        out float x,
        out float z,
        out float travelX,
        out float travelZ)
    {
        if (absMeters <= 0f)
        {
            x = _snap.NoseX;
            z = _snap.NoseZ;
            travelX = _snap.FwdX;
            travelZ = _snap.FwdZ;
            return;
        }

        var last = _segments.Length - 1;
        for (var i = 0; i < _segments.Length; i++)
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
                travelX = _snap.FwdX;
                travelZ = _snap.FwdZ;
                return;
            }

            x = seg.EntryX + ((seg.HintX / hintLen) * along);
            z = seg.EntryZ + ((seg.HintZ / hintLen) * along);
            travelX = seg.HintX / hintLen;
            travelZ = seg.HintZ / hintLen;
            return;
        }

        x = _snap.NoseX;
        z = _snap.NoseZ;
        travelX = _snap.FwdX;
        travelZ = _snap.FwdZ;
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
