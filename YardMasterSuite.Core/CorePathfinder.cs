using System;

namespace YardMasterSuite.Core;

/// <summary>
/// 9.1.3 Win 2 — walk dumped tracks + thrown junctions from the loco.
/// Allocates for the one-shot HTP walk; Unity tick pooling is Win 5.
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
        int intoLength)
    {
        if (tracks == null || tracks.Length == 0 || into == null || intoLength <= 0)
        {
            return 0;
        }

        var juncs = junctions ?? Array.Empty<CoreJunction>();
        var maxHops = intoLength < MaxHops ? intoLength : MaxHops;
        if (maxDistanceMeters <= 0f)
        {
            maxDistanceMeters = LookaheadMeters;
        }

        if (!TryClosestTrack(tracks, locoX, locoZ, out var startIndex))
        {
            return 0;
        }

        var visited = new int[maxHops];
        var visitedN = 0;
        var track = tracks[startIndex];
        var towardOut = PreferTowardOut(in track, locoX, locoZ, forwardX, forwardZ);
        var entryX = towardOut ? track.InX : track.OutX;
        var entryZ = towardOut ? track.InZ : track.OutZ;
        var covered = AlongChord(in track, locoX, locoZ, towardOut);
        var entryAbs = -covered;
        var count = 0;

        for (var hop = 0; hop < maxHops; hop++)
        {
            if (Contains(visited, visitedN, track.Id) || count >= into.Length)
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

            var length = track.LengthMeters > 0f ? track.LengthMeters : hLen;
            into[count++] = new PathSegmentAlong(entryAbs, entryX, 0f, entryZ, hx, hz, length);
            var exitAbs = entryAbs + length;
            if (exitAbs >= maxDistanceMeters)
            {
                break;
            }

            if (!TryNext(
                    tracks,
                    juncs,
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

        return count;
    }

    private static bool TryClosestTrack(
        CoreTrack[] tracks,
        float locoX,
        float locoZ,
        out int index)
    {
        index = -1;
        var best = float.MaxValue;
        for (var i = 0; i < tracks.Length; i++)
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

    private static bool PreferTowardOut(
        in CoreTrack track,
        float locoX,
        float locoZ,
        float forwardX,
        float forwardZ)
    {
        var towardOut = ((track.OutX - locoX) * forwardX) + ((track.OutZ - locoZ) * forwardZ);
        var towardIn = ((track.InX - locoX) * forwardX) + ((track.InZ - locoZ) * forwardZ);
        return towardOut >= towardIn;
    }

    private static float AlongChord(in CoreTrack track, float px, float pz, bool towardOut)
    {
        var abx = track.OutX - track.InX;
        var abz = track.OutZ - track.InZ;
        var len = track.LengthMeters > 0f
            ? track.LengthMeters
            : (float)Math.Sqrt((abx * abx) + (abz * abz));
        if (len <= 1e-4f)
        {
            return 0f;
        }

        var abLenSq = (abx * abx) + (abz * abz);
        var u = 0f;
        if (abLenSq >= 1e-8f)
        {
            u = (((px - track.InX) * abx) + ((pz - track.InZ) * abz)) / abLenSq;
            if (u < 0f)
            {
                u = 0f;
            }
            else if (u > 1f)
            {
                u = 1f;
            }
        }

        var fromIn = u * len;
        return towardOut ? fromIn : len - fromIn;
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
        CoreJunction[] junctions,
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
                junctions,
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
        CoreJunction[] junctions,
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
        for (var i = 0; i < junctions.Length; i++)
        {
            var j = junctions[i];
            if (!TryFind(tracks, j.StemId, out var stem)
                || !TryFind(tracks, j.LeftId, out var left)
                || !TryFind(tracks, j.RightId, out var right)
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

            if (!TryFind(tracks, nextId, out next))
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
        for (var i = 0; i < tracks.Length; i++)
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

    private static bool TryFind(CoreTrack[] tracks, int id, out CoreTrack track)
    {
        track = default;
        if (id == 0)
        {
            return false;
        }

        for (var i = 0; i < tracks.Length; i++)
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
