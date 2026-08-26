using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Group track catalog entries by yard/city for destination pickers (v1 3.5 / **8.1**).</summary>
public static class DestinationCatalog
{
    public static IReadOnlyList<string> ListYards(IEnumerable<(string YardId, string TrackId)> entries)
    {
        var yards = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        if (entries == null)
        {
            return Array.Empty<string>();
        }

        foreach (var (yardId, trackId) in entries)
        {
            if (!TryNormalize(yardId, trackId, out var y, out _))
            {
                continue;
            }

            yards.Add(y);
        }

        return new List<string>(yards);
    }

    public static IReadOnlyList<string> ListTracksInYard(
        IEnumerable<(string YardId, string TrackId)> entries,
        string? yardId)
    {
        var yard = yardId?.Trim();
        if (string.IsNullOrEmpty(yard) || entries == null)
        {
            return Array.Empty<string>();
        }

        var tracks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (y, trackId) in entries)
        {
            if (!TryNormalize(y, trackId, out var ny, out var t))
            {
                continue;
            }

            if (!string.Equals(ny, yard, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            tracks.Add(t);
        }

        return new List<string>(tracks);
    }

    public static int CycleIndex(int current, int count, int delta)
    {
        if (count <= 0)
        {
            return 0;
        }

        var next = current + delta;
        next %= count;
        if (next < 0)
        {
            next += count;
        }

        return next;
    }

    /// <summary>Derive yard from display id <c>SM-A2P</c> → <c>SM</c>.</summary>
    public static string? YardIdFromTrackKey(string? trackKey)
    {
        var key = trackKey?.Trim();
        if (string.IsNullOrEmpty(key) || key![0] == '#')
        {
            return null;
        }

        if (!LocoRadarDisplay.TrackIncludesCity(key))
        {
            return null;
        }

        var dash = key.IndexOf('-');
        var yard = key.Substring(0, dash).Trim();
        return LocoRadarDisplay.IsUsableCityYardId(yard) ? yard : null;
    }

    public static bool IsListedTrack(string? trackId)
    {
        var t = trackId?.Trim();
        return !string.IsNullOrEmpty(t) && t![0] != '#';
    }

    public static bool TryAdd(
        ICollection<(string YardId, string TrackId)> catalog,
        string? yardId,
        string? trackId)
    {
        if (catalog == null || !TryNormalize(yardId, trackId, out var y, out var t))
        {
            return false;
        }

        catalog.Add((y, t));
        return true;
    }

    private static bool TryNormalize(
        string? yardId,
        string? trackId,
        out string yard,
        out string track)
    {
        yard = string.Empty;
        track = string.Empty;
        if (!IsListedTrack(trackId))
        {
            return false;
        }

        var y = yardId?.Trim();
        if (!LocoRadarDisplay.IsUsableCityYardId(y))
        {
            return false;
        }

        yard = y!;
        track = trackId!.Trim();
        return true;
    }
}
