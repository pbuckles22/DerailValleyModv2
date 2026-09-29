using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Ladder boxes from junction XZ. Junctions within <see cref="GapMeters"/> of each
/// other (including through a chain) are one ladder. City prefixes are not boxes:
/// <c>SW-C4S</c> and <c>SW-B4L</c> are both yard SW.
/// </summary>
public readonly struct YardLadderZones
{
    /// <summary>
    /// Wider than one frog, narrower than the gap between the C and B ladders
    /// in the 16.3 fixture (~400 m).
    /// </summary>
    public const float GapMeters = 120f;

    private readonly Dictionary<string, int> _zoneOf;
    private readonly Dictionary<int, string> _nameOf;

    private YardLadderZones(Dictionary<string, int> zoneOf, Dictionary<int, string> nameOf)
    {
        _zoneOf = zoneOf;
        _nameOf = nameOf;
    }

    public bool Active => _zoneOf != null && _zoneOf.Count > 0;

    public static YardLadderZones Build(SpatialGraph spatial)
    {
        if (!spatial.HasCoordinates || spatial.JunctionXz == null)
        {
            return default;
        }

        var ids = new List<string>(spatial.JunctionCount);
        foreach (var kv in spatial.JunctionXz)
        {
            if (!string.IsNullOrEmpty(kv.Key))
            {
                ids.Add(kv.Key);
            }
        }

        if (ids.Count == 0)
        {
            return default;
        }

        var parent = new int[ids.Count];
        for (var i = 0; i < parent.Length; i++)
        {
            parent[i] = i;
        }

        var gap2 = GapMeters * GapMeters;
        for (var i = 0; i < ids.Count; i++)
        {
            if (!spatial.TryGetJunctionXz(ids[i], out var ax, out var az))
            {
                continue;
            }

            for (var j = i + 1; j < ids.Count; j++)
            {
                if (!spatial.TryGetJunctionXz(ids[j], out var bx, out var bz))
                {
                    continue;
                }

                var dx = ax - bx;
                var dz = az - bz;
                if ((dx * dx) + (dz * dz) <= gap2)
                {
                    Union(parent, i, j);
                }
            }
        }

        var zoneOf = new Dictionary<string, int>(ids.Count, StringComparer.Ordinal);
        var rootToZone = new Dictionary<int, int>();
        var nameOf = new Dictionary<int, string>();
        var next = 0;
        for (var i = 0; i < ids.Count; i++)
        {
            var root = Find(parent, i);
            if (!rootToZone.TryGetValue(root, out var zone))
            {
                zone = next++;
                rootToZone[root] = zone;
                nameOf[zone] = ids[i];
            }
            else if (string.CompareOrdinal(ids[i], nameOf[zone]) < 0)
            {
                nameOf[zone] = ids[i];
            }

            zoneOf[ids[i]] = zone;
        }

        return new YardLadderZones(zoneOf, nameOf);
    }

    public bool TryZone(string? junctionId, out int zone)
    {
        zone = -1;
        var id = junctionId?.Trim();
        if (string.IsNullOrEmpty(id) || _zoneOf == null)
        {
            return false;
        }

        return _zoneOf.TryGetValue(id!, out zone);
    }

    public string NameOf(int zone)
    {
        if (_nameOf != null && _nameOf.TryGetValue(zone, out var name))
        {
            return name;
        }

        return "zone";
    }

    /// <summary>
    /// True when this hop steps back into the origin ladder after the walk has
    /// already left it, and the destination ladder is a different one.
    /// Same-ladder destinations stay open so a consist can pull past its frog.
    /// </summary>
    public bool BlocksReentry(int entryZone, int hopZone, int originZone, int destZone) =>
        Active
        && originZone >= 0
        && destZone >= 0
        && destZone != originZone
        && entryZone >= 0
        && entryZone != originZone
        && hopZone == originZone;

    private static int Find(int[] parent, int i)
    {
        while (parent[i] != i)
        {
            parent[i] = parent[parent[i]];
            i = parent[i];
        }

        return i;
    }

    private static void Union(int[] parent, int a, int b)
    {
        var ra = Find(parent, a);
        var rb = Find(parent, b);
        if (ra != rb)
        {
            parent[rb] = ra;
        }
    }
}
