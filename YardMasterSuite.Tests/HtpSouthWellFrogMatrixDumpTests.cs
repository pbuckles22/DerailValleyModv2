using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Opt-in full SW track×track dump. Default <c>dotnet test</c> is a no-op.
/// Set <c>YMS_FROG_MATRIX_FULL=1</c>. Writes gitignored dropzone TSV + Gemini pack.
/// </summary>
[Collection("StaticSessions")]
public sealed class HtpSouthWellFrogMatrixDumpTests
{
    public const string EnvName = "YMS_FROG_MATRIX_FULL";

    [Fact]
    public async Task Dump_full_SW_track_matrix_opt_in()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnvName), "1", StringComparison.Ordinal))
        {
            await Task.CompletedTask;
            return;
        }

        var drop = DropzoneDir();
        Directory.CreateDirectory(drop);
        var progressPath = Path.Combine(drop, "sw-frog-matrix.progress.txt");
        if (File.Exists(progressPath))
        {
            var prev = File.ReadAllText(progressPath);
            if (prev.StartsWith("done ", StringComparison.Ordinal))
            {
                await Task.CompletedTask;
                return;
            }
        }

        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var tracks = AllTracks(snap);
        Assert.True(tracks.Count >= 8);

        var tsvPath = Path.Combine(drop, "sw-frog-matrix.tsv");
        var geminiPath = Path.Combine(drop, "sw-frog-matrix-gemini.txt");

        var graph = PathPlan.Compile(snap.Edges);
        var destYard = PathRouteConstraints.EffectiveDestYardId(
            "SW-C4S",
            snap.YardId ?? "SW",
            PathRouteConstraints.YardIdOf);

        var totalPairs = (long)tracks.Count * (tracks.Count - 1);
        var resume = HtpFrogMatrixCrunch.PrepareTsvResume(tsvPath);
        long pairs = 0;
        long planned = 0;
        long noPath = 0;
        long same = 0;
        long withLast = 0;
        long firstNeLast = 0;
        long noJunction = 0;
        long bindLie = 0;
        var named = NamedSw(snap);
        var namedLines = new List<string>();
        var disagreeHead = new List<string>(8000);
        if (resume.SkipPairs > 0)
        {
            HtpFrogMatrixCrunch.ScanFrogTsv(
                tsvPath,
                named,
                out planned,
                out noPath,
                out withLast,
                out noJunction,
                out firstNeLast,
                namedLines,
                disagreeHead,
                out bindLie);
        }

        var sw = Stopwatch.StartNew();

        using (var tsv = new StreamWriter(tsvPath, resume.Append, new UTF8Encoding(false), 1 << 20))
        {
            if (!resume.Append)
            {
                tsv.WriteLine(HtpFrogMatrixCrunch.TsvHeader);
            }

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
                    if (pairs <= resume.SkipPairs)
                    {
                        continue;
                    }

                    var dest = tracks[j];
                    var plan = PathPlan.Find(
                        graph,
                        snap.Selected,
                        origin,
                        dest,
                        destYardId: destYard,
                        yardFor: PathRouteConstraints.YardIdOf,
                        mode: PathPlanMode.Yard);

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
                    var isNoPath = plan.Status == PathCheckStatus.NoPath
                        || plan.Status == PathCheckStatus.NoOrigin
                        || plan.Status == PathCheckStatus.NoDestination;
                    var eng = HtpFrogMatrixCrunch.EngineerTsvTail(plan, isNoPath, out var rowBindLie);
                    if (rowBindLie)
                    {
                        bindLie++;
                    }

                    if (isNoPath)
                    {
                        noPath++;
                        status = plan.Status.ToString();
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
                    tsv.Write('\t');
                    tsv.Write(eng);
                    tsv.Write('\n');

                    if (named.Contains(origin) && named.Contains(dest))
                    {
                        namedLines.Add(
                            origin + "\t" + dest + "\t" + status + "\t" + cost + "\t" + hops + "\t"
                            + first + "\t" + last + "\t" + pick + "\t" + latch + "\t" + ne + "\t" + eng);
                    }

                    if (pairs % 25000 == 0)
                    {
                        tsv.Flush();
                        var elapsedS = sw.Elapsed.TotalSeconds;
                        File.WriteAllText(
                            progressPath,
                            "pairs=" + pairs.ToString(CultureInfo.InvariantCulture)
                            + " planned=" + planned.ToString(CultureInfo.InvariantCulture)
                            + " nopath=" + noPath.ToString(CultureInfo.InvariantCulture)
                            + " first_ne_last=" + firstNeLast.ToString(CultureInfo.InvariantCulture)
                            + " elapsed_s=" + elapsedS.ToString("0.0", CultureInfo.InvariantCulture)
                            + HtpFrogMatrixCrunch.ProgressEtaSuffix(pairs, totalPairs, elapsedS, resume.SkipPairs)
                            + Environment.NewLine);
                    }
                }
            }
        }

        sw.Stop();
        var tsvBytes = new FileInfo(tsvPath).Length;
        WriteGemini(
            geminiPath,
            tracks.Count,
            pairs,
            planned,
            noPath,
            same,
            withLast,
            noJunction,
            firstNeLast,
            bindLie,
            sw.Elapsed,
            tsvBytes,
            tsvPath,
            named,
            namedLines,
            disagreeHead);

        File.WriteAllText(
            progressPath,
            "done pairs=" + pairs.ToString(CultureInfo.InvariantCulture)
            + " elapsed_s=" + sw.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture)
            + " tsv_bytes=" + tsvBytes.ToString(CultureInfo.InvariantCulture)
            + Environment.NewLine);

        Assert.True(pairs == (long)tracks.Count * (tracks.Count - 1));
        Assert.True(File.Exists(geminiPath));
        Assert.True(tsvBytes > 1000);
    }

    private static void WriteGemini(
        string path,
        int tracks,
        long pairs,
        long planned,
        long noPath,
        long same,
        long withLast,
        long noJunction,
        long firstNeLast,
        long bindLie,
        TimeSpan elapsed,
        long tsvBytes,
        string tsvPath,
        ISet<string> named,
        IReadOnlyList<string> namedLines,
        IReadOnlyList<string> disagreeHead)
    {
        var sb = new StringBuilder(64 * 1024);
        sb.AppendLine("# SW frog matrix — Gemini pack");
        sb.AppendLine();
        sb.AppendLine("Full TSV is too large for chat. Read THIS file. Open the TSV only if you need a specific pair.");
        sb.AppendLine("Dump: " + tsvPath);
        sb.AppendLine("Bytes: " + tsvBytes.ToString("N0", CultureInfo.InvariantCulture));
        sb.AppendLine("Elapsed: " + elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");
        sb.AppendLine("Graph: corridor-sw-sl-55-2026-09-04.txt (2027 unique rails; same as corridor.txt).");
        sb.AppendLine("Planner: PathPlan.Find Yard Dijkstra (least cost), adjacency compiled once.");
        sb.AppendLine("Latch rule (cab 2.13.2.5.8 Past-switch Observe): last junction if present, else PickPinJunctionId.");
        sb.AppendLine();
        sb.AppendLine("## How to read (column table)");
        sb.AppendLine();
        sb.AppendLine("| Column | File | Meaning |");
        sb.AppendLine("|--------|------|---------|");
        sb.AppendLine("| origin | TSV | Start track id |");
        sb.AppendLine("| dest | TSV | End track id |");
        sb.AppendLine("| status | TSV | Aligned / Misaligned / NoPath / NoOrigin / NoDestination |");
        sb.AppendLine("| cost | TSV | Dijkstra totalCost (least time/cost) |");
        sb.AppendLine("| hops | TSV | Track count on the path (1 = same rail, not dumped as a pair) |");
        sb.AppendLine("| first | TSV | JunctionFirstStop (sawtooth / branch-reuse). Empty if none |");
        sb.AppendLine("| last | TSV | Last junction on the path = dest-side frog. Empty if no junctions |");
        sb.AppendLine("| pick | TSV | SwitchListRouteLeg.PickPinJunctionId (first-stop or first flip) |");
        sb.AppendLine("| latch | TSV | What cab Observe latches on Past-switch: last if set, else pick |");
        sb.AppendLine("| first_ne_last | TSV | 1 if first and last both set and differ (C4S 989976 vs dest-side) |");
        sb.AppendLine("| first_rev | TSV | 1 if first hop RequiresReverse (engineer leave-origin) |");
        sb.AppendLine("| last_rev | TSV | 1 if last hop RequiresReverse |");
        sb.AppendLine("| rev_n | TSV | Count of reverse hops (sawtooth complexity) |");
        sb.AppendLine("| bind_lie | TSV | 1 if bind Forward would disagree with first_rev (2.13.2.5.18 class) |");
        sb.AppendLine();
        sb.AppendLine("Same-track pairs are omitted (count in Summary.same). TSV is tab-separated, UTF-8, no BOM, one header row.");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine("| tracks | " + tracks.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| pairs (N×N minus same) | " + pairs.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| same-track skipped | " + same.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| planned (has path) | " + planned.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| NoPath/NoOrigin/NoDest | " + noPath.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| path with dest-side last | " + withLast.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| path with no junction | " + noJunction.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| first_ne_last=1 | " + firstNeLast.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| bind_lie=1 | " + bindLie.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| named SW tracks | " + named.Count.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine("| disagree rows below (capped 8000) | " + disagreeHead.Count.ToString(CultureInfo.InvariantCulture) + " |");
        sb.AppendLine();
        sb.AppendLine("## Named SW × named SW (complete)");
        sb.AppendLine();
        sb.AppendLine("origin\tdest\tstatus\tcost\thops\tfirst\tlast\tpick\tlatch\tfirst_ne_last\tfirst_rev\tlast_rev\trev_n\tbind_lie");
        for (var i = 0; i < namedLines.Count; i++)
        {
            sb.AppendLine(namedLines[i]);
        }

        sb.AppendLine();
        sb.AppendLine("## first_ne_last sample (first 8000)");
        sb.AppendLine();
        sb.AppendLine("origin\tdest\tfirst\tlast\tpick\tlatch");
        for (var i = 0; i < disagreeHead.Count; i++)
        {
            sb.AppendLine(disagreeHead[i]);
        }

        if (firstNeLast > disagreeHead.Count)
        {
            sb.AppendLine("# truncated; remaining first_ne_last="
                + (firstNeLast - disagreeHead.Count).ToString(CultureInfo.InvariantCulture)
                + " are only in the full TSV (filter first_ne_last=1).");
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static string DropzoneDir()
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

    private static List<string> AllTracks(in RouteHarvestSnapshot snap)
    {
        var set = new SortedSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < snap.Edges.Count; i++)
        {
            var from = snap.Edges[i].FromTrackId?.Trim();
            var to = snap.Edges[i].ToTrackId?.Trim();
            if (!string.IsNullOrEmpty(from))
            {
                set.Add(from);
            }

            if (!string.IsNullOrEmpty(to))
            {
                set.Add(to);
            }
        }

        return new List<string>(set);
    }

    private static HashSet<string> NamedSw(in RouteHarvestSnapshot snap)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < snap.Edges.Count; i++)
        {
            AddNamed(set, snap.Edges[i].FromTrackId);
            AddNamed(set, snap.Edges[i].ToTrackId);
        }

        set.Add("#Y-#S1774#T");
        return set;
    }

    private static void AddNamed(HashSet<string> set, string? trackId)
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
}
