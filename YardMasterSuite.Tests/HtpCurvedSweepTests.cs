using System;
using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.3 Win 5 cab FAIL (2.9.1.36): Next 40 counted 296 m → 13 m then froze;
/// Player.log has zero <c>limit filo: take</c> for the whole drive.
///
/// Every prior HTP fixture is a straight hop, so arc length equals chord length
/// and the projection in <see cref="PostedPathAheadGate.AlongOnTrack"/> is exact.
/// The shipped Unity walk is not: <c>TrackPathAhead</c> stores Bezier
/// <c>curve.length</c> as <see cref="PathSegmentAlong.LengthMeters"/> while the
/// hint is the straight In→Out chord. These walks rebuild that mismatch and
/// sweep the loco along real curve geometry instead of along the chord.
/// </summary>
public class HtpCurvedSweepTests
{
    private const int HopA = 101;
    private const int HopB = 102;
    private const int HopC = 103;

    /// <summary>
    /// Quarter-circle hop then a straight hop. Board sits just inside the
    /// straight hop, so its abs is built on an entry distance accumulated from
    /// hop A's <em>arc</em> while the loco's along can only ever reach hop A's
    /// <em>chord</em>.
    /// </summary>
    [Fact]
    public void Sweep_Curved_Hop_Takes_Forty_Instead_Of_Freezing_Next()
    {
        var corridor = CurvedCorridor.QuarterThenStraight(radius: 100f, straight: 200f);
        var segs = corridor.Segments;
        var forty = Board(1398156, 40f, corridor.PositionAt(corridor.TotalMeters * 0.75f), HopB);

        var funnel = Locked(corridor);
        var minRemaining = float.PositiveInfinity;
        for (var travelled = 0f; travelled <= corridor.TotalMeters; travelled += 5f)
        {
            var pose = corridor.PositionAt(travelled);
            var tangent = corridor.TangentAt(travelled);
            funnel.Evaluate(
                new[] { forty },
                segs,
                segs.Length,
                pose.X,
                0f,
                pose.Z,
                tangent.X,
                0f,
                tangent.Z,
                speedKmh: 25f,
                locoTrackId: corridor.TrackIdAt(travelled));
            var snap = funnel.ToSnapshot();
            if (snap.NextAlongMeters is float remaining)
            {
                minRemaining = Math.Min(minRemaining, remaining);
            }

            if (snap.Kmh is not null)
            {
                break;
            }
        }

        Assert.True(
            funnel.StickyKmh == 40f,
            "sweeping the whole corridor must take the 40; sticky="
                + (funnel.StickyKmh?.ToString("0") ?? "—")
                + " min remaining="
                + minRemaining.ToString("0")
                + " m (cab froze at 13 m)");
    }

    /// <summary>
    /// The cab ask itself: leave SW, take 40, then 60 becomes Next, then take 60.
    /// Thirty-plus builds have never shown the 60. Ordered, on curved rail.
    /// </summary>
    [Fact]
    public void Sweep_Takes_Forty_Then_Queues_And_Takes_Sixty()
    {
        var corridor = CurvedCorridor.CurveStraightCurve();
        var segs = corridor.Segments;
        var forty = Board(1398156, 40f, corridor.PositionAt(corridor.TotalMeters * 0.30f), corridor.TrackIdAt(corridor.TotalMeters * 0.30f));
        var sixty = Board(1402212, 60f, corridor.PositionAt(corridor.TotalMeters * 0.70f), corridor.TrackIdAt(corridor.TotalMeters * 0.70f));
        var roster = new[] { forty, sixty };

        var funnel = Locked(corridor);
        var sawFortyAsNext = false;
        var sawSixtyAsNextAfterTake = false;
        float? tookFirst = null;

        for (var travelled = 0f; travelled <= corridor.TotalMeters; travelled += 5f)
        {
            var pose = corridor.PositionAt(travelled);
            var tangent = corridor.TangentAt(travelled);
            funnel.Evaluate(
                roster,
                segs,
                segs.Length,
                pose.X,
                0f,
                pose.Z,
                tangent.X,
                0f,
                tangent.Z,
                speedKmh: 25f,
                locoTrackId: corridor.TrackIdAt(travelled));
            var snap = funnel.ToSnapshot();
            if (snap.Kmh is null && snap.NextKmh == 40f)
            {
                sawFortyAsNext = true;
            }

            if (snap.Kmh is float taken)
            {
                tookFirst ??= taken;
                if (tookFirst == 40f && snap.NextKmh == 60f)
                {
                    sawSixtyAsNextAfterTake = true;
                }
            }
        }

        Assert.True(sawFortyAsNext, "40 must be Next before it is passed");
        Assert.Equal(40f, tookFirst);
        Assert.True(sawSixtyAsNextAfterTake, "after take 40 the 60 must become Next");
        Assert.Equal(60f, funnel.StickyKmh);
    }

    /// <summary>
    /// The shipped path: loco span from the bogie, board spans cached at attach.
    /// This is the walk that must stay green for the cab to take 40 then 60.
    /// </summary>
    [Fact]
    public void Span_Sweep_Takes_Forty_Then_Sixty_On_Bends()
    {
        var corridor = CurvedCorridor.CurveStraightCurve();
        var segs = corridor.Segments;
        var fortyAt = corridor.TotalMeters * 0.30f;
        var sixtyAt = corridor.TotalMeters * 0.70f;
        var roster = new[]
        {
            Board(1398156, 40f, corridor.PositionAt(fortyAt), corridor.TrackIdAt(fortyAt))
                .WithTrackSpan(corridor.TrackIdAt(fortyAt), corridor.SpanAt(fortyAt)),
            Board(1402212, 60f, corridor.PositionAt(sixtyAt), corridor.TrackIdAt(sixtyAt))
                .WithTrackSpan(corridor.TrackIdAt(sixtyAt), corridor.SpanAt(sixtyAt)),
        };

        var funnel = Locked(corridor);
        var order = new List<float>();
        var sixtyQueuedAfterForty = false;
        for (var travelled = 0f; travelled <= corridor.TotalMeters; travelled += 5f)
        {
            var pose = corridor.PositionAt(travelled);
            var tangent = corridor.TangentAt(travelled);
            funnel.Evaluate(
                roster,
                segs,
                segs.Length,
                pose.X,
                0f,
                pose.Z,
                tangent.X,
                0f,
                tangent.Z,
                speedKmh: 25f,
                corridor.TrackIdAt(travelled),
                corridor.SpanAt(travelled));
            var snap = funnel.ToSnapshot();
            if (snap.Kmh is float sticky && (order.Count == 0 || order[order.Count - 1] != sticky))
            {
                order.Add(sticky);
            }

            if (snap.Kmh == 40f && snap.NextKmh == 60f)
            {
                sixtyQueuedAfterForty = true;
            }
        }

        Assert.Equal(new[] { 40f, 60f }, order);
        Assert.True(sixtyQueuedAfterForty, "60 must be Next while 40 is the held Limit");
    }

    /// <summary>
    /// Same sweep with no usable loco track id — <c>LocoTrackProbe</c> returns 0
    /// or an off-walk bogie track. Segment choice then falls back to the 12 m
    /// corridor scan, and a loco mid-curve is further off the chord than that.
    /// </summary>
    [Fact]
    public void Sweep_Curved_Hop_Takes_Forty_Without_Loco_Track_Id()
    {
        var corridor = CurvedCorridor.QuarterThenStraight(radius: 100f, straight: 200f);
        var segs = corridor.Segments;
        var forty = Board(1398156, 40f, corridor.PositionAt(corridor.TotalMeters * 0.75f), HopB);

        var funnel = Locked(corridor);
        var minRemaining = float.PositiveInfinity;
        for (var travelled = 0f; travelled <= corridor.TotalMeters; travelled += 5f)
        {
            var pose = corridor.PositionAt(travelled);
            var tangent = corridor.TangentAt(travelled);
            funnel.Evaluate(
                new[] { forty },
                segs,
                segs.Length,
                pose.X,
                0f,
                pose.Z,
                tangent.X,
                0f,
                tangent.Z,
                speedKmh: 25f,
                locoTrackId: 0);
            var snap = funnel.ToSnapshot();
            if (snap.NextAlongMeters is float remaining)
            {
                minRemaining = Math.Min(minRemaining, remaining);
            }

            if (snap.Kmh is not null)
            {
                break;
            }
        }

        Assert.True(
            funnel.StickyKmh == 40f,
            "corridor fallback must still take the 40; sticky="
                + (funnel.StickyKmh?.ToString("0") ?? "—")
                + " min remaining="
                + minRemaining.ToString("0")
                + " m");
    }

    /// <summary>
    /// A loco mid-curve must still resolve to the hop it is physically on.
    /// The 12 m corridor gate measures to the chord, and a bend departs from
    /// its own chord by far more than that.
    /// </summary>
    [Fact]
    public void Loco_Mid_Curve_Resolves_To_Its_Own_Hop_Without_Track_Id()
    {
        var corridor = CurvedCorridor.QuarterThenStraight(radius: 100f, straight: 200f);
        var segs = corridor.Segments;
        var mid = corridor.PositionAt(corridor.Segments[0].LengthMeters * 0.5f);
        var offChord = PostedPathAheadGate.AlongOnTrack(mid.X, mid.Z, in segs[0]);

        Assert.Equal(
            0,
            PostedPathAheadGate.SelectSegmentIndex(mid.X, mid.Z, segs, segs.Length, locoTrackId: 0));
        Assert.True(
            PostedPathAheadGate.IsOnCorridor(mid.X, mid.Z, in segs[0]),
            "mid-curve loco must count as on its own hop; chord along="
                + offChord.ToString("0.0")
                + " lateral gate="
                + PostedPathAheadGate.CorridorLateralMeters.ToString("0")
                + " m");
    }

    /// <summary>
    /// The freeze floor is exactly the arc-minus-chord surplus the loco can
    /// never project onto. Names the number so a regression reads as metres,
    /// not as a failed equality.
    /// </summary>
    [Fact]
    public void Curved_Hop_Along_Reaches_Full_Segment_Length()
    {
        var corridor = CurvedCorridor.QuarterThenStraight(radius: 100f, straight: 200f);
        var segs = corridor.Segments;
        var hopA = segs[0];
        var exit = corridor.PositionAt(hopA.LengthMeters);

        // Raw chord projection is short by the bow — that is the cab freeze.
        var chord = PostedPathAheadGate.AlongOnTrack(exit.X, exit.Z, in hopA);
        Assert.True(
            hopA.LengthMeters - chord > 10f,
            "fixture must actually bend; surplus="
                + (hopA.LengthMeters - chord).ToString("0.0")
                + " m");

        Assert.True(
            PostedPathAheadGate.TryAbsFromSpan(
                segs,
                segs.Length,
                HopA,
                corridor.SpanAt(hopA.LengthMeters),
                out var abs),
            "hop span must resolve to a route distance");
        Assert.InRange(abs, hopA.LengthMeters - 1f, hopA.LengthMeters + 1f);
    }

    private static PostedLimitFunnel Locked(CurvedCorridor corridor)
    {
        var start = corridor.PositionAt(0f);
        var tangent = corridor.TangentAt(0f);
        var funnel = new PostedLimitFunnel();
        funnel.Warm(
            Array.Empty<ParsedPostedBoard>(),
            start.X,
            0f,
            start.Z,
            tangent.X,
            0f,
            tangent.Z);
        funnel.SetTravel(tangent.X, 0f, tangent.Z, speedKmh: 25f, start.X, 0f, start.Z);
        return funnel;
    }

    private static ParsedPostedBoard Board(int id, float kmh, Point at, int trackId) =>
        new ParsedPostedBoard(
            id,
            at.X,
            0f,
            at.Z,
            0f,
            -1f,
            1f,
            0f,
            kmh,
            kmh,
            false,
            false,
            trackId);

    internal readonly struct Point
    {
        public Point(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }
    }

    /// <summary>
    /// Hop geometry the straight fixtures cannot express: arc length differs
    /// from the In→Out chord, exactly like a live <c>RailTrack.curve</c>.
    /// </summary>
    internal sealed class CurvedCorridor
    {
        private readonly Hop[] _hops;

        private CurvedCorridor(params Hop[] hops)
        {
            _hops = hops;
            var abs = 0f;
            var origin = new Point(0f, 0f);
            var heading = new Point(0f, 1f);
            for (var i = 0; i < hops.Length; i++)
            {
                hops[i].Seat(origin, heading);
                hops[i].EntryMeters = abs;
                abs += hops[i].ArcMeters;
                origin = hops[i].PositionAt(hops[i].ArcMeters);
                heading = hops[i].TangentAt(hops[i].ArcMeters);
            }

            TotalMeters = abs;
            Segments = Build();
        }

        public PathSegmentAlong[] Segments { get; }

        public float TotalMeters { get; }

        public static CurvedCorridor QuarterThenStraight(float radius, float straight) =>
            new CurvedCorridor(Hop.Arc(HopA, radius, quarterTurns: 1f), Hop.Straight(HopB, straight));

        /// <summary>Bend, tangent, bend — the SW leave shape the cab drives.</summary>
        public static CurvedCorridor CurveStraightCurve() =>
            new CurvedCorridor(
                Hop.Arc(HopA, radius: 120f, quarterTurns: 1f),
                Hop.Straight(HopB, 300f),
                Hop.Arc(HopC, radius: 150f, quarterTurns: -1f));

        /// <summary>True position after travelling <paramref name="meters"/> of rail.</summary>
        public Point PositionAt(float meters) => Locate(meters, out var hop, out var into)
            ? hop.PositionAt(into)
            : _hops[_hops.Length - 1].PositionAt(_hops[_hops.Length - 1].ArcMeters);

        public Point TangentAt(float meters) => Locate(meters, out var hop, out var into)
            ? hop.TangentAt(into)
            : _hops[_hops.Length - 1].TangentAt(_hops[_hops.Length - 1].ArcMeters);

        public int TrackIdAt(float meters) =>
            Locate(meters, out var hop, out _) ? hop.TrackId : _hops[_hops.Length - 1].TrackId;

        /// <summary>Arc metres from the hop's own in-end, i.e. a live Bezier span.</summary>
        public float SpanAt(float meters) => Locate(meters, out _, out var into) ? into : 0f;

        private bool Locate(float meters, out Hop hop, out float into)
        {
            for (var i = 0; i < _hops.Length; i++)
            {
                var candidate = _hops[i];
                var end = candidate.EntryMeters + candidate.ArcMeters;
                if (meters <= end || i == _hops.Length - 1)
                {
                    hop = candidate;
                    into = meters - candidate.EntryMeters;
                    return meters <= end;
                }
            }

            hop = _hops[_hops.Length - 1];
            into = hop.ArcMeters;
            return false;
        }

        private PathSegmentAlong[] Build()
        {
            var segs = new PathSegmentAlong[_hops.Length];
            for (var i = 0; i < _hops.Length; i++)
            {
                var hop = _hops[i];
                segs[i] = Segment(
                    hop.PositionAt(0f),
                    hop.PositionAt(hop.ArcMeters),
                    hop.ArcMeters,
                    hop.TrackId,
                    hop.EntryMeters);
            }

            return segs;
        }

        /// <summary>
        /// Mirrors <c>TrackPathAhead.ToAlong</c>: hint is the normalised chord,
        /// length is the Bezier arc.
        /// </summary>
        private static PathSegmentAlong Segment(
            Point entry,
            Point exit,
            float arcMeters,
            int trackId,
            float entryDistance)
        {
            var dx = exit.X - entry.X;
            var dz = exit.Z - entry.Z;
            var chord = (float)Math.Sqrt((dx * dx) + (dz * dz));
            return new PathSegmentAlong(
                entryDistance,
                entry.X,
                0f,
                entry.Z,
                dx / chord,
                dz / chord,
                arcMeters,
                trackId,
                travelIncreasingSpan: true,
                chordLengthMeters: chord);
        }

        /// <summary>Circular arc or straight, chained so each starts where the last ended.</summary>
        internal sealed class Hop
        {
            private readonly float _radius;
            private readonly float _turnSign;
            private Point _origin;
            private Point _heading;

            private Hop(int trackId, float radius, float turnSign, float arcMeters)
            {
                TrackId = trackId;
                _radius = radius;
                _turnSign = turnSign;
                ArcMeters = arcMeters;
            }

            public int TrackId { get; }

            public float ArcMeters { get; }

            public float EntryMeters { get; set; }

            public static Hop Arc(int trackId, float radius, float quarterTurns) =>
                new Hop(
                    trackId,
                    radius,
                    Math.Sign(quarterTurns),
                    (float)(radius * Math.PI * 0.5 * Math.Abs(quarterTurns)));

            public static Hop Straight(int trackId, float meters) =>
                new Hop(trackId, 0f, 0f, meters);

            public void Seat(Point origin, Point heading)
            {
                _origin = origin;
                _heading = heading;
            }

            public Point PositionAt(float into)
            {
                if (_turnSign == 0f)
                {
                    return new Point(
                        _origin.X + (_heading.X * into),
                        _origin.Z + (_heading.Z * into));
                }

                // Centre sits one radius off the heading normal; the entry point
                // then sweeps around it by the arc angle.
                var centreX = _origin.X + (-_heading.Z * _turnSign * _radius);
                var centreZ = _origin.Z + (_heading.X * _turnSign * _radius);
                var vx = _origin.X - centreX;
                var vz = _origin.Z - centreZ;
                var t = (into / _radius) * _turnSign;
                var cos = (float)Math.Cos(t);
                var sin = (float)Math.Sin(t);
                return new Point(
                    centreX + ((vx * cos) - (vz * sin)),
                    centreZ + ((vx * sin) + (vz * cos)));
            }

            public Point TangentAt(float into)
            {
                if (_turnSign == 0f)
                {
                    return _heading;
                }

                var t = (into / _radius) * _turnSign;
                var cos = (float)Math.Cos(t);
                var sin = (float)Math.Sin(t);
                return new Point(
                    (_heading.X * cos) - (_heading.Z * sin),
                    (_heading.X * sin) + (_heading.Z * cos));
            }
        }
    }
}
