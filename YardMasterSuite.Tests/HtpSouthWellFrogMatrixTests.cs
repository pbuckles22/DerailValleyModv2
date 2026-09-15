using System;
using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Harvested SW Dijkstra matrix: shortest path, Past-switch Observe pin
/// (ahead first-stop, else dest-side last). Cab 2.13.2.5.8 leave-TT.
/// </summary>
[Collection("StaticSessions")]
public sealed class HtpSouthWellFrogMatrixTests : IDisposable
{
    public const string Turntable = "#Y-#S1774#T";

    public HtpSouthWellFrogMatrixTests() => YmsRouteSessions.ClearAll();

    public void Dispose() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Fuzz_named_SW_tracks_shortest_path_latches_dest_side_frog()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        WalkMatrix(snap, NamedSwAndTurntable(snap), requireConnected: true, minPlanned: 8);
    }

    [Fact]
    public void Fuzz_cab_corridor_txt_named_SW_tracks_dest_side_frog()
    {
        var snap = HtpFixtures.LoadCorridor();
        WalkMatrix(snap, NamedSwAndTurntable(snap), requireConnected: true, minPlanned: 8);
    }

    [Fact]
    public void Fuzz_tracks_on_named_shortest_paths_latches_dest_side_frog()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var named = NamedSwAndTurntable(snap);
        var used = TracksOnNamedShortestPaths(snap, named);
        Assert.True(used.Count > named.Count, "shortest paths never left named rails");
        WalkMatrix(snap, used, requireConnected: false, minPlanned: used.Count);
    }

    [Fact]
    public void Fuzz_cab_corridor_txt_tracks_on_named_shortest_paths_dest_side_frog()
    {
        var snap = HtpFixtures.LoadCorridor();
        var named = NamedSwAndTurntable(snap);
        var used = TracksOnNamedShortestPaths(snap, named);
        Assert.True(used.Count > named.Count);
        WalkMatrix(snap, used, requireConnected: false, minPlanned: used.Count);
    }

    private static void WalkMatrix(
        in RouteHarvestSnapshot snap,
        IReadOnlyList<string> tracks,
        bool requireConnected,
        int minPlanned)
    {
        SwitchListSession.Bind(
            "SW-FROG-MATRIX",
            new[]
            {
                new SwitchListStep(
                    1,
                    SwitchListStepKind.Transit,
                    "SW",
                    tracks[0],
                    "Past",
                    bindNeedsReverse: true),
            });

        var planned = 0;
        var latched = 0;
        var noPath = new List<string>();
        var offPath = new List<string>();
        var offMap = new List<string>();
        var invented = new List<string>();

        for (var i = 0; i < tracks.Count; i++)
        {
            var origin = tracks[i];
            for (var j = 0; j < tracks.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var dest = tracks[j];
                var plan = PlanYard(snap, origin, dest);
                if (plan.Status == PathCheckStatus.NoPath
                    || plan.Status == PathCheckStatus.NoOrigin
                    || plan.Status == PathCheckStatus.NoDestination)
                {
                    noPath.Add(origin + " → " + dest);
                    continue;
                }

                planned++;
                Assert.Equal(origin, plan.TrackIds[0]);
                Assert.Equal(dest, plan.TrackIds[plan.TrackIds.Count - 1]);

                RoutePinLatch.Clear();
                RoutePinLatch.Observe("set-dest", plan, pinIsBehind: false);
                var latch = RoutePinLatch.Id;
                var pair = origin + " → " + dest;

                // A plan with no junctions has no frog to pin. Anything latched
                // there was invented from outside this corridor.
                if (plan.Junctions.Count == 0)
                {
                    if (!string.IsNullOrEmpty(latch))
                    {
                        invented.Add(pair + " latch=" + latch);
                    }

                    continue;
                }

                if (string.IsNullOrEmpty(latch))
                {
                    continue;
                }

                latched++;
                if (!PlanHasJunction(plan, latch!))
                {
                    offPath.Add(
                        pair + " latch=" + latch + " hops=" + JunctionIds(plan));
                }
                else if (!HtpFixtures.TryJunctionXz(in snap, latch, out _, out _))
                {
                    offMap.Add(pair + " latch=" + latch);
                }
            }
        }

        YmsRouteSessions.ClearAll();
        Assert.True(planned >= minPlanned, "Dijkstra planned " + planned + " pairs");
        if (requireConnected)
        {
            Assert.True(
                noPath.Count == 0,
                "NoPath " + noPath.Count + ": " + JoinHead(noPath, 12));
        }

        // Oracle is the plan itself, not the policy under test: every latched frog
        // must be a junction this path actually traverses and a real harvested node.
        Assert.True(
            offPath.Count == 0,
            "Latch off the planned path " + offPath.Count + ": " + JoinHead(offPath, 12));
        Assert.True(
            offMap.Count == 0,
            "Latch not in harvest " + offMap.Count + ": " + JoinHead(offMap, 12));
        Assert.True(
            invented.Count == 0,
            "Latch on a junctionless plan " + invented.Count + ": " + JoinHead(invented, 12));
        Assert.True(latched > 0, "no pair latched a frog — policy returned null everywhere");
    }

    private static bool PlanHasJunction(PathPlanResult plan, string junctionId)
    {
        for (var i = 0; i < plan.Junctions.Count; i++)
        {
            if (string.Equals(plan.Junctions[i].JunctionId, junctionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string JunctionIds(PathPlanResult plan)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < plan.Junctions.Count && i < 8; i++)
        {
            if (i > 0)
            {
                sb.Append('/');
            }

            sb.Append(plan.Junctions[i].JunctionId);
        }

        return sb.ToString();
    }

    private static PathPlanResult PlanYard(in RouteHarvestSnapshot snap, string origin, string dest)
    {
        var destYard = PathRouteConstraints.EffectiveDestYardId(
            dest,
            snap.YardId ?? "SW",
            PathRouteConstraints.YardIdOf);
        return PathPlan.Find(
            snap.Edges,
            snap.Selected,
            origin,
            dest,
            destYardId: destYard,
            yardFor: PathRouteConstraints.YardIdOf,
            mode: PathPlanMode.Yard);
    }

    private static IReadOnlyList<string> NamedSwAndTurntable(in RouteHarvestSnapshot snap)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < snap.Edges.Count; i++)
        {
            AddNamedSw(set, snap.Edges[i].FromTrackId);
            AddNamedSw(set, snap.Edges[i].ToTrackId);
        }

        set.Add(Turntable);
        return new List<string>(set);
    }

    private static IReadOnlyList<string> TracksOnNamedShortestPaths(
        in RouteHarvestSnapshot snap,
        IReadOnlyList<string> named)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < named.Count; i++)
        {
            set.Add(named[i]);
            for (var j = 0; j < named.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var plan = PlanYard(snap, named[i], named[j]);
                if (plan.Status == PathCheckStatus.NoPath
                    || plan.TrackIds == null
                    || plan.TrackIds.Count == 0)
                {
                    continue;
                }

                for (var t = 0; t < plan.TrackIds.Count; t++)
                {
                    var id = plan.TrackIds[t]?.Trim();
                    if (!string.IsNullOrEmpty(id))
                    {
                        set.Add(id);
                    }
                }
            }
        }

        return new List<string>(set);
    }

    private static void AddNamedSw(SortedSet<string> set, string? trackId)
    {
        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (string.Equals(PathRouteConstraints.YardIdOf(id), "SW", StringComparison.Ordinal))
        {
            set.Add(id);
        }
    }

    private static string JoinHead(IReadOnlyList<string> rows, int max)
    {
        var n = rows.Count < max ? rows.Count : max;
        var parts = new string[n];
        for (var i = 0; i < n; i++)
        {
            parts[i] = rows[i];
        }

        return string.Join("; ", parts);
    }
}
