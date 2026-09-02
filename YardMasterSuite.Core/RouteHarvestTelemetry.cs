using System;

namespace YardMasterSuite.Core;

/// <summary>Last km-bucket dest remaining line (<b>13.1.15</b>).</summary>
public struct RouteRemainLogCache
{
    public bool Seeded;
    public int Bucket;
    public int DestHash;
}

/// <summary>Rising-edge dest-yard-behind (<b>13.1.15</b>).</summary>
public struct RouteDestYardBehindCache
{
    public bool Seeded;
    public bool Behind;
}

/// <summary>
/// Change-only dest remaining / dest-yard-behind harvest. Desk open or closed.
/// Does not write HUD remaining.
/// </summary>
public static class RouteHarvestTelemetry
{
    public const string DestYardBehind = "T2 route: dest-yard behind";

    public static int KmBucket(float meters)
    {
        if (float.IsNaN(meters) || float.IsInfinity(meters) || meters < 0f)
        {
            return -1;
        }

        return (int)Math.Floor(meters / 1000f);
    }

    /// <summary>
    /// Dest yard is behind the consist when the dest city differs from the
    /// consist city and the dest track is behind facing.
    /// </summary>
    public static bool IsDestYardBehind(
        string? destYardId,
        string? consistYardId,
        bool destTrackBehind)
    {
        if (!destTrackBehind || string.IsNullOrWhiteSpace(destYardId))
        {
            return false;
        }

        var dest = destYardId!.Trim();
        if (string.IsNullOrEmpty(consistYardId))
        {
            return true;
        }

        return !string.Equals(dest, consistYardId!.Trim(), StringComparison.Ordinal);
    }

    public static string FormatRemain(float meters, string? dest)
    {
        var m = float.IsNaN(meters) || float.IsInfinity(meters) || meters < 0f
            ? 0
            : (int)Math.Round(meters, MidpointRounding.AwayFromZero);
        var d = string.IsNullOrWhiteSpace(dest) ? "—" : dest!.Trim();
        return "T2 route: rem=" + m + "m dest=" + d;
    }

    public static string? NextRemain(float? meters, string? dest, ref RouteRemainLogCache cache)
    {
        if (meters is not float m || float.IsNaN(m) || float.IsInfinity(m) || m < 0f
            || string.IsNullOrWhiteSpace(dest))
        {
            cache.Seeded = false;
            cache.Bucket = -1;
            cache.DestHash = 0;
            return null;
        }

        var bucket = KmBucket(m);
        var destHash = StringComparer.Ordinal.GetHashCode(dest!.Trim());
        if (cache.Seeded && cache.Bucket == bucket && cache.DestHash == destHash)
        {
            return null;
        }

        cache.Seeded = true;
        cache.Bucket = bucket;
        cache.DestHash = destHash;
        return FormatRemain(m, dest);
    }

    public static string? NextDestYardBehind(bool behind, ref RouteDestYardBehindCache cache)
    {
        if (cache.Seeded && cache.Behind == behind)
        {
            return null;
        }

        cache.Seeded = true;
        cache.Behind = behind;
        return behind ? DestYardBehind : null;
    }
}
