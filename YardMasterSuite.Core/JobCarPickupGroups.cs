using System;

namespace YardMasterSuite.Core;

/// <summary>Running centroid for one pickup-spur group.</summary>
public struct JobCarPickupAccum
{
    public string TrackLabel;
    public int Count;
    public float SumX;
    public float SumY;
    public float SumZ;

    public float CentroidX => Count > 0 ? SumX / Count : 0f;

    public float CentroidY => Count > 0 ? SumY / Count : 0f;

    public float CentroidZ => Count > 0 ? SumZ / Count : 0f;
}

/// <summary>Ranked pickup-group pin after distance sort.</summary>
public readonly struct JobCarPickupMarker
{
    public JobCarPickupMarker(string trackLabel, int count, float x, float y, float z, int groupIndex = 0)
    {
        TrackLabel = trackLabel;
        Count = count;
        X = x;
        Y = y;
        Z = z;
        GroupIndex = groupIndex;
    }

    public string TrackLabel { get; }

    public int Count { get; }

    public float X { get; }

    public float Y { get; }

    public float Z { get; }

    public int GroupIndex { get; }
}

/// <summary>One job-car sample inside a spur group.</summary>
public readonly struct JobCarPickupSample
{
    public JobCarPickupSample(int groupIndex, float x, float y, float z)
    {
        GroupIndex = groupIndex;
        X = x;
        Y = y;
        Z = z;
    }

    public int GroupIndex { get; }

    public float X { get; }

    public float Y { get; }

    public float Z { get; }
}

/// <summary>
/// Group unattached job cars by spur, then keep the nearest
/// <see cref="JobCarMarkerDisplay.DefaultMaxMarkers"/> pins. No heap.
/// </summary>
public static class JobCarPickupGroups
{
    public const int DefaultMaxMarkers = JobCarMarkerDisplay.DefaultMaxMarkers;

    public const int AccumCapacity = 16;

    public const int SampleCapacity = 32;

    public const string MissingSpurLabel = "—";

    /// <summary>Camera-forward threshold: same band as AR behind-camera enter.</summary>
    public const float InViewMinForward = 0.05f;

    /// <summary>
    /// Standing next to a car that fills the frame: origin can be ~90° off
    /// axis (2.6.21.4 smoke, 43 m edge pin). Cosine vs look, not meters
    /// behind the camera plane — a 17 m flatcar origin is ~8.5 m beside you.
    /// </summary>
    public const float DefaultAdjacentMeters = 16f;

    /// <summary>~104° from look (origin just over the shoulder).</summary>
    public const float AdjacentMinForward = -0.25f;

    /// <summary>~60° from look (120° cone) for far cars still on-screen.</summary>
    public const float DefaultMinCosFov = 0.5f;

    public static bool IsInView(
        float dx,
        float dy,
        float dz,
        float forwardX,
        float forwardY,
        float forwardZ,
        float minViewForward = InViewMinForward,
        float adjacentMeters = DefaultAdjacentMeters,
        float minCosFov = DefaultMinCosFov)
    {
        var viewForward = (dx * forwardX) + (dy * forwardY) + (dz * forwardZ);
        var distSq = (dx * dx) + (dy * dy) + (dz * dz);
        var adjacent = adjacentMeters > 0f ? adjacentMeters : DefaultAdjacentMeters;
        if (distSq <= adjacent * adjacent)
        {
            if (distSq < 1e-8f)
            {
                return true;
            }

            var adjDist = (float)Math.Sqrt(distSq);
            if (viewForward / adjDist > AdjacentMinForward)
            {
                return true;
            }
        }

        if (viewForward <= minViewForward)
        {
            return false;
        }

        var dist = (float)Math.Sqrt(distSq);
        if (dist < 1e-4f)
        {
            return true;
        }

        var minCos = minCosFov;
        if (minCos <= 0f || float.IsNaN(minCos))
        {
            minCos = DefaultMinCosFov;
        }

        return viewForward / dist >= minCos;
    }

    public static bool Add(
        JobCarPickupAccum[] groups,
        ref int groupCount,
        string? spur,
        float x,
        float y,
        float z) =>
        TryAdd(groups, ref groupCount, spur, x, y, z, out _);

    public static bool TryAdd(
        JobCarPickupAccum[] groups,
        ref int groupCount,
        string? spur,
        float x,
        float y,
        float z,
        out int groupIndex)
    {
        groupIndex = -1;
        if (groups == null || groupCount < 0)
        {
            return false;
        }

        if (!JobCarMarkerDisplay.CanPinTrack(spur))
        {
            return false;
        }

        var key = spur!.Trim();
        for (var i = 0; i < groupCount; i++)
        {
            if (string.Equals(groups[i].TrackLabel, key, StringComparison.Ordinal))
            {
                groups[i].Count++;
                groups[i].SumX += x;
                groups[i].SumY += y;
                groups[i].SumZ += z;
                groupIndex = i;
                return true;
            }
        }

        if (groupCount >= groups.Length)
        {
            return false;
        }

        groups[groupCount] = new JobCarPickupAccum
        {
            TrackLabel = key,
            Count = 1,
            SumX = x,
            SumY = y,
            SumZ = z,
        };
        groupIndex = groupCount;
        groupCount++;
        return true;
    }

    public static int RankNearest(
        JobCarPickupAccum[] groups,
        int groupCount,
        bool havePlayer,
        float playerX,
        float playerY,
        float playerZ,
        JobCarPickupMarker[] dest)
    {
        if (dest == null || dest.Length == 0 || groups == null || groupCount <= 0)
        {
            return 0;
        }

        var limit = groupCount;
        if (limit > groups.Length)
        {
            limit = groups.Length;
        }

        var cap = DefaultMaxMarkers;
        if (cap > dest.Length)
        {
            cap = dest.Length;
        }

        var used = 0;
        var ranked = 0;
        while (ranked < cap)
        {
            var best = -1;
            for (var i = 0; i < limit; i++)
            {
                if ((used & (1 << i)) != 0 || groups[i].Count <= 0)
                {
                    continue;
                }

                if (best < 0 || IsCloser(i, best, groups, havePlayer, playerX, playerY, playerZ))
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                break;
            }

            used |= 1 << best;
            var g = groups[best];
            dest[ranked] = new JobCarPickupMarker(
                g.TrackLabel,
                g.Count,
                g.CentroidX,
                g.CentroidY,
                g.CentroidZ,
                best);
            ranked++;
        }

        return ranked;
    }

    public static bool TryAddSample(
        JobCarPickupAccum[] groups,
        ref int groupCount,
        JobCarPickupSample[] samples,
        ref int sampleCount,
        string? spur,
        float x,
        float y,
        float z)
    {
        if (samples == null || sampleCount < 0 || sampleCount >= samples.Length)
        {
            return false;
        }

        if (!TryAdd(groups, ref groupCount, spur, x, y, z, out var groupIndex))
        {
            return false;
        }

        samples[sampleCount] = new JobCarPickupSample(groupIndex, x, y, z);
        sampleCount++;
        return true;
    }

    public static bool TryPickNearestInGroup(
        JobCarPickupSample[] samples,
        int sampleCount,
        int groupIndex,
        float playerX,
        float playerY,
        float playerZ,
        out float x,
        out float y,
        out float z)
    {
        x = 0f;
        y = 0f;
        z = 0f;
        if (samples == null || sampleCount <= 0)
        {
            return false;
        }

        var limit = sampleCount;
        if (limit > samples.Length)
        {
            limit = samples.Length;
        }

        var found = false;
        var bestSq = 0f;
        for (var i = 0; i < limit; i++)
        {
            var s = samples[i];
            if (s.GroupIndex != groupIndex)
            {
                continue;
            }

            var dx = s.X - playerX;
            var dy = s.Y - playerY;
            var dz = s.Z - playerZ;
            var sq = (dx * dx) + (dy * dy) + (dz * dz);
            if (!found || sq < bestSq)
            {
                found = true;
                bestSq = sq;
                x = s.X;
                y = s.Y;
                z = s.Z;
            }
        }

        return found;
    }

    public static bool TryPickNearestInView(
        JobCarPickupSample[] samples,
        int sampleCount,
        int groupIndex,
        float playerX,
        float playerY,
        float playerZ,
        float forwardX,
        float forwardY,
        float forwardZ,
        float minViewForward,
        out float x,
        out float y,
        out float z)
    {
        x = 0f;
        y = 0f;
        z = 0f;
        if (samples == null || sampleCount <= 0)
        {
            return false;
        }

        var limit = sampleCount;
        if (limit > samples.Length)
        {
            limit = samples.Length;
        }

        var minFwd = minViewForward;
        var found = false;
        var bestSq = 0f;
        for (var i = 0; i < limit; i++)
        {
            var s = samples[i];
            if (s.GroupIndex != groupIndex)
            {
                continue;
            }

            var dx = s.X - playerX;
            var dy = s.Y - playerY;
            var dz = s.Z - playerZ;
            if (!IsInView(dx, dy, dz, forwardX, forwardY, forwardZ, minFwd))
            {
                continue;
            }

            var sq = (dx * dx) + (dy * dy) + (dz * dz);
            if (!found || sq < bestSq)
            {
                found = true;
                bestSq = sq;
                x = s.X;
                y = s.Y;
                z = s.Z;
            }
        }

        return found;
    }

    private static bool IsCloser(
        int a,
        int b,
        JobCarPickupAccum[] groups,
        bool havePlayer,
        float px,
        float py,
        float pz)
    {
        if (havePlayer)
        {
            var da = DistanceSq(groups[a], px, py, pz);
            var db = DistanceSq(groups[b], px, py, pz);
            var cmp = da.CompareTo(db);
            if (cmp != 0)
            {
                return cmp < 0;
            }
        }

        var nameCmp = string.CompareOrdinal(groups[a].TrackLabel, groups[b].TrackLabel);
        if (nameCmp != 0)
        {
            return nameCmp < 0;
        }

        return a < b;
    }

    private static float DistanceSq(in JobCarPickupAccum g, float px, float py, float pz)
    {
        var dx = g.CentroidX - px;
        var dy = g.CentroidY - py;
        var dz = g.CentroidZ - pz;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }
}
