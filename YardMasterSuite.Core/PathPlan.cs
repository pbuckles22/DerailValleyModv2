using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Planner profile on one shared graph.
/// World = long-haul (junction commitment hard-skip). Yard = in-town / TT.
/// </summary>
public enum PathPlanMode
{
    World = 0,
    Yard = 1,
}

/// <summary>
/// First place a Yard corridor re-uses a junction with a different required branch.
/// Pin / list stop = this switch (approach), not the first corridor flip.
/// </summary>
public readonly struct PathJunctionFirstStop
{
    public PathJunctionFirstStop(
        string junctionId,
        int requiredBranch,
        string fromTrackId,
        string toTrackId)
    {
        JunctionId = junctionId ?? string.Empty;
        RequiredBranch = requiredBranch;
        FromTrackId = fromTrackId ?? string.Empty;
        ToTrackId = toTrackId ?? string.Empty;
    }

    public string JunctionId { get; }
    public int RequiredBranch { get; }
    public string FromTrackId { get; }
    public string ToTrackId { get; }
}

/// <summary>Dijkstra path plan with reverse cues for Align Route (3.5).</summary>
public sealed class PathPlanResult
{
    public PathPlanResult(
        PathCheckStatus status,
        IReadOnlyList<string> trackIds,
        IReadOnlyList<PathJunctionEval> junctions,
        int misalignedCount,
        int reverseCount,
        bool lastHopRequiresReverse,
        float totalCost,
        PathJunctionFirstStop? junctionFirstStop = null,
        IReadOnlyDictionary<string, string>? junctionApproachFrom = null,
        float spatialPenaltySeconds = 0f,
        string? zoneBlockLog = null)
    {
        Status = status;
        TrackIds = trackIds;
        Junctions = junctions;
        MisalignedCount = misalignedCount;
        ReverseCount = reverseCount;
        LastHopRequiresReverse = lastHopRequiresReverse;
        TotalCost = totalCost;
        JunctionFirstStop = junctionFirstStop;
        JunctionApproachFrom = junctionApproachFrom ?? EmptyApproach;
        SpatialPenaltySeconds = spatialPenaltySeconds;
        ZoneBlockLog = zoneBlockLog;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyApproach =
        new Dictionary<string, string>(0);

    public PathCheckStatus Status { get; }
    public IReadOnlyList<string> TrackIds { get; }
    public IReadOnlyList<PathJunctionEval> Junctions { get; }
    public int MisalignedCount { get; }
    public int ReverseCount { get; }
    public bool LastHopRequiresReverse { get; }
    public float TotalCost { get; }

    /// <summary>
    /// Seconds added because hops moved away from the destination.
    /// Zero when the graph has no coordinates.
    /// </summary>
    public float SpatialPenaltySeconds { get; }

    /// <summary>
    /// Set when a hop back into the origin ladder was refused. Null when the
    /// search never needed that block.
    /// </summary>
    public string? ZoneBlockLog { get; }

    /// <summary>
    /// Sawtooth / corridor: first <c>from</c> track on initial crossing of each junction id.
    /// </summary>
    public IReadOnlyDictionary<string, string> JunctionApproachFrom { get; }

    /// <summary>
    /// When set, sawtooth re-entry junction (Path OK). Pin uses RequiredFlips first when misaligned.
    /// </summary>
    public PathJunctionFirstStop? JunctionFirstStop { get; }

    /// <summary>First hop <c>from</c> track before <paramref name="junctionId"/> on this corridor.</summary>
    public bool TryGetApproachTrack(string? junctionId, out string? fromTrackId)
    {
        fromTrackId = null;
        var id = junctionId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        return JunctionApproachFrom.TryGetValue(id, out fromTrackId);
    }

    public PathCheckResult ToCheckResult() =>
        new(Status, TrackIds, Junctions, MisalignedCount);

    /// <summary>True when <paramref name="trackId"/> is any hop on this plan (driving along route).</summary>
    public bool ContainsTrack(string? trackId)
    {
        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (var i = 0; i < TrackIds.Count; i++)
        {
            if (string.Equals(TrackIds[i], id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tail is still on the pin's approach hop, or on an earlier hop.
    /// Cab 2.16.22 step 6: forward-dot read 2 m to clear on
    /// <c>#Y-#S241#T</c> / <c>#Y-#S1263#T</c>, the near side of 1002848,
    /// then the C Prep reversed back into B.
    /// Unknown approach / tail / hop → true (fail closed: hold approach).
    /// </summary>
    public bool TailStillBeforePinExit(string? pinJunctionId, string? tailTrackId)
    {
        if (!TryGetApproachTrack(pinJunctionId, out var from) || string.IsNullOrEmpty(from))
        {
            return true;
        }

        var tail = tailTrackId?.Trim();
        if (string.IsNullOrEmpty(tail) || TrackIds == null)
        {
            return true;
        }

        var fromIndex = -1;
        var tailIndex = -1;
        for (var i = 0; i < TrackIds.Count; i++)
        {
            if (fromIndex < 0 && string.Equals(TrackIds[i], from, StringComparison.Ordinal))
            {
                fromIndex = i;
            }

            if (tailIndex < 0 && string.Equals(TrackIds[i], tail, StringComparison.Ordinal))
            {
                tailIndex = i;
            }
        }

        if (fromIndex < 0 || tailIndex < 0)
        {
            return true;
        }

        return tailIndex <= fromIndex;
    }
}

/// <summary>Frozen adjacency for repeated <see cref="PathPlan.Find"/> (HTP matrix).</summary>
public readonly struct PathPlanGraph
{
    internal PathPlanGraph(Dictionary<string, List<PathEdge>> adj)
    {
        Adj = adj ?? new Dictionary<string, List<PathEdge>>(StringComparer.Ordinal);
    }

    internal Dictionary<string, List<PathEdge>> Adj { get; }
}

/// <summary>
/// Spatial coordinates for A* heuristic. Junction XZ indexed by junction ID.
/// When provided, PathPlan.Find uses A* instead of Dijkstra.
/// </summary>
public readonly struct SpatialGraph
{
    public SpatialGraph(IReadOnlyDictionary<string, (float x, float z)>? junctionXz)
    {
        JunctionXz = junctionXz;
    }

    /// <summary>Junction ID → (X, Z) world coordinates.</summary>
    public IReadOnlyDictionary<string, (float x, float z)>? JunctionXz { get; }

    public bool HasCoordinates => JunctionXz != null && JunctionXz.Count > 0;

    public int JunctionCount => JunctionXz?.Count ?? 0;

    public bool TryGetJunctionXz(string? junctionId, out float x, out float z)
    {
        x = z = 0f;
        var id = junctionId?.Trim();
        if (string.IsNullOrEmpty(id) || JunctionXz == null)
        {
            return false;
        }

        if (JunctionXz.TryGetValue(id!, out var coord))
        {
            x = coord.x;
            z = coord.z;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Build from <see cref="RouteHarvestJunction"/> list (HTP fixtures or live dump).
    /// </summary>
    public static SpatialGraph FromHarvestJunctions(IReadOnlyList<RouteHarvestJunction>? junctions)
    {
        if (junctions == null || junctions.Count == 0)
        {
            return default;
        }

        var dict = new Dictionary<string, (float x, float z)>(junctions.Count, StringComparer.Ordinal);
        for (var i = 0; i < junctions.Count; i++)
        {
            var j = junctions[i];
            var id = j.Id?.Trim();
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (float.IsNaN(j.X) || float.IsNaN(j.Z))
            {
                continue;
            }

            dict[id!] = (j.X, j.Z);
        }

        return new SpatialGraph(dict);
    }
}

/// <summary>Cost-aware pathfinder used by Align Route preview / throw (3.5).</summary>
public static class PathPlan
{
    /// <summary>
    /// Extra seconds still eligible after the cheapest time path.
    /// Above a 7-second ladder hop. Far below a 4964-second tour.
    /// </summary>
    public const float CostBandSeconds = 12f;

    public static PathPlanResult Find(
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> junctionSelectedBranch,
        string? originTrackId,
        string? destinationTrackId,
        Func<string, PathTrackClass>? classFor = null,
        bool skipPlainOnMultiBranchStem = true,
        string? destYardId = null,
        Func<string, string?>? yardFor = null,
        PathPlanMode mode = PathPlanMode.World,
        SpatialGraph spatial = default,
        float consistLengthMeters = 0f)
    {
        var dest = Normalize(destinationTrackId);
        if (dest == null)
        {
            return Empty(PathCheckStatus.NoDestination);
        }

        var origin = Normalize(originTrackId);
        if (origin == null)
        {
            return Empty(PathCheckStatus.NoOrigin);
        }

        if (string.Equals(origin, dest, StringComparison.Ordinal))
        {
            return SameTrack(origin);
        }

        return Find(
            Compile(edges),
            junctionSelectedBranch,
            origin,
            dest,
            classFor,
            skipPlainOnMultiBranchStem,
            destYardId,
            yardFor,
            mode,
            spatial,
            consistLengthMeters);
    }

    /// <summary>Adjacency compiled once for many origin/dest Finds (HTP matrix dump).</summary>
    public static PathPlanGraph Compile(IReadOnlyList<PathEdge>? edges) =>
        new(BuildAdjacency(edges ?? Array.Empty<PathEdge>()));

    /// <summary>
    /// Tracks that can reach at least one anchor (reverse BFS). Drops nodes
    /// with no walk to a named spur. Not the same as Yard-mode destYard NoPath
    /// (SW dump: #Y-#S1779#T still reverse-reaches some named rail).
    /// </summary>
    public static HashSet<string> TracksThatCanReach(
        PathPlanGraph graph,
        IEnumerable<string>? anchors)
    {
        var keep = new HashSet<string>(StringComparer.Ordinal);
        if (graph.Adj == null || anchors == null)
        {
            return keep;
        }

        var reverse = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var kv in graph.Adj)
        {
            var from = kv.Key;
            var hops = kv.Value;
            if (hops == null)
            {
                continue;
            }

            for (var i = 0; i < hops.Count; i++)
            {
                var to = hops[i].ToTrackId?.Trim();
                if (string.IsNullOrEmpty(to))
                {
                    continue;
                }

                if (!reverse.TryGetValue(to, out var prevs))
                {
                    prevs = new List<string>();
                    reverse[to] = prevs;
                }

                prevs.Add(from);
            }
        }

        var q = new Queue<string>();
        foreach (var raw in anchors)
        {
            var a = Normalize(raw);
            if (a == null)
            {
                continue;
            }

            if (keep.Add(a))
            {
                q.Enqueue(a);
            }
        }

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (!reverse.TryGetValue(cur, out var prevs))
            {
                continue;
            }

            for (var i = 0; i < prevs.Count; i++)
            {
                var p = prevs[i];
                if (keep.Add(p))
                {
                    q.Enqueue(p);
                }
            }
        }

        return keep;
    }

    /// <summary>
    /// Named rails of <paramref name="yardId"/> plus anonymous hops that appear
    /// on Yard-mode shortest paths between those named rails. Does not flood
    /// the anonymous mainline into every town matrix.
    /// </summary>
    public static HashSet<string> YardConnectedBlob(
        PathPlanGraph graph,
        string? yardId,
        Func<string, string?>? yardFor) =>
        YardConnectedBlob(graph, null, yardId, yardFor);

    public static HashSet<string> YardConnectedBlob(
        PathPlanGraph graph,
        IReadOnlyDictionary<string, int>? junctionSelectedBranch,
        string? yardId,
        Func<string, string?>? yardFor)
    {
        var keep = new HashSet<string>(StringComparer.Ordinal);
        var yard = yardId?.Trim();
        if (graph.Adj == null || string.IsNullOrEmpty(yard))
        {
            return keep;
        }

        yardFor ??= PathRouteConstraints.YardIdOf;
        var selected = junctionSelectedBranch
            ?? new Dictionary<string, int>(StringComparer.Ordinal);
        var named = new List<string>();
        void SeedNamed(string? track)
        {
            var id = Normalize(track);
            if (id == null || !string.Equals(yardFor(id), yard, StringComparison.Ordinal))
            {
                return;
            }

            if (keep.Add(id))
            {
                named.Add(id);
            }
        }

        foreach (var kv in graph.Adj)
        {
            SeedNamed(kv.Key);
            var hops = kv.Value;
            if (hops == null)
            {
                continue;
            }

            for (var i = 0; i < hops.Count; i++)
            {
                SeedNamed(hops[i].ToTrackId);
            }
        }

        for (var i = 0; i < named.Count; i++)
        {
            for (var j = 0; j < named.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var plan = Find(
                    graph,
                    selected,
                    named[i],
                    named[j],
                    destYardId: yard,
                    yardFor: yardFor,
                    mode: PathPlanMode.Yard);
                if (plan.Status == PathCheckStatus.NoPath
                    || plan.TrackIds == null)
                {
                    continue;
                }

                for (var t = 0; t < plan.TrackIds.Count; t++)
                {
                    var hop = Normalize(plan.TrackIds[t]);
                    if (hop != null)
                    {
                        keep.Add(hop);
                    }
                }
            }
        }

        return keep;
    }

    public static PathPlanResult Find(
        PathPlanGraph graph,
        IReadOnlyDictionary<string, int> junctionSelectedBranch,
        string? originTrackId,
        string? destinationTrackId,
        Func<string, PathTrackClass>? classFor = null,
        bool skipPlainOnMultiBranchStem = true,
        string? destYardId = null,
        Func<string, string?>? yardFor = null,
        PathPlanMode mode = PathPlanMode.World,
        SpatialGraph spatial = default,
        float consistLengthMeters = 0f)
    {
        var dest = Normalize(destinationTrackId);
        if (dest == null)
        {
            return Empty(PathCheckStatus.NoDestination);
        }

        var origin = Normalize(originTrackId);
        if (origin == null)
        {
            return Empty(PathCheckStatus.NoOrigin);
        }

        if (string.Equals(origin, dest, StringComparison.Ordinal))
        {
            return SameTrack(origin);
        }

        var adj = graph.Adj;
        if (adj == null)
        {
            return Empty(PathCheckStatus.NoPath);
        }

        if (!TryAStar(
                adj,
                origin,
                dest,
                classFor,
                skipPlainOnMultiBranchStem,
                destYardId,
                yardFor,
                mode,
                spatial,
                consistLengthMeters,
                out var path,
                out var totalCost,
                out var spatialPenalty,
                out var zoneBlock))
        {
            return Empty(PathCheckStatus.NoPath);
        }

        if (spatial.HasCoordinates
            && TryPickCostBandPath(
                adj,
                origin,
                dest,
                classFor,
                skipPlainOnMultiBranchStem,
                destYardId,
                yardFor,
                mode,
                spatial,
                out var bandPath,
                out var bandCost)
            && !PathReentersOriginLadder(adj, bandPath, spatial, origin, dest))
        {
            path = bandPath;
            totalCost = bandCost;
            spatialPenalty = 0f;
        }

        var junctionEvals = new List<PathJunctionEval>();
        var seenJunctions = new HashSet<string>(StringComparer.Ordinal);
        var misaligned = 0;
        var reverseCount = 0;
        var lastReverse = false;
        var selected = junctionSelectedBranch ?? new Dictionary<string, int>();

        for (var i = 0; i < path.Count - 1; i++)
        {
            var from = path[i];
            var to = path[i + 1];
            if (!TryGetHop(adj, from, to, out var hop))
            {
                continue;
            }

            if (hop.RequiresReverse)
            {
                reverseCount++;
                if (i == path.Count - 2)
                {
                    lastReverse = true;
                }
            }

            AddUniqueJunctionEval(junctionEvals, seenJunctions, ref misaligned, hop, selected);
        }

        TryFindJunctionFirstStop(path, adj, out var firstStop, out var approachFrom);
        var status = misaligned == 0 ? PathCheckStatus.Aligned : PathCheckStatus.Misaligned;
        return new PathPlanResult(
            status,
            path,
            junctionEvals,
            misaligned,
            reverseCount,
            lastReverse,
            totalCost,
            firstStop,
            approachFrom,
            spatialPenalty,
            zoneBlock);
    }

    private static PathPlanResult SameTrack(string origin) =>
        new(
            PathCheckStatus.Aligned,
            new[] { origin },
            Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            0f);

    /// <summary>
    /// Walk corridor hops; when a junction is required at a different branch than an
    /// earlier hop committed, that switch is the junction-first stop (approach pin).
    /// </summary>
    private static bool TryFindJunctionFirstStop(
        IReadOnlyList<string> trackIds,
        Dictionary<string, List<PathEdge>> adj,
        out PathJunctionFirstStop? stop,
        out Dictionary<string, string> firstApproachFrom)
    {
        stop = null;
        firstApproachFrom = new Dictionary<string, string>(StringComparer.Ordinal);
        if (trackIds == null || trackIds.Count < 2 || adj == null)
        {
            return false;
        }

        var committed = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var from = Normalize(trackIds[i]);
            var to = Normalize(trackIds[i + 1]);
            if (from == null || to == null || !TryGetHop(adj, from, to, out var hop))
            {
                continue;
            }

            if (!hop.HasJunction || hop.JunctionId == null)
            {
                continue;
            }

            if (!firstApproachFrom.ContainsKey(hop.JunctionId))
            {
                firstApproachFrom[hop.JunctionId] = from;
            }

            if (committed.TryGetValue(hop.JunctionId, out var prior)
                && prior != hop.RequiredBranch)
            {
                firstApproachFrom.TryGetValue(hop.JunctionId, out var approachFrom);
                stop = new PathJunctionFirstStop(
                    hop.JunctionId,
                    hop.RequiredBranch,
                    approachFrom ?? from,
                    to);
                return true;
            }

            committed[hop.JunctionId] = hop.RequiredBranch;
        }

        return false;
    }

    /// <summary>
    /// Junction flips still needed before the path is clear.
    /// One throw per junction (first required branch along the corridor) — Align cannot
    /// set two branches on the same points.
    /// </summary>
    public static IReadOnlyList<PathJunctionEval> RequiredFlips(PathPlanResult plan)
    {
        if (plan == null || plan.Junctions.Count == 0)
        {
            return Array.Empty<PathJunctionEval>();
        }

        var list = new List<PathJunctionEval>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var j in plan.Junctions)
        {
            if (j.Aligned || string.IsNullOrEmpty(j.JunctionId))
            {
                continue;
            }

            if (!seen.Add(j.JunctionId))
            {
                continue;
            }

            list.Add(j);
        }

        return list;
    }

    /// <summary>
    /// Re-check junction alignment along a frozen corridor (post-Align) without re-Dijkstra.
    /// Preserves TrackIds / reverse cues from the plan that was thrown.
    /// </summary>
    public static PathPlanResult ReevaluateAlong(
        IReadOnlyList<string> trackIds,
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> junctionSelectedBranch,
        Func<string, PathTrackClass>? classFor = null)
    {
        if (trackIds == null || trackIds.Count == 0)
        {
            return Empty(PathCheckStatus.NoPath);
        }

        if (trackIds.Count == 1)
        {
            return new PathPlanResult(
                PathCheckStatus.Aligned,
                trackIds,
                Array.Empty<PathJunctionEval>(),
                0,
                0,
                false,
                0f);
        }

        var adj = BuildAdjacency(edges);
        var selected = junctionSelectedBranch ?? new Dictionary<string, int>();
        var junctionEvals = new List<PathJunctionEval>();
        var seenJunctions = new HashSet<string>(StringComparer.Ordinal);
        var misaligned = 0;
        var reverseCount = 0;
        var lastReverse = false;
        var totalCost = 0f;

        var origin = Normalize(trackIds[0]);
        var dest = Normalize(trackIds[trackIds.Count - 1]) ?? string.Empty;
        var originYard = PathRouteConstraints.YardIdOf(origin);
        var destYard = PathRouteConstraints.YardIdOf(dest);

        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            var from = Normalize(trackIds[i]);
            var to = Normalize(trackIds[i + 1]);
            if (from == null || to == null || !TryGetHop(adj, from, to, out var hop))
            {
                continue;
            }

            if (!TryStepCost(hop, to, dest, originYard, destYard, classFor, out var step))
            {
                // Corridor became illegal under forward-only rules — keep structure, omit cost.
                step = hop.Cost;
            }

            totalCost += step;
            if (hop.RequiresReverse)
            {
                reverseCount++;
                if (i == trackIds.Count - 2)
                {
                    lastReverse = true;
                }
            }

            // First required branch along the corridor wins — dual W-0416:0 then :1
            // must not leave Align flipping the same points forever.
            AddUniqueJunctionEval(junctionEvals, seenJunctions, ref misaligned, hop, selected);
        }

        TryFindJunctionFirstStop(trackIds, adj, out var firstStop, out var approachFrom);
        var status = misaligned == 0 ? PathCheckStatus.Aligned : PathCheckStatus.Misaligned;
        return new PathPlanResult(
            status,
            trackIds,
            junctionEvals,
            misaligned,
            reverseCount,
            lastReverse,
            totalCost,
            firstStop,
            approachFrom);
    }

    /// <summary>
    /// One eval per junction id (first hop along the corridor). Later hops that
    /// re-tag the same points with another branch are ignored for Align/HUD.
    /// </summary>
    private static void AddUniqueJunctionEval(
        List<PathJunctionEval> junctionEvals,
        HashSet<string> seenJunctions,
        ref int misaligned,
        PathEdge hop,
        IReadOnlyDictionary<string, int> selected)
    {
        if (!hop.HasJunction || hop.JunctionId == null || !seenJunctions.Add(hop.JunctionId))
        {
            return;
        }

        selected.TryGetValue(hop.JunctionId, out var actual);
        var eval = new PathJunctionEval(hop.JunctionId, hop.RequiredBranch, actual);
        junctionEvals.Add(eval);
        if (!eval.Aligned)
        {
            misaligned++;
        }
    }

    private static PathPlanResult Empty(PathCheckStatus status) =>
        new(status, Array.Empty<string>(), Array.Empty<PathJunctionEval>(), 0, 0, false, 0f);

    private static string? Normalize(string? id)
    {
        var t = id?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    private static Dictionary<string, List<PathEdge>> BuildAdjacency(IReadOnlyList<PathEdge> edges)
    {
        var adj = new Dictionary<string, List<PathEdge>>(StringComparer.Ordinal);
        if (edges == null)
        {
            return adj;
        }

        foreach (var edge in edges)
        {
            var from = Normalize(edge.FromTrackId);
            var to = Normalize(edge.ToTrackId);
            if (from == null || to == null)
            {
                continue;
            }

            var normalized = new PathEdge(
                from,
                to,
                edge.JunctionId,
                edge.RequiredBranch,
                edge.Cost,
                edge.RequiresReverse,
                edge.LengthMeters);
            if (!adj.TryGetValue(from, out var list))
            {
                list = new List<PathEdge>();
                adj[from] = list;
            }

            list.Add(normalized);
        }

        return adj;
    }

    /// <summary>
    /// Among time paths within <see cref="CostBandSeconds"/> of the cheapest,
    /// keep the one whose first frog is closest to the destination.
    /// Hop cost stays seconds. Returns false when coordinates cannot place the dest.
    /// </summary>
    private static bool TryPickCostBandPath(
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string dest,
        Func<string, PathTrackClass>? classFor,
        bool skipPlainOnMultiBranchStem,
        string? destYardId,
        Func<string, string?>? yardFor,
        PathPlanMode mode,
        SpatialGraph spatial,
        out List<string> path,
        out float totalCost)
    {
        path = new List<string>();
        totalCost = 0f;
        var trackXz = BuildTrackXzLookup(adj, spatial);
        if (!trackXz.TryGetValue(dest, out var destXz))
        {
            return false;
        }

        string? YardOf(string id) => yardFor?.Invoke(id) ?? PathRouteConstraints.YardIdOf(id);
        var originYard = YardOf(origin);
        var destYard = !string.IsNullOrWhiteSpace(destYardId)
            ? destYardId!.Trim()
            : YardOf(dest);
        var enforceJunctionCommitment = mode != PathPlanMode.Yard;
        var originStem = skipPlainOnMultiBranchStem
            && adj.TryGetValue(origin, out var originHops)
            && IsMultiBranchJunctionStem(originHops);

        var costPlain = new Dictionary<string, float>(StringComparer.Ordinal) { [origin] = 0f };
        var plainParent = new Dictionary<string, string>(StringComparer.Ordinal);
        var plainOpen = new List<string> { origin };
        while (plainOpen.Count > 0)
        {
            var current = PopLowest(plainOpen, costPlain);
            if (!adj.TryGetValue(current, out var hops))
            {
                continue;
            }

            var blockPlain = originStem && string.Equals(current, origin, StringComparison.Ordinal);
            foreach (var hop in hops)
            {
                if (hop.HasJunction || blockPlain)
                {
                    continue;
                }

                if (!TryStepCost(hop, hop.ToTrackId, dest, originYard, destYard, classFor, out var step, yardFor))
                {
                    continue;
                }

                Relax(costPlain, plainParent, plainOpen, current, hop.ToTrackId, costPlain[current] + step);
            }
        }

        var incoming = new Dictionary<string, List<PathEdge>>(StringComparer.Ordinal);
        foreach (var kv in adj)
        {
            var hops = kv.Value;
            if (hops == null)
            {
                continue;
            }

            foreach (var hop in hops)
            {
                var to = hop.ToTrackId;
                if (string.IsNullOrEmpty(to))
                {
                    continue;
                }

                if (!incoming.TryGetValue(to, out var list))
                {
                    list = new List<PathEdge>();
                    incoming[to] = list;
                }

                list.Add(hop);
            }
        }

        var costToDest = new Dictionary<string, float>(StringComparer.Ordinal) { [dest] = 0f };
        var reverseNext = new Dictionary<string, string>(StringComparer.Ordinal);
        var reverseOpen = new List<string> { dest };
        while (reverseOpen.Count > 0)
        {
            var current = PopLowest(reverseOpen, costToDest);
            if (!incoming.TryGetValue(current, out var hops))
            {
                continue;
            }

            foreach (var hop in hops)
            {
                var prev = hop.FromTrackId;
                if (string.IsNullOrEmpty(prev))
                {
                    continue;
                }

                if (originStem
                    && string.Equals(prev, origin, StringComparison.Ordinal)
                    && !hop.HasJunction)
                {
                    continue;
                }

                if (!TryStepCost(hop, current, dest, originYard, destYard, classFor, out var step, yardFor))
                {
                    continue;
                }

                Relax(costToDest, reverseNext, reverseOpen, current, prev, costToDest[current] + step);
            }
        }

        if (!costToDest.TryGetValue(origin, out var cheapest))
        {
            return false;
        }

        var cap = cheapest + CostBandSeconds;
        var bestDist = float.MaxValue;
        var bestCost = float.MaxValue;
        List<string>? bestPath = null;

        foreach (var kv in adj)
        {
            var from = kv.Key;
            if (!costPlain.TryGetValue(from, out var plainCost))
            {
                continue;
            }

            var hops = kv.Value;
            if (hops == null)
            {
                continue;
            }

            foreach (var hop in hops)
            {
                if (!hop.HasJunction || hop.JunctionId == null)
                {
                    continue;
                }

                var next = hop.ToTrackId;
                if (!TryStepCost(hop, next, dest, originYard, destYard, classFor, out var step, yardFor))
                {
                    continue;
                }

                if (!costToDest.TryGetValue(next, out var tail))
                {
                    continue;
                }

                var total = plainCost + step + tail;
                if (total > cap)
                {
                    continue;
                }

                if (!spatial.TryGetJunctionXz(hop.JunctionId, out var jx, out var jz))
                {
                    continue;
                }

                var dx = jx - destXz.x;
                var dz = jz - destXz.z;
                var dist = (dx * dx) + (dz * dz);
                if (dist > bestDist || (dist == bestDist && total >= bestCost))
                {
                    continue;
                }

                var candidate = BuildBandPath(origin, dest, from, next, plainParent, reverseNext);
                if (candidate == null)
                {
                    continue;
                }

                if (enforceJunctionCommitment && PathReusesJunction(adj, candidate))
                {
                    continue;
                }

                bestDist = dist;
                bestCost = total;
                bestPath = candidate;
            }
        }

        if (bestPath == null)
        {
            return false;
        }

        path = bestPath;
        totalCost = bestCost;
        return true;
    }

    private static List<string>? BuildBandPath(
        string origin,
        string dest,
        string from,
        string next,
        Dictionary<string, string> plainParent,
        Dictionary<string, string> reverseNext)
    {
        var prefix = new List<string>();
        var node = from;
        var guard = 0;
        while (!string.Equals(node, origin, StringComparison.Ordinal))
        {
            if (++guard > 10000 || !plainParent.TryGetValue(node, out var prev))
            {
                return null;
            }

            prefix.Add(node);
            node = prev;
        }

        prefix.Add(origin);
        prefix.Reverse();
        if (!string.Equals(prefix[prefix.Count - 1], next, StringComparison.Ordinal))
        {
            prefix.Add(next);
        }

        node = next;
        while (!string.Equals(node, dest, StringComparison.Ordinal))
        {
            if (++guard > 10000 || !reverseNext.TryGetValue(node, out var step))
            {
                return null;
            }

            node = step;
            prefix.Add(node);
        }

        return prefix;
    }

    private static bool PathReusesJunction(
        Dictionary<string, List<PathEdge>> adj,
        List<string> trackIds)
    {
        var committed = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < trackIds.Count - 1; i++)
        {
            if (!TryGetHop(adj, trackIds[i], trackIds[i + 1], out var hop)
                || !hop.HasJunction
                || hop.JunctionId == null)
            {
                continue;
            }

            if (committed.TryGetValue(hop.JunctionId, out var prior) && prior != hop.RequiredBranch)
            {
                return true;
            }

            committed[hop.JunctionId] = hop.RequiredBranch;
        }

        return false;
    }

    private static void Relax(
        Dictionary<string, float> cost,
        Dictionary<string, string> parent,
        List<string> open,
        string from,
        string to,
        float newCost)
    {
        if (cost.TryGetValue(to, out var old) && newCost >= old)
        {
            return;
        }

        cost[to] = newCost;
        parent[to] = from;
        if (!open.Contains(to))
        {
            open.Add(to);
        }
    }

    private static string PopLowest(List<string> open, Dictionary<string, float> cost)
    {
        var bestIdx = 0;
        var best = cost[open[0]];
        for (var i = 1; i < open.Count; i++)
        {
            var c = cost[open[i]];
            if (c < best)
            {
                best = c;
                bestIdx = i;
            }
        }

        var node = open[bestIdx];
        open.RemoveAt(bestIdx);
        return node;
    }

    /// <summary>
    /// A* pathfinder with optional spatial heuristic. Falls back to Dijkstra when
    /// <paramref name="spatial"/> has no coordinates.
    /// When <paramref name="consistLengthMeters"/> &gt; 0, reverse hops whose
    /// prior run-up (sum of <see cref="PathEdge.LengthMeters"/>) is shorter than
    /// the consist are skipped (pull-past frog clearance).
    /// </summary>
    private static bool TryAStar(
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string dest,
        Func<string, PathTrackClass>? classFor,
        bool skipPlainOnMultiBranchStem,
        string? destYardId,
        Func<string, string?>? yardFor,
        PathPlanMode mode,
        SpatialGraph spatial,
        float consistLengthMeters,
        out List<string> path,
        out float totalCost,
        out float spatialPenalty,
        out string? zoneBlockLog)
    {
        path = new List<string>();
        totalCost = 0f;
        spatialPenalty = 0f;
        zoneBlockLog = null;

        // Time cost only. A per-meter away penalty rewrote every prep (cab 2.16.17:
        // throat→SW-C4S became 7 switches / 4964s). Haul preference waits for a
        // gate that does not touch the pickup reverses.
        var trackXz = BuildTrackXzLookup(adj, spatial);
        var hasDestXz = trackXz.TryGetValue(dest, out var destXz);

        var costSoFar = new Dictionary<string, float>(StringComparer.Ordinal) { [origin] = 0f };
        var fScore = new Dictionary<string, float>(StringComparer.Ordinal);
        fScore[origin] = Heuristic(origin, destXz, trackXz, hasDestXz);

        var cameFrom = new Dictionary<string, string>(StringComparer.Ordinal) { [origin] = origin };
        var open = new List<string> { origin };
        string? YardOf(string id) => yardFor?.Invoke(id) ?? PathRouteConstraints.YardIdOf(id);
        var originYard = YardOf(origin);
        var destYard = !string.IsNullOrWhiteSpace(destYardId)
            ? destYardId!.Trim()
            : YardOf(dest);
        var enforceJunctionCommitment = mode != PathPlanMode.Yard;
        var ladders = YardLadderZones.Build(spatial);
        var originLadder = LadderOfOutgoing(adj, ladders, origin);
        var destLadder = LadderOfIncoming(adj, ladders, dest, originLadder);
        var entryLadder = new Dictionary<string, int>(StringComparer.Ordinal);
        if (originLadder >= 0)
        {
            entryLadder[origin] = originLadder;
        }

        while (open.Count > 0)
        {
            // A*: pick by fScore (costSoFar + heuristic), not just costSoFar
            var bestIdx = 0;
            var bestF = fScore.TryGetValue(open[0], out var f0) ? f0 : costSoFar[open[0]];
            for (var i = 1; i < open.Count; i++)
            {
                var f = fScore.TryGetValue(open[i], out var fi) ? fi : costSoFar[open[i]];
                if (f < bestF)
                {
                    bestF = f;
                    bestIdx = i;
                }
            }

            var current = open[bestIdx];
            open.RemoveAt(bestIdx);

            if (string.Equals(current, dest, StringComparison.Ordinal))
            {
                path = Reconstruct(cameFrom, origin, dest);
                totalCost = costSoFar[dest];
                return true;
            }

            if (!adj.TryGetValue(current, out var hops))
            {
                continue;
            }

            // At the loco's origin throat, ignore a duplicate plain shortcut so A*
            // must choose an actual junction branch. Do not repeat this downstream:
            // branch rails can also look like stems, and their plain edge is the continuation.
            var junctionStem = skipPlainOnMultiBranchStem
                && string.Equals(current, origin, StringComparison.Ordinal)
                && IsMultiBranchJunctionStem(hops);

            foreach (var hop in hops)
            {
                if (junctionStem && !hop.HasJunction)
                {
                    continue;
                }

                // World: a single turnout cannot be both 0 and 1 on one corridor (W-0416).
                // Yard: dense #Y mesh breaks cheapest-path substructure — do not hard-skip.
                if (enforceJunctionCommitment
                    && hop.HasJunction
                    && hop.JunctionId != null
                    && ConflictsJunctionCommitment(
                        cameFrom,
                        adj,
                        origin,
                        current,
                        hop.JunctionId,
                        hop.RequiredBranch))
                {
                    continue;
                }

                // Pull-past: reverse only if prior directional run-up fits the consist.
                if (hop.RequiresReverse
                    && consistLengthMeters > 0f
                    && !ReverseRunUpFitsConsist(
                        cameFrom,
                        adj,
                        origin,
                        current,
                        consistLengthMeters))
                {
                    continue;
                }

                var next = hop.ToTrackId;
                if (!TryStepCost(
                        hop, next, dest, originYard, destYard, classFor, out var step, yardFor))
                {
                    continue; // forward-only hard ban outside dest / same-town
                }

                if (ladders.TryZone(hop.JunctionId, out var hopLadder)
                    && entryLadder.TryGetValue(current, out var fromLadder)
                    && ladders.BlocksReentry(fromLadder, hopLadder, originLadder, destLadder))
                {
                    zoneBlockLog ??= PathGraphTelemetry.FormatZoneBlock(
                        hop.JunctionId,
                        ladders.NameOf(originLadder));
                    continue;
                }

                var newCost = costSoFar[current] + step;
                if (costSoFar.TryGetValue(next, out var old) && newCost >= old)
                {
                    continue;
                }

                costSoFar[next] = newCost;
                fScore[next] = newCost + Heuristic(next, destXz, trackXz, hasDestXz);
                cameFrom[next] = current;
                if (ladders.TryZone(hop.JunctionId, out var entered))
                {
                    entryLadder[next] = entered;
                }
                else if (entryLadder.TryGetValue(current, out var carry))
                {
                    entryLadder[next] = carry;
                }
                if (!open.Contains(next))
                {
                    open.Add(next);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Sum <see cref="PathEdge.LengthMeters"/> walking <paramref name="cameFrom"/>
    /// backward from <paramref name="current"/> until origin or a prior reverse hop.
    /// Unknown length (0) → fail-open (true). Else require sum ≥ consist.
    /// </summary>
    private static bool ReverseRunUpFitsConsist(
        Dictionary<string, string> cameFrom,
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string current,
        float consistLengthMeters)
    {
        var summed = 0f;
        var node = current;
        var guard = 0;
        while (guard++ < 4096)
        {
            if (string.Equals(node, origin, StringComparison.Ordinal))
            {
                break;
            }

            if (!cameFrom.TryGetValue(node, out var prev)
                || string.IsNullOrEmpty(prev)
                || string.Equals(prev, node, StringComparison.Ordinal))
            {
                break;
            }

            if (!TryGetHop(adj, prev, node, out var prior))
            {
                return true; // unknown topology — fail-open
            }

            // Prior reverse starts a new directional movement — stop before adding it.
            if (prior.RequiresReverse)
            {
                break;
            }

            var len = prior.LengthMeters;
            if (len <= 0f)
            {
                return true; // harvest without meters — fail-open
            }

            summed += len;
            node = prev;
        }

        return summed >= consistLengthMeters;
    }

    /// <summary>
    /// Build approximate track XZ from junction coordinates. Each track inherits
    /// the position of the first junction it connects through.
    /// </summary>
    private static Dictionary<string, (float x, float z)> BuildTrackXzLookup(
        Dictionary<string, List<PathEdge>> adj,
        SpatialGraph spatial)
    {
        var lookup = new Dictionary<string, (float x, float z)>(StringComparer.Ordinal);
        if (!spatial.HasCoordinates || adj == null)
        {
            return lookup;
        }

        foreach (var kv in adj)
        {
            var from = kv.Key;
            var hops = kv.Value;
            if (hops == null)
            {
                continue;
            }

            foreach (var hop in hops)
            {
                if (!hop.HasJunction || hop.JunctionId == null)
                {
                    continue;
                }

                if (!spatial.TryGetJunctionXz(hop.JunctionId, out var x, out var z))
                {
                    continue;
                }

                // Associate junction XZ with both connected tracks
                if (!lookup.ContainsKey(from))
                {
                    lookup[from] = (x, z);
                }

                var to = hop.ToTrackId;
                if (!string.IsNullOrEmpty(to) && !lookup.ContainsKey(to))
                {
                    lookup[to] = (x, z);
                }
            }
        }

        return lookup;
    }

    /// <summary>
    /// <summary>
    /// Euclidean distance heuristic for A*. Returns 0 if coordinates unavailable
    /// (falls back to Dijkstra behavior).
    /// </summary>
    private static float Heuristic(
        string trackId,
        (float x, float z) destXz,
        Dictionary<string, (float x, float z)> trackXz,
        bool hasDestXz)
    {
        if (!hasDestXz)
        {
            return 0f;
        }

        if (!trackXz.TryGetValue(trackId, out var pos))
        {
            return 0f;
        }

        var dx = pos.x - destXz.x;
        var dz = pos.z - destXz.z;
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }


    /// <summary>
    /// True when the path origin→current already committed <paramref name="junctionId"/>
    /// to a different branch than <paramref name="requiredBranch"/>.
    /// </summary>
    private static bool ConflictsJunctionCommitment(
        Dictionary<string, string> cameFrom,
        Dictionary<string, List<PathEdge>> adj,
        string origin,
        string current,
        string junctionId,
        int requiredBranch)
    {
        var node = current;
        var guard = 0;
        while (!string.Equals(node, origin, StringComparison.Ordinal))
        {
            if (!cameFrom.TryGetValue(node, out var prev) || ++guard > 10000)
            {
                return false;
            }

            if (TryGetHop(adj, prev, node, out var prior)
                && prior.HasJunction
                && string.Equals(prior.JunctionId, junctionId, StringComparison.Ordinal)
                && prior.RequiredBranch != requiredBranch)
            {
                return true;
            }

            node = prev;
        }

        return false;
    }

    /// <summary>
    /// True when this node is a turnout stem: 2+ outbound junction hops (different branches).
    /// </summary>
    public static bool IsMultiBranchJunctionStem(IReadOnlyList<PathEdge> hops)
    {
        if (hops == null)
        {
            return false;
        }

        var junctionOuts = 0;
        for (var i = 0; i < hops.Count; i++)
        {
            if (hops[i].HasJunction)
            {
                junctionOuts++;
                if (junctionOuts >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int LadderOfOutgoing(
        Dictionary<string, List<PathEdge>> adj,
        YardLadderZones ladders,
        string track)
    {
        if (!ladders.Active || !adj.TryGetValue(track, out var hops) || hops == null)
        {
            return -1;
        }

        for (var i = 0; i < hops.Count; i++)
        {
            if (ladders.TryZone(hops[i].JunctionId, out var zone))
            {
                return zone;
            }
        }

        return -1;
    }

    /// <summary>
    /// Ladder of the destination. If any arrival junction still sits in the
    /// origin ladder, the destination counts as that ladder so a pull-past
    /// sawtooth is not a re-entry.
    /// </summary>
    private static int LadderOfIncoming(
        Dictionary<string, List<PathEdge>> adj,
        YardLadderZones ladders,
        string dest,
        int originLadder)
    {
        if (!ladders.Active)
        {
            return -1;
        }

        var found = -1;
        foreach (var kv in adj)
        {
            var hops = kv.Value;
            if (hops == null)
            {
                continue;
            }

            for (var i = 0; i < hops.Count; i++)
            {
                if (!string.Equals(hops[i].ToTrackId, dest, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!ladders.TryZone(hops[i].JunctionId, out var zone))
                {
                    continue;
                }

                if (zone == originLadder)
                {
                    return originLadder;
                }

                if (found < 0)
                {
                    found = zone;
                }
            }
        }

        return found;
    }

    private static bool PathReentersOriginLadder(
        Dictionary<string, List<PathEdge>> adj,
        IReadOnlyList<string> path,
        SpatialGraph spatial,
        string origin,
        string dest)
    {
        var ladders = YardLadderZones.Build(spatial);
        var originLadder = LadderOfOutgoing(adj, ladders, origin);
        var destLadder = LadderOfIncoming(adj, ladders, dest, originLadder);
        if (originLadder < 0 || destLadder < 0 || destLadder == originLadder || path == null)
        {
            return false;
        }

        var entry = originLadder;
        for (var i = 0; i < path.Count - 1; i++)
        {
            if (!TryGetHop(adj, path[i], path[i + 1], out var hop))
            {
                continue;
            }

            if (!ladders.TryZone(hop.JunctionId, out var hopLadder))
            {
                continue;
            }

            if (ladders.BlocksReentry(entry, hopLadder, originLadder, destLadder))
            {
                return true;
            }

            entry = hopLadder;
        }

        return false;
    }

    /// <summary>
    /// Edge step cost with pass-through rules. Returns false when the hop is hard-banned
    /// (reverse outside destination yard / dest track / same-town). Public for Tier 2 think dumps.
    /// </summary>
    public static bool TryStepCost(
        PathEdge hop,
        string nextTrackId,
        string destTrackId,
        string? originYardId,
        string? destYardId,
        Func<string, PathTrackClass>? classFor,
        out float stepSeconds,
        Func<string, string?>? yardFor = null)
    {
        stepSeconds = hop.Cost;
        var nextYard = yardFor?.Invoke(nextTrackId) ?? PathRouteConstraints.YardIdOf(nextTrackId);
        var inDestYard = nextYard != null
            && destYardId != null
            && string.Equals(nextYard, destYardId, StringComparison.OrdinalIgnoreCase);
        var isDestTrack = string.Equals(nextTrackId, destTrackId, StringComparison.Ordinal);
        var sameTown = originYardId != null
            && destYardId != null
            && string.Equals(originYardId, destYardId, StringComparison.OrdinalIgnoreCase);

        // HARD BAN: reverse only into dest yard / dest track, or any reverse when same-town
        // (Town TT Align — anonymous #Y dest with session yard).
        if (hop.RequiresReverse && !inDestYard && !isDestTrack && !sameTown)
        {
            return false;
        }

        if (classFor != null)
        {
            var toClass = classFor(nextTrackId);
            if (toClass == PathTrackClass.SpurPocket)
            {
                var inOriginYard = nextYard != null
                    && originYardId != null
                    && string.Equals(nextYard, originYardId, StringComparison.OrdinalIgnoreCase);
                if (inDestYard || inOriginYard || isDestTrack)
                {
                    stepSeconds += PathTrackCosts.SpurOccupancyPenaltySeconds * 0.5f;
                }
                else
                {
                    stepSeconds += PathTrackCosts.SpurOccupancyPenaltySeconds;
                }
            }
            else if (toClass == PathTrackClass.Unknown || toClass == PathTrackClass.YardService)
            {
                stepSeconds += PathTrackCosts.NonThroughPenaltySeconds;
            }
        }

        if (hop.RequiresReverse)
        {
            stepSeconds += PathTrackCosts.ReversePenalty;
        }

        return true;
    }

    private static List<string> Reconstruct(
        Dictionary<string, string> cameFrom,
        string origin,
        string dest)
    {
        var path = new List<string>();
        var current = dest;
        path.Add(current);
        while (!string.Equals(current, origin, StringComparison.Ordinal))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private static bool TryGetHop(
        Dictionary<string, List<PathEdge>> adj,
        string from,
        string to,
        out PathEdge hop)
    {
        hop = default;
        if (!adj.TryGetValue(from, out var hops))
        {
            return false;
        }

        PathEdge? plain = null;
        foreach (var candidate in hops)
        {
            if (!string.Equals(candidate.ToTrackId, to, StringComparison.Ordinal))
            {
                continue;
            }

            if (candidate.HasJunction)
            {
                hop = candidate;
                return true;
            }

            plain ??= candidate;
        }

        if (plain == null)
        {
            return false;
        }

        hop = plain.Value;
        return true;
    }
}
