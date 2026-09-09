using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Always-on blob prune + opt-in hours crunch (per town then world).
/// Set <c>YMS_FROG_MATRIX_CRUNCH=1</c>. Resume skips <c>done </c> progress files.
/// </summary>
[Collection("StaticSessions")]
public sealed class HtpFrogMatrixCrunchTests
{
    [Fact]
    public void Smoke_SW_blob_keeps_job_spurs_drops_CS_and_island()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var graph = PathPlan.Compile(snap.Edges);
        var blob = PathPlan.YardConnectedBlob(
            graph,
            snap.Selected,
            "SW",
            PathRouteConstraints.YardIdOf);
        Assert.Contains("SW-B1S", blob);
        Assert.Contains("SW-C4S", blob);
        Assert.Contains("SW-B4L", blob);
        Assert.DoesNotContain("CS-M14P", blob);
        Assert.DoesNotContain("#Y-#S1779#T", blob);
        Assert.True(blob.Count < 400, "SW blob leaked the mainline (" + blob.Count + ")");
    }

    [Fact]
    public void Smoke_per_town_crunch_skips_SW_keeps_HB_and_MB()
    {
        Assert.True(HtpFrogMatrixCrunch.SkipPerTownYard("SW"));
        Assert.True(HtpFrogMatrixCrunch.SkipPerTownYard("sw"));
        Assert.False(HtpFrogMatrixCrunch.SkipPerTownYard("HB"));
        Assert.False(HtpFrogMatrixCrunch.SkipPerTownYard("MB"));
    }

    [Fact]
    public void Smoke_WORLD_prune_keeps_named_spurs()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var graph = PathPlan.Compile(snap.Edges);
        var named = HtpFrogMatrixCrunch.NamedByYard(in snap);
        var keep = PathPlan.TracksThatCanReach(graph, named);
        var all = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < snap.Edges.Count; i++)
        {
            var e = snap.Edges[i];
            if (!string.IsNullOrEmpty(e.FromTrackId))
            {
                all.Add(e.FromTrackId.Trim());
            }

            if (!string.IsNullOrEmpty(e.ToTrackId))
            {
                all.Add(e.ToTrackId.Trim());
            }
        }

        Assert.Contains("SW-B1S", keep);
        Assert.Contains("CS-M14P", named);
        Assert.True(keep.Count >= named.Count);
        Assert.True(keep.Count <= all.Count);
        // #Y-#S1779#T reverse-reaches some named spur, so WORLD prune keeps it.
        // The 4.1M SW dump NoPath flood was PathPlanMode.Yard + destYard=SW, not a graph island.
    }

    [Fact(Timeout = 43200000)]
    public async Task Dump_per_town_then_world_opt_in()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(HtpFrogMatrixCrunch.EnvName),
                "1",
                StringComparison.Ordinal))
        {
            await Task.CompletedTask;
            return;
        }

        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var graph = PathPlan.Compile(snap.Edges);
        var named = HtpFrogMatrixCrunch.NamedByYard(in snap);
        var yards = HtpFrogMatrixCrunch.Yards(named);
        var drop = HtpFrogMatrixCrunch.DropzoneDir();
        Directory.CreateDirectory(drop);

        var index = new StringBuilder();
        index.AppendLine("# Global frog crunch index");
        index.AppendLine();
        index.AppendLine("Give Gemini this file plus each matrix-*-gemini.txt. Not the TSVs.");
        index.AppendLine("Graph: corridor-sw-sl-55-2026-09-04 (PathCheck edges already include every city prefix).");
        index.AppendLine("Per town: every harvest yard except SW (SW 2027² is YMS_FROG_MATRIX_FULL).");
        index.AppendLine("World: TracksThatCanReach any named spur, PathPlanMode.World (includes SW rails).");
        index.AppendLine();
        index.AppendLine("| slug | tracks | notes |");
        index.AppendLine("|------|--------|-------|");

        foreach (var yard in yards)
        {
            if (HtpFrogMatrixCrunch.SkipPerTownYard(yard))
            {
                index.AppendLine("| sw | skipped | SW 2027² is YMS_FROG_MATRIX_FULL, not this loop |");
                File.WriteAllText(Path.Combine(drop, "matrix-index-gemini.txt"), index.ToString());
                continue;
            }

            var blob = PathPlan.YardConnectedBlob(
                graph,
                snap.Selected,
                yard,
                PathRouteConstraints.YardIdOf);
            var tracks = HtpFrogMatrixCrunch.Sorted(blob);
            var highlight = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in named)
            {
                if (string.Equals(PathRouteConstraints.YardIdOf(id), yard, StringComparison.Ordinal))
                {
                    highlight.Add(id);
                }
            }

            var slug = yard.ToLowerInvariant();
            index.AppendLine("| " + slug + " | " + tracks.Count.ToString() + " | Yard mode destYard=" + yard + " |");
            File.WriteAllText(Path.Combine(drop, "matrix-index-gemini.txt"), index.ToString());
            HtpFrogMatrixCrunch.DumpJob(
                in snap,
                graph,
                slug,
                tracks,
                PathPlanMode.Yard,
                yard,
                highlight,
                drop);
        }

        var worldKeep = PathPlan.TracksThatCanReach(graph, named);
        var worldTracks = HtpFrogMatrixCrunch.Sorted(worldKeep);
        index.AppendLine("| world | " + worldTracks.Count.ToString() + " | World mode, pruned islands |");
        File.WriteAllText(Path.Combine(drop, "matrix-index-gemini.txt"), index.ToString());
        HtpFrogMatrixCrunch.DumpJob(
            in snap,
            graph,
            "world",
            worldTracks,
            PathPlanMode.World,
            destYardId: null,
            named,
            drop);

        index.AppendLine();
        index.AppendLine("Done. Progress files: matrix-*.progress.txt (must start with done).");
        File.WriteAllText(Path.Combine(drop, "matrix-index-gemini.txt"), index.ToString());
        Assert.True(File.Exists(Path.Combine(drop, "matrix-index-gemini.txt")));
        await Task.CompletedTask;
    }
}
