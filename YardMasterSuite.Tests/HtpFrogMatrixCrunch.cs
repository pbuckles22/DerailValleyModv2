using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Per-town then WORLD frog matrices on the harvested PathCheck graph.
/// Gemini 2026-09-09: batch by yard, prune islands, resume on done.
/// </summary>
internal static class HtpFrogMatrixCrunch
{
    public const string EnvName = "YMS_FROG_MATRIX_CRUNCH";

    public static string DropzoneDir()
    {
        var env = Environment.GetEnvironmentVariable("YMS_FROG_MATRIX_OUT");
        if (!string.IsNullOrWhiteSpace(env))
        {
            return env.Trim();
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "YardMasterSuite.sln")))
            {
                var dz = Path.Combine(dir.FullName, "docs", "gemini", "dropzone");
                Directory.CreateDirectory(dz);
                return dz;
            }

            dir = dir.Parent;
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "frog-matrix");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    public static List<string> Sorted(HashSet<string> set)
    {
        var list = new List<string>(set);
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    public static HashSet<string> NamedByYard(in RouteHarvestSnapshot snap)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < snap.Edges.Count; i++)
        {
            AddNamed(set, snap.Edges[i].FromTrackId);
            AddNamed(set, snap.Edges[i].ToTrackId);
        }

        return set;
    }

    public static SortedSet<string> Yards(HashSet<string> named)
    {
        var yards = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var id in named)
        {
            var y = PathRouteConstraints.YardIdOf(id);
            if (!string.IsNullOrEmpty(y))
            {
                yards.Add(y!);
            }
        }

        return yards;
    }

    public static void DumpJob(
        in RouteHarvestSnapshot snap,
        PathPlanGraph graph,
        string slug,
        IReadOnlyList<string> tracks,
        PathPlanMode mode,
        string? destYardId,
        HashSet<string> namedHighlight,
        string drop)
    {
        var progressPath = Path.Combine(drop, "matrix-" + slug + ".progress.txt");
        if (File.Exists(progressPath))
        {
            var prev = File.ReadAllText(progressPath);
            if (prev.StartsWith("done ", StringComparison.Ordinal))
            {
                return;
            }
        }

        if (tracks.Count < 2)
        {
            File.WriteAllText(progressPath, "done pairs=0 skipped=too-small" + Environment.NewLine);
            return;
        }

        var tsvPath = Path.Combine(drop, "matrix-" + slug + ".tsv");
        var geminiPath = Path.Combine(drop, "matrix-" + slug + "-gemini.txt");
        var sw = Stopwatch.StartNew();
        long pairs = 0;
        long planned = 0;
        long noPath = 0;
        long same = 0;
        long withLast = 0;
        long firstNeLast = 0;
        long noJunction = 0;
        var namedLines = new List<string>();
        var disagreeHead = new List<string>(8000);

        using (var tsv = new StreamWriter(tsvPath, false, new UTF8Encoding(false), 1 << 20))
        {
            tsv.WriteLine("origin\tdest\tstatus\tcost\thops\tfirst\tlast\tpick\tlatch\tfirst_ne_last");
            for (var i = 0; i < tracks.Count; i++)
            {
                var origin = tracks[i];
                for (var j = 0; j < tracks.Count; j++)
                {
                    if (i == j)
                    {
                        same++;
                        continue;
                    }

                    pairs++;
                    var dest = tracks[j];
                    var plan = PathPlan.Find(
                        graph,
                        snap.Selected,
                        origin,
                        dest,
                        destYardId: destYardId,
                        yardFor: PathRouteConstraints.YardIdOf,
                        mode: mode);

                    var status = plan.Status.ToString();
                    var cost = plan.TotalCost.ToString("0.###", CultureInfo.InvariantCulture);
                    var hops = plan.TrackIds == null ? 0 : plan.TrackIds.Count;
                    var first = plan.JunctionFirstStop?.JunctionId ?? "";
                    var last = RouteStepDestPolicy.PickLastJunctionId(plan) ?? "";
                    var pick = SwitchListRouteLeg.PickPinJunctionId(plan) ?? "";
                    var latch = last.Length > 0 ? last : pick;
                    var ne = first.Length > 0 && last.Length > 0
                        && !string.Equals(first, last, StringComparison.Ordinal)
                        ? "1"
                        : "0";

                    if (plan.Status == PathCheckStatus.NoPath
                        || plan.Status == PathCheckStatus.NoOrigin
                        || plan.Status == PathCheckStatus.NoDestination)
                    {
                        noPath++;
                    }
                    else
                    {
                        planned++;
                        if (last.Length == 0)
                        {
                            noJunction++;
                        }
                        else
                        {
                            withLast++;
                        }

                        if (ne == "1")
                        {
                            firstNeLast++;
                            if (disagreeHead.Count < 8000)
                            {
                                disagreeHead.Add(
                                    origin + "\t" + dest + "\t" + first + "\t" + last + "\t" + pick + "\t" + latch);
                            }
                        }
                    }

                    tsv.Write(origin);
                    tsv.Write('\t');
                    tsv.Write(dest);
                    tsv.Write('\t');
                    tsv.Write(status);
                    tsv.Write('\t');
                    tsv.Write(cost);
                    tsv.Write('\t');
                    tsv.Write(hops.ToString(CultureInfo.InvariantCulture));
                    tsv.Write('\t');
                    tsv.Write(first);
                    tsv.Write('\t');
                    tsv.Write(last);
                    tsv.Write('\t');
                    tsv.Write(pick);
                    tsv.Write('\t');
                    tsv.Write(latch);
                    tsv.Write('\t');
                    tsv.Write(ne);
                    tsv.Write('\n');

                    if (namedHighlight.Contains(origin) && namedHighlight.Contains(dest))
                    {
                        namedLines.Add(
                            origin + "\t" + dest + "\t" + status + "\t" + cost + "\t" + hops + "\t"
                            + first + "\t" + last + "\t" + pick + "\t" + latch + "\t" + ne);
                    }

                    if (pairs % 25000 == 0)
                    {
                        File.WriteAllText(
                            progressPath,
                            "slug=" + slug
                            + " pairs=" + pairs.ToString(CultureInfo.InvariantCulture)
                            + " planned=" + planned.ToString(CultureInfo.InvariantCulture)
                            + " nopath=" + noPath.ToString(CultureInfo.InvariantCulture)
                            + " first_ne_last=" + firstNeLast.ToString(CultureInfo.InvariantCulture)
                            + " elapsed_s=" + sw.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)
                            + Environment.NewLine);
                    }
                }
            }
        }

        sw.Stop();
        var tsvBytes = new FileInfo(tsvPath).Length;
        WriteGemini(
            geminiPath,
            slug,
            mode,
            destYardId,
            tracks.Count,
            pairs,
            planned,
            noPath,
            same,
            withLast,
            noJunction,
            firstNeLast,
            sw.Elapsed,
            tsvBytes,
            tsvPath,
            namedLines,
            disagreeHead);
        File.WriteAllText(
            progressPath,
            "done slug=" + slug
            + " pairs=" + pairs.ToString(CultureInfo.InvariantCulture)
            + " planned=" + planned.ToString(CultureInfo.InvariantCulture)
            + " nopath=" + noPath.ToString(CultureInfo.InvariantCulture)
            + " tracks=" + tracks.Count.ToString(CultureInfo.InvariantCulture)
            + " elapsed_s=" + sw.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)
            + " tsv_bytes=" + tsvBytes.ToString(CultureInfo.InvariantCulture)
            + Environment.NewLine);
    }

    private static void AddNamed(HashSet<string> set, string? trackId)
    {
        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (PathRouteConstraints.YardIdOf(id) != null)
        {
            set.Add(id);
        }
    }

    private static void WriteGemini(
        string path,
        string slug,
        PathPlanMode mode,
        string? destYardId,
        int tracks,
        long pairs,
        long planned,
        long noPath,
        long same,
        long withLast,
        long noJunction,
        long firstNeLast,
        TimeSpan elapsed,
        long tsvBytes,
        string tsvPath,
        IReadOnlyList<string> namedLines,
        IReadOnlyList<string> disagreeHead)
    {
        var sb = new StringBuilder(64 * 1024);
        sb.AppendLine("# Frog matrix — " + slug);
        sb.AppendLine();
        sb.AppendLine("mode=" + mode + " destYard=" + (destYardId ?? "null"));
        sb.AppendLine("Dump: " + tsvPath);
        sb.AppendLine("Bytes: " + tsvBytes.ToString("N0", CultureInfo.InvariantCulture));
        sb.AppendLine("Elapsed: " + elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
        sb.AppendLine("Latch: last junction if set, else PickPinJunctionId (cab 2.13.2.5.8 Past-switch).");
        sb.AppendLine();
        sb.AppendLine("## How to read");
        sb.AppendLine();
        sb.AppendLine("| Column | Meaning |");
        sb.AppendLine("|--------|---------|");
        sb.AppendLine("| origin / dest | Start and end track |");
        sb.AppendLine("| status | Aligned / Misaligned / NoPath |");
        sb.AppendLine("| cost | Dijkstra least-cost |");
        sb.AppendLine("| hops | Tracks on the path |");
        sb.AppendLine("| first | JunctionFirstStop |");
        sb.AppendLine("| last | Dest-side frog |");
        sb.AppendLine("| pick | Old first-stop/flip picker |");
        sb.AppendLine("| latch | Cab Past-switch pin |");
        sb.AppendLine("| first_ne_last | 1 if first and last differ |");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine("| tracks | " + tracks.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| pairs | " + pairs.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| same skipped | " + same.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| planned | " + planned.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| NoPath | " + noPath.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| with last | " + withLast.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| no junction | " + noJunction.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| first_ne_last | " + firstNeLast.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine();
        sb.AppendLine("## Named × named in this job");
        sb.AppendLine();
        sb.AppendLine("origin\tdest\tstatus\tcost\thops\tfirst\tlast\tpick\tlatch\tfirst_ne_last");
        for (var i = 0; i < namedLines.Count; i++)
        {
            sb.AppendLine(namedLines[i]);
        }

        sb.AppendLine();
        sb.AppendLine("## first_ne_last sample (cap 8000)");
        sb.AppendLine();
        sb.AppendLine("origin\tdest\tfirst\tlast\tpick\tlatch");
        for (var i = 0; i < disagreeHead.Count; i++)
        {
            sb.AppendLine(disagreeHead[i]);
        }

        File.WriteAllText(path, sb.ToString());
    }
}
