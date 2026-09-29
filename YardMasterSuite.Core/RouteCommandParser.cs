using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Translates <see cref="PathPlanResult"/> hops into a sawtooth command queue
/// (Gemini Step 7). Spec only — does not write Unity / cab controls.
/// </summary>
public static class RouteCommandParser
{
    /// <summary>
    /// Walk <paramref name="plan"/>.TrackIds. A reverse hop (harvest <see cref="PathEdge.RequiresReverse"/>
    /// or a branch → stem → branch pivot on one junction) starts a new direction:
    /// Stop → ThrowSwitch → ChangeDirection, then Drive.
    /// Consecutive hops in the same direction collapse into one Drive leg.
    /// </summary>
    /// <param name="edges">Graph edges; only hops on the plan are read, so the full graph is fine.</param>
    /// <param name="startReverse">Loco must leave the origin in reverse (world facing).</param>
    public static IReadOnlyList<LocoCommand> Generate(
        PathPlanResult? plan,
        IReadOnlyList<PathEdge>? edges = null,
        bool startReverse = false)
    {
        if (plan == null
            || plan.TrackIds == null
            || plan.TrackIds.Count < 2
            || (plan.Status != PathCheckStatus.Aligned
                && plan.Status != PathCheckStatus.Misaligned))
        {
            return Array.Empty<LocoCommand>();
        }

        var tracks = plan.TrackIds;
        var hopLookup = BuildHopLookup(tracks, edges);
        var cmds = new List<LocoCommand>(8);
        var travelReverse = startReverse;

        for (var i = 0; i < tracks.Count - 1; i++)
        {
            var from = tracks[i];
            var to = tracks[i + 1];
            var isLastHop = i == tracks.Count - 2;
            var requiresReverse = HopRequiresReverse(hopLookup, from, to, plan, isLastHop)
                || (i > 0 && StemPivot(hopLookup, tracks[i - 1], from, to));

            if (requiresReverse)
            {
                cmds.Add(new LocoCommand(LocoCommandAction.Stop));
                var incomingFrom = i > 0 ? tracks[i - 1] : null;
                var junction = HopJunction(hopLookup, from, to, out var branch);
                if (junction == null && incomingFrom != null)
                {
                    junction = HopJunction(hopLookup, incomingFrom, from, out branch);
                }

                cmds.Add(junction != null
                    ? new LocoCommand(
                        LocoCommandAction.ThrowSwitch,
                        junction,
                        targetIsJunction: true,
                        requiredBranch: branch)
                    : new LocoCommand(LocoCommandAction.ThrowSwitch, from));
                travelReverse = !travelReverse;
                cmds.Add(new LocoCommand(LocoCommandAction.ChangeDirection, travelReverse: travelReverse));
            }

            var last = cmds.Count - 1;
            if (last >= 0
                && cmds[last].Action == LocoCommandAction.Drive
                && cmds[last].TravelReverse == travelReverse)
            {
                cmds[last] = new LocoCommand(LocoCommandAction.Drive, to, travelReverse);
            }
            else
            {
                cmds.Add(new LocoCommand(LocoCommandAction.Drive, to, travelReverse));
            }
        }

        return cmds;
    }

    private static Dictionary<string, PathEdge>? BuildHopLookup(
        IReadOnlyList<string> tracks,
        IReadOnlyList<PathEdge>? edges)
    {
        if (edges == null || edges.Count == 0)
        {
            return null;
        }

        var next = new Dictionary<string, List<string>>(tracks.Count, StringComparer.Ordinal);
        for (var i = 0; i < tracks.Count - 1; i++)
        {
            if (!next.TryGetValue(tracks[i], out var tos))
            {
                tos = new List<string>(1);
                next[tracks[i]] = tos;
            }

            tos.Add(tracks[i + 1]);
        }

        var map = new Dictionary<string, PathEdge>(tracks.Count, StringComparer.Ordinal);
        for (var i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
            if (!next.TryGetValue(e.FromTrackId, out var tos)
                || !tos.Contains(e.ToTrackId))
            {
                continue;
            }

            var key = HopKey(e.FromTrackId, e.ToTrackId);
            if (!map.ContainsKey(key))
            {
                map[key] = e;
            }
        }

        return map;
    }

    private static string HopKey(string from, string to) => from + "\0" + to;

    private static bool HopRequiresReverse(
        Dictionary<string, PathEdge>? hopLookup,
        string from,
        string to,
        PathPlanResult plan,
        bool isLastHop)
    {
        if (hopLookup != null
            && hopLookup.TryGetValue(HopKey(from, to), out var hop))
        {
            return hop.RequiresReverse;
        }

        // No edges: last hop only, from plan.LastHopRequiresReverse.
        return isLastHop && plan.LastHopRequiresReverse;
    }

    /// <summary>
    /// Live graph hops are junction stem ↔ branch and never carry RequiresReverse.
    /// Entering <paramref name="stem"/> from one branch and leaving on another branch of
    /// the same junction cannot be done rolling through the points — the train must
    /// pull past onto the stem and reverse.
    /// </summary>
    private static bool StemPivot(
        Dictionary<string, PathEdge>? hopLookup,
        string prev,
        string stem,
        string next)
    {
        if (hopLookup == null
            || !hopLookup.TryGetValue(HopKey(prev, stem), out var inbound)
            || !hopLookup.TryGetValue(HopKey(stem, next), out var outbound)
            || !inbound.HasJunction
            || !outbound.HasJunction)
        {
            return false;
        }

        return string.Equals(inbound.JunctionId, outbound.JunctionId, StringComparison.Ordinal)
            && inbound.RequiredBranch != outbound.RequiredBranch;
    }

    private static string? HopJunction(
        Dictionary<string, PathEdge>? hopLookup,
        string from,
        string to,
        out int requiredBranch)
    {
        requiredBranch = -1;
        if (hopLookup == null
            || !hopLookup.TryGetValue(HopKey(from, to), out var hop)
            || !hop.HasJunction)
        {
            return null;
        }

        requiredBranch = hop.RequiredBranch;
        return hop.JunctionId;
    }
}
