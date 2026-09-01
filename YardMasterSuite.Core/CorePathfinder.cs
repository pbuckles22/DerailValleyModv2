using System;

namespace YardMasterSuite.Core;

/// <summary>
/// 9.1.3 Win 2 — walk dumped tracks + thrown junctions from the loco.
/// Allocates visited when the Unity tick pool is omitted; Win 5 passes pooled buffers.
/// </summary>
public static class CorePathfinder
{
    public const int MaxHops = 128;
    public const float LookaheadMeters = PostedBoardActiveRoster.LookaheadMinMeters;
    public const float ConnectMeters = 1.25f;

    public static int BuildPath(
        CoreTrack[]? tracks,
        CoreJunction[]? junctions,
        float locoX,
        float locoZ,
        float forwardX,
        float forwardZ,
        float maxDistanceMeters,
        PathSegmentAlong[]? into,
        int intoLength) =>
        BuildPath(
            tracks,
            junctions,
            locoX,
            locoZ,
            forwardX,
            forwardZ,
            maxDistanceMeters,
            into,
            intoLength,
            trackIds: null,
            visitedScratch: null);

    /// <summary>Win 5 — pooled track ids + visited. HTP may pass null (allocates visited).</summary>
    public static int BuildPath(
        CoreTrack[]? tracks,
        CoreJunction[]? junctions,
        float locoX,
        float locoZ,
        float forwardX,
        float forwardZ,
        float maxDistanceMeters,
        PathSegmentAlong[]? into,
        int intoLength,
        int[]? trackIds,
        int[]? visitedScratch,
        int trackCount = 0,
        int juncCount = 0,
        int startTrackId = 0)
    {
        if (tracks == null || tracks.Length == 0 || into == null || intoLength <= 0)
        {
            return 0;
        }

        var juncs = junctions ?? Array.Empty<CoreJunction>();
        var trackN = ClampCount(tracks.Length, trackCount);
        var juncN = ClampCount(juncs.Length, juncCount);
        var maxHops = intoLength < MaxHops ? intoLength : MaxHops;
        if (maxDistanceMeters <= 0f)
        {
            maxDistanceMeters = LookaheadMeters;
        }

        if (!TryStartTrack(tracks, trackN, locoX, locoZ, startTrackId, out var startIndex))
        {
            return 0;
        }

        var visited = visitedScratch != null && visitedScratch.Length >= maxHops
            ? visitedScratch
            : new int[maxHops];
        var start = tracks[startIndex];
        WalkOnce(
            tracks,
            trackN,
            juncs,
            juncN,
            in start,
            towardOut: true,
            locoX,
            locoZ,
            forwardX,
            forwardZ,
            maxDistanceMeters,
            into: null,
            intoLength: 0,
            trackIds: null,
            visited,
            maxHops,
            out var probeOut);
        WalkOnce(
            tracks,
            trackN,
            juncs,
            juncN,
            in start,
            towardOut: false,
            locoX,
            locoZ,
            forwardX,
            forwardZ,
            maxDistanceMeters,
            into: null,
            intoLength: 0,
            trackIds: null,
            visited,
            maxHops,
            out var probeIn);
        var towardOut = PreferProbe(in probeOut, in probeIn);
        return WalkOnce(
            tracks,
            trackN,
            juncs,
            juncN,
            in start,
            towardOut,
            locoX,
            locoZ,
            forwardX,
            forwardZ,
            maxDistanceMeters,
            into,
            intoLength,
            trackIds,
            visited,
            maxHops,
            out _);
    }

    /// <summary>
    /// Reverse travel still has to walk the thrown leave, not a 3 km inbound
    /// bezier. Giant hops lose; otherwise keep the start that matches travel.
    /// </summary>
    private static bool PreferProbe(in WalkProbe towardOut, in WalkProbe towardIn)
    {
        if (towardOut.Giant != towardIn.Giant)
        {
            return !towardOut.Giant;
        }

        if (towardOut.Align != towardIn.Align)
        {
            return towardOut.Align > towardIn.Align;
        }

        return towardOut.Hops >= towardIn.Hops;
    }

    private static int WalkOnce(
        CoreTrack[] tracks,
        int trackN,
        CoreJunction[] juncs,
        int juncN,
        in CoreTrack start,
        bool towardOut,
        float locoX,
        float locoZ,
        float forwardX,
        float forwardZ,
        float maxDistanceMeters,
        PathSegmentAlong[]? into,
        int intoLength,
        int[]? trackIds,
        int[] visited,
        int maxHops,
        out WalkProbe probe)
    {
        var visitedN = 0;
        var track = start;
        var entryX = towardOut ? track.InX : track.OutX;
        var entryZ = towardOut ? track.InZ : track.OutZ;
        var covered = AlongArc(in track, locoX, locoZ, towardOut);
        var entryAbs = -covered;
        var count = 0;
        var giant = false;
        var align = 0f;
        var first = true;
        var write = into != null && intoLength > 0;
        var cap = write ? (intoLength < into!.Length ? intoLength : into.Length) : maxHops;
        if (cap > maxHops)
        {
            cap = maxHops;
        }

        for (var hop = 0; hop < maxHops; hop++)
        {
            if (Contains(visited, visitedN, track.Id) || count >= cap)
            {
                break;
            }

            visited[visitedN++] = track.Id;
            var exitX = towardOut ? track.OutX : track.InX;
            var exitZ = towardOut ? track.OutZ : track.InZ;
            var hx = exitX - entryX;
            var hz = exitZ - entryZ;
            var hLen = (float)Math.Sqrt((hx * hx) + (hz * hz));
            if (hLen > 1e-4f)
            {
                hx /= hLen;
                hz /= hLen;
            }
            else
            {
                hx = forwardX;
                hz = forwardZ;
            }

            // Bezier arc, matching the live walk. Chord stays the direction hint
            // only; span conversion is what lets remaining leave a hop.
            var length = track.LengthMeters > 1e-4f ? track.LengthMeters : hLen;
            if (track.LengthMeters > LookaheadMeters * 2f)
            {
                giant = true;
            }

            if (first)
            {
                var lax = exitX - locoX;
                var laz = exitZ - locoZ;
                var laLen = (float)Math.Sqrt((lax * lax) + (laz * laz));
                if (laLen > 1e-4f)
                {
                    lax /= laLen;
                    laz /= laLen;
                }
                else
                {
                    lax = hx;
                    laz = hz;
                }

                align = (lax * forwardX) + (laz * forwardZ);
                first = false;
            }

            if (trackIds != null && count < trackIds.Length)
            {
                trackIds[count] = track.Id;
            }

            if (write)
            {
                into![count] = new PathSegmentAlong(
                    entryAbs,
                    entryX,
                    0f,
                    entryZ,
                    hx,
                    hz,
                    length,
                    track.Id,
                    travelIncreasingSpan: towardOut,
                    chordLengthMeters: hLen);
            }

            count++;
            var exitAbs = entryAbs + length;
            if (exitAbs >= maxDistanceMeters)
            {
                break;
            }

            if (!TryNext(
                    tracks,
                    trackN,
                    juncs,
                    juncN,
                    track.Id,
                    exitX,
                    exitZ,
                    visited,
                    visitedN,
                    out var next,
                    out var nextTowardOut))
            {
                break;
            }

            track = next;
            towardOut = nextTowardOut;
            entryX = towardOut ? track.InX : track.OutX;
            entryZ = towardOut ? track.InZ : track.OutZ;
            entryAbs = exitAbs;
        }

        probe = new WalkProbe(giant, align, count);
        return count;
    }

    private readonly struct WalkProbe
    {
        public WalkProbe(bool giant, float align, int hops)
        {
            Giant = giant;
            Align = align;
            Hops = hops;
        }

        public bool Giant { get; }

        public float Align { get; }

        public int Hops { get; }
    }

    public static bool PathContainsTrack(int[]? trackIds, int count, int trackId)
    {
        if (trackIds == null || trackId == 0 || count <= 0)
        {
            return false;
        }

        var n = count > trackIds.Length ? trackIds.Length : count;
        for (var i = 0; i < n; i++)
        {
            if (trackIds[i] == trackId)
            {
                return true;
            }
        }

        return false;
    }

    private static int ClampCount(int length, int count)
    {
        if (count <= 0 || count > length)
        {
            return length;
        }

        return count;
    }

    private static bool TryStartTrack(
        CoreTrack[] tracks,
        int trackN,
        float locoX,
        float locoZ,
        int startTrackId,
        out int index)
    {
        if (startTrackId != 0)
        {
            var n = trackN > tracks.Length ? tracks.Length : trackN;
            for (var i = 0; i < n; i++)
            {
                if (tracks[i].Id == startTrackId)
                {
                    index = i;
                    return true;
                }
            }
        }

        return TryClosestTrack(tracks, trackN, locoX, locoZ, out index);
    }

    private static bool TryClosestTrack(
        CoreTrack[] tracks,
        int trackN,
        float locoX,
        float locoZ,
        out int index)
    {
        index = -1;
        var best = float.MaxValue;
        var n = trackN > tracks.Length ? tracks.Length : trackN;
        for (var i = 0; i < n; i++)
        {
            var d = ChordDistSq(tracks[i], locoX, locoZ);
            if (d < best)
            {
                best = d;
                index = i;
            }
        }

        return index >= 0;
    }

    /// <summary>
    /// Chord progress scaled onto the track's arc, so the loco's covered metres
    /// share the same axis as the arc-based entry distances.
    /// </summary>
    private static float AlongArc(in CoreTrack track, float px, float pz, bool towardOut)
    {
        var chordAlong = AlongChord(in track, px, pz, towardOut);
        var chord = ChordLength(in track);
        if (chord <= 1e-4f || track.LengthMeters <= 0f)
        {
            return chordAlong;
        }

        return chordAlong * (track.LengthMeters / chord);
    }

    private static float ChordLength(in CoreTrack track)
    {
        var dx = track.OutX - track.InX;
        var dz = track.OutZ - track.InZ;
        return (float)Math.Sqrt((dx * dx) + (dz * dz));
    }

    private static float AlongChord(in CoreTrack track, float px, float pz, bool towardOut)
    {
        var abx = track.OutX - track.InX;
        var abz = track.OutZ - track.InZ;
        var chordLenSq = (abx * abx) + (abz * abz);
        if (chordLenSq <= 1e-8f)
        {
            return 0f;
        }

        var chordLen = (float)Math.Sqrt(chordLenSq);
        var u = (((px - track.InX) * abx) + ((pz - track.InZ) * abz)) / chordLenSq;
        if (u < 0f)
        {
            u = 0f;
        }
        else if (u > 1f)
        {
            u = 1f;
        }

        var fromIn = u * chordLen;
        return towardOut ? fromIn : chordLen - fromIn;
    }

    private static float ChordDistSq(in CoreTrack track, float px, float pz)
    {
        var abx = track.OutX - track.InX;
        var abz = track.OutZ - track.InZ;
        var abLenSq = (abx * abx) + (abz * abz);
        float qx = track.InX;
        float qz = track.InZ;
        if (abLenSq >= 1e-8f)
        {
            var u = (((px - track.InX) * abx) + ((pz - track.InZ) * abz)) / abLenSq;
            if (u < 0f)
            {
                u = 0f;
            }
            else if (u > 1f)
            {
                u = 1f;
            }

            qx = track.InX + (abx * u);
            qz = track.InZ + (abz * u);
        }

        var dx = px - qx;
        var dz = pz - qz;
        return (dx * dx) + (dz * dz);
    }

    private static bool TryNext(
        CoreTrack[] tracks,
        int trackN,
        CoreJunction[] junctions,
        int juncN,
        int currentId,
        float exitX,
        float exitZ,
        int[] visited,
        int visitedN,
        out CoreTrack next,
        out bool nextTowardOut)
    {
        if (TryNextViaJunction(
                tracks,
                trackN,
                junctions,
                juncN,
                currentId,
                exitX,
                exitZ,
                visited,
                visitedN,
                out next,
                out nextTowardOut))
        {
            return true;
        }

        return TryNextPlain(
            tracks,
            trackN,
            currentId,
            exitX,
            exitZ,
            visited,
            visitedN,
            out next,
            out nextTowardOut);
    }

    private static bool TryNextViaJunction(
        CoreTrack[] tracks,
        int trackN,
        CoreJunction[] junctions,
        int juncN,
        int currentId,
        float exitX,
        float exitZ,
        int[] visited,
        int visitedN,
        out CoreTrack next,
        out bool nextTowardOut)
    {
        next = default;
        nextTowardOut = true;
        var eps = ConnectMeters * ConnectMeters;
        var n = juncN > junctions.Length ? junctions.Length : juncN;
        for (var i = 0; i < n; i++)
        {
            var j = junctions[i];
            if (!TryFind(tracks, trackN, j.StemId, out var stem)
                || !TryFind(tracks, trackN, j.LeftId, out var left)
                || !TryFind(tracks, trackN, j.RightId, out var right)
                || !TryFrog(in stem, in left, in right, out var fx, out var fz))
            {
                continue;
            }

            if (DistSq(exitX, exitZ, fx, fz) > eps)
            {
                continue;
            }

            int nextId;
            if (currentId == j.StemId)
            {
                nextId = j.SelectedBranch == 0 ? j.LeftId : j.RightId;
            }
            else if (currentId == j.LeftId || currentId == j.RightId)
            {
                nextId = j.StemId;
            }
            else
            {
                continue;
            }

            if (nextId == currentId || nextId == 0 || Contains(visited, visitedN, nextId))
            {
                continue;
            }

            if (!TryFind(tracks, trackN, nextId, out next))
            {
                continue;
            }

            nextTowardOut = DistSq(next.InX, next.InZ, exitX, exitZ)
                <= DistSq(next.OutX, next.OutZ, exitX, exitZ);
            return true;
        }

        return false;
    }

    private static bool TryNextPlain(
        CoreTrack[] tracks,
        int trackN,
        int currentId,
        float exitX,
        float exitZ,
        int[] visited,
        int visitedN,
        out CoreTrack next,
        out bool nextTowardOut)
    {
        next = default;
        nextTowardOut = true;
        var best = ConnectMeters * ConnectMeters;
        var found = false;
        var towardOut = true;
        var n = trackN > tracks.Length ? tracks.Length : trackN;
        for (var i = 0; i < n; i++)
        {
            var t = tracks[i];
            if (t.Id == currentId || Contains(visited, visitedN, t.Id))
            {
                continue;
            }

            var din = DistSq(t.InX, t.InZ, exitX, exitZ);
            var dout = DistSq(t.OutX, t.OutZ, exitX, exitZ);
            var d = din <= dout ? din : dout;
            if (d > best)
            {
                continue;
            }

            best = d;
            next = t;
            towardOut = din <= dout;
            found = true;
        }

        nextTowardOut = towardOut;
        return found;
    }

    private static bool TryFrog(
        in CoreTrack stem,
        in CoreTrack left,
        in CoreTrack right,
        out float fx,
        out float fz)
    {
        if (BothBranchesMeet(in left, in right, stem.InX, stem.InZ))
        {
            fx = stem.InX;
            fz = stem.InZ;
            return true;
        }

        if (BothBranchesMeet(in left, in right, stem.OutX, stem.OutZ))
        {
            fx = stem.OutX;
            fz = stem.OutZ;
            return true;
        }

        fx = 0f;
        fz = 0f;
        return false;
    }

    private static bool BothBranchesMeet(in CoreTrack left, in CoreTrack right, float x, float z) =>
        HasEndNear(in left, x, z) && HasEndNear(in right, x, z);

    private static bool HasEndNear(in CoreTrack track, float x, float z)
    {
        var lim = ConnectMeters * ConnectMeters;
        return DistSq(track.InX, track.InZ, x, z) <= lim
            || DistSq(track.OutX, track.OutZ, x, z) <= lim;
    }

    private static float DistSq(float ax, float az, float bx, float bz)
    {
        var dx = ax - bx;
        var dz = az - bz;
        return (dx * dx) + (dz * dz);
    }

    private static bool TryFind(CoreTrack[] tracks, int trackN, int id, out CoreTrack track)
    {
        track = default;
        if (id == 0)
        {
            return false;
        }

        var n = trackN > tracks.Length ? tracks.Length : trackN;
        for (var i = 0; i < n; i++)
        {
            if (tracks[i].Id == id)
            {
                track = tracks[i];
                return true;
            }
        }

        return false;
    }

    private static bool Contains(int[] ids, int n, int id)
    {
        for (var i = 0; i < n; i++)
        {
            if (ids[i] == id)
            {
                return true;
            }
        }

        return false;
    }
}
