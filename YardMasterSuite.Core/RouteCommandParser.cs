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
    /// Walk <paramref name="plan"/>.TrackIds. On a <see cref="PathEdge.RequiresReverse"/>
    /// hop, inject Stop → ThrowSwitch → ChangeDirection before the Drive.
    /// </summary>
    public static IReadOnlyList<LocoCommand> Generate(
        PathPlanResult? plan,
        IReadOnlyList<PathEdge>? edges = null)
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
        var hopLookup = BuildHopLookup(edges);
        var cmds = new List<LocoCommand>(tracks.Count * 2);
        var travelReverse = false;

        for (var i = 0; i < tracks.Count - 1; i++)
        {
            var from = tracks[i];
            var to = tracks[i + 1];
            var isLastHop = i == tracks.Count - 2;
            var requiresReverse = HopRequiresReverse(hopLookup, from, to, plan, isLastHop);

            if (requiresReverse)
            {
                cmds.Add(new LocoCommand(LocoCommandAction.Stop));
                var throwTarget = HopThrowTarget(hopLookup, from, to) ?? from;
                cmds.Add(new LocoCommand(LocoCommandAction.ThrowSwitch, throwTarget));
                travelReverse = true;
                cmds.Add(new LocoCommand(LocoCommandAction.ChangeDirection, travelReverse: true));
            }

            cmds.Add(new LocoCommand(LocoCommandAction.Drive, to, travelReverse));
        }

        return cmds;
    }

    private static Dictionary<string, PathEdge>? BuildHopLookup(IReadOnlyList<PathEdge>? edges)
    {
        if (edges == null || edges.Count == 0)
        {
            return null;
        }

        var map = new Dictionary<string, PathEdge>(edges.Count, StringComparer.Ordinal);
        for (var i = 0; i < edges.Count; i++)
        {
            var e = edges[i];
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

    private static string? HopThrowTarget(
        Dictionary<string, PathEdge>? hopLookup,
        string from,
        string to)
    {
        if (hopLookup == null
            || !hopLookup.TryGetValue(HopKey(from, to), out var hop))
        {
            return null;
        }

        return hop.HasJunction ? hop.JunctionId : null;
    }
}
