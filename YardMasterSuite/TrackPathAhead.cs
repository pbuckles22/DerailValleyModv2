using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Route the consist will travel: walk forward through each junction as thrown
    /// and record how far along the path every track starts (v1 1.16 / 6.10).
    /// Distances use Bezier span (arc), not chord. Facing travel is the route
    /// tangent at the board.
    /// </summary>
    internal static class TrackPathAhead
    {
        public const int MaxHops = 128;

        /// <summary>Pre-sized path dictionary capacity (matches MaxHops).</summary>
        public const int PathDictionaryCapacity = MaxHops;

        internal readonly struct Segment
        {
            public Segment(
                float entryDistanceMeters,
                Vector3 entryPosition,
                float lengthMeters,
                bool travelIncreasingSpan,
                Vector3 travelHint,
                RailTrack track)
            {
                EntryDistanceMeters = entryDistanceMeters;
                EntryPosition = entryPosition;
                LengthMeters = lengthMeters;
                TravelIncreasingSpan = travelIncreasingSpan;
                TravelHint = travelHint;
                Track = track;
            }

            public float EntryDistanceMeters { get; }
            public Vector3 EntryPosition { get; }
            public float LengthMeters { get; }
            public bool TravelIncreasingSpan { get; }
            public Vector3 TravelHint { get; }
            public RailTrack Track { get; }
        }

        public static bool TryBuild(
            RailTrack? start,
            Vector3 locoPosition,
            Vector3 travelForward,
            float maxDistanceMeters,
            Dictionary<int, Segment> into)
        {
            into.Clear();
            if (start == null)
            {
                return false;
            }

            try
            {
                if (!TryEndpoints(start, out var startIn, out var startOut, out var startLength))
                {
                    return false;
                }

                var flat = new Vector3(travelForward.x, 0f, travelForward.z);
                var towardOut = Vector3.Dot(flat, Flat(startOut - locoPosition));
                var towardIn = Vector3.Dot(flat, Flat(startIn - locoPosition));
                var forward = towardOut >= towardIn;

                var track = start;
                var entryPosition = forward ? startIn : startOut;
                var length = startLength;
                var entryDistance = -StartTrackCoveredMeters(
                    start,
                    locoPosition,
                    length,
                    travelIncreasingSpan: forward);

                for (var hop = 0; hop < MaxHops; hop++)
                {
                    var id = track.GetInstanceID();
                    if (into.ContainsKey(id))
                    {
                        break;
                    }

                    var exitPosition = ExitPositionOf(track, entryPosition);
                    var hint = Flat(exitPosition - entryPosition);
                    if (hint.sqrMagnitude > 1e-8f)
                    {
                        hint.Normalize();
                    }
                    else
                    {
                        hint = Flat(flat).sqrMagnitude > 1e-8f ? Flat(flat).normalized : Vector3.forward;
                    }

                    into[id] = new Segment(
                        entryDistance,
                        entryPosition,
                        length,
                        travelIncreasingSpan: forward,
                        travelHint: hint,
                        track: track);

                    var exitDistance = entryDistance + length;
                    if (exitDistance >= maxDistanceMeters)
                    {
                        break;
                    }

                    var next = NextTrack(track, forward);
                    if (next == null || !TryEndpoints(next, out var nextIn, out var nextOut, out var nextLength))
                    {
                        break;
                    }

                    var enterAtIn = (nextIn - exitPosition).sqrMagnitude <= (nextOut - exitPosition).sqrMagnitude;
                    forward = enterAtIn;
                    entryPosition = enterAtIn ? nextIn : nextOut;
                    entryDistance = exitDistance;
                    length = nextLength;
                    track = next;
                }

                return into.Count > 0;
            }
            catch
            {
                into.Clear();
                return false;
            }
        }

        public static bool TrySample(
            Dictionary<int, Segment> path,
            RailTrack? boardTrack,
            Vector3 boardPosition,
            out float distanceMeters,
            out Vector3 travelForward)
        {
            distanceMeters = 0f;
            travelForward = Vector3.zero;
            if (boardTrack == null || !path.TryGetValue(boardTrack.GetInstanceID(), out var segment))
            {
                return false;
            }

            if (!TryClosestOnTrack(boardTrack, boardPosition, out var spanMeters, out var tangent))
            {
                var withinTrack = Vector3.Distance(segment.EntryPosition, boardPosition);
                if (segment.LengthMeters > 0f && withinTrack > segment.LengthMeters)
                {
                    withinTrack = segment.LengthMeters;
                }

                distanceMeters = segment.EntryDistanceMeters + withinTrack;
                travelForward = segment.TravelHint;
                return true;
            }

            var alongTrack = TrackPathSpan.WithinTrackMeters(
                spanMeters,
                segment.LengthMeters,
                segment.TravelIncreasingSpan);
            distanceMeters = segment.EntryDistanceMeters + alongTrack;
            travelForward = segment.TravelIncreasingSpan ? tangent : -tangent;
            travelForward.y = 0f;
            if (travelForward.sqrMagnitude < 1e-8f)
            {
                travelForward = segment.TravelHint;
            }

            return true;
        }

        /// <summary>
        /// Live junction branch fingerprint for switches on the cached corridor.
        /// Zero alloc when scratch is preallocated (Update tick).
        /// </summary>
        public static int ComputeJunctionFingerprint(
            Dictionary<int, Segment> path,
            JunctionBranchState[] scratch,
            out int scratchCount)
        {
            scratchCount = 0;
            if (path == null || path.Count == 0 || scratch == null || scratch.Length == 0)
            {
                return 0;
            }

            foreach (var segment in path.Values)
            {
                var track = segment.Track;
                if (track == null)
                {
                    continue;
                }

                TryAddJunction(track.outJunction, scratch, ref scratchCount);
                TryAddJunction(track.inJunction, scratch, ref scratchCount);
            }

            return PostedPathAheadGate.JunctionBranchFingerprint(scratch, scratchCount);
        }

        public static PathSegmentAlong ToAlong(in Segment segment) =>
            new PathSegmentAlong(
                segment.EntryDistanceMeters,
                segment.EntryPosition.x,
                segment.EntryPosition.y,
                segment.EntryPosition.z,
                segment.TravelHint.x,
                segment.TravelHint.z,
                segment.LengthMeters);

        private static void TryAddJunction(
            Junction? junction,
            JunctionBranchState[] scratch,
            ref int scratchCount)
        {
            if (junction == null)
            {
                return;
            }

            int id;
            int branch;
            try
            {
                id = junction.GetInstanceID();
                branch = junction.selectedBranch;
            }
            catch
            {
                return;
            }

            if (id == 0)
            {
                return;
            }

            for (var i = 0; i < scratchCount; i++)
            {
                if (scratch[i].JunctionId == id)
                {
                    return;
                }
            }

            if (scratchCount >= scratch.Length)
            {
                return;
            }

            scratch[scratchCount++] = new JunctionBranchState(id, branch);
        }

        private static float StartTrackCoveredMeters(
            RailTrack track,
            Vector3 locoPosition,
            float lengthMeters,
            bool travelIncreasingSpan)
        {
            if (TryClosestOnTrack(track, locoPosition, out var span, out _))
            {
                return TrackPathSpan.WithinTrackMeters(span, lengthMeters, travelIncreasingSpan);
            }

            if (!TryEndpoints(track, out var inPos, out var outPos, out _))
            {
                return 0f;
            }

            var entry = travelIncreasingSpan ? inPos : outPos;
            var covered = Vector3.Distance(entry, locoPosition);
            return lengthMeters > 0f && covered > lengthMeters ? lengthMeters : covered;
        }

        private static bool TryClosestOnTrack(
            RailTrack track,
            Vector3 worldPosition,
            out float spanMeters,
            out Vector3 tangent)
        {
            spanMeters = 0f;
            tangent = Vector3.zero;
            try
            {
                var closest = RailTrack.GetClosestPoint(track, worldPosition, 0f);
                if (closest.Item1 is not { } point)
                {
                    return false;
                }

                spanMeters = (float)point.span;
                tangent = point.forward;
                tangent.y = 0f;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static RailTrack? NextTrack(RailTrack track, bool forward)
        {
            var junction = forward ? track.outJunction : track.inJunction;
            if (junction != null)
            {
                var stem = junction.inBranch?.track;
                if (ReferenceEquals(stem, track))
                {
                    var branches = junction.outBranches;
                    if (branches == null || branches.Count == 0)
                    {
                        return null;
                    }

                    var index = junction.selectedBranch;
                    if (index < 0 || index >= branches.Count)
                    {
                        index = 0;
                    }

                    return branches[index]?.track;
                }

                return stem;
            }

            if (forward)
            {
                return track.outIsConnected ? track.outBranch?.track : null;
            }

            return track.inIsConnected ? track.inBranch?.track : null;
        }

        private static Vector3 ExitPositionOf(RailTrack track, Vector3 entryPosition)
        {
            if (!TryEndpoints(track, out var inPos, out var outPos, out _))
            {
                return entryPosition;
            }

            return (inPos - entryPosition).sqrMagnitude <= (outPos - entryPosition).sqrMagnitude
                ? outPos
                : inPos;
        }

        private static bool TryEndpoints(
            RailTrack track,
            out Vector3 inPosition,
            out Vector3 outPosition,
            out float lengthMeters)
        {
            inPosition = Vector3.zero;
            outPosition = Vector3.zero;
            lengthMeters = 0f;

            var curve = track.curve;
            if (curve == null || curve.pointCount < 2)
            {
                return false;
            }

            var first = curve[0];
            var last = curve[curve.pointCount - 1];
            if (first == null || last == null)
            {
                return false;
            }

            inPosition = first.position;
            outPosition = last.position;
            lengthMeters = curve.length;
            if (lengthMeters <= 0f)
            {
                lengthMeters = Vector3.Distance(inPosition, outPosition);
            }

            return lengthMeters > 0f;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
