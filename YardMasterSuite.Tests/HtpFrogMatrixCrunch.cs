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
/// Mid-job timeout: count complete TSV rows and append (do not overwrite).
/// </summary>
internal static class HtpFrogMatrixCrunch
{
    public const string EnvName = "YMS_FROG_MATRIX_CRUNCH";

    /// <summary>
    /// Per-town crunch skips SW — covered by <c>YMS_FROG_MATRIX_FULL</c> 2027² dump.
    /// WORLD still includes SW rails.
    /// </summary>
    public static bool SkipPerTownYard(string? yardId) =>
        string.Equals(yardId?.Trim(), "SW", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Remaining hours (hundredths) from this session's work rate (pairs beyond skip).
    /// Resume must not use cumulative pairs / session elapsed — that understates ETA.
    /// </summary>
    public static string ProgressEtaSuffix(long pairs, long total, double elapsedS, long skipPairs = 0)
    {
        var suffix = " total=" + total.ToString(CultureInfo.InvariantCulture);
        if (skipPairs > 0)
        {
            suffix += " skip=" + skipPairs.ToString(CultureInfo.InvariantCulture);
        }

        var work = pairs - skipPairs;
        if (work <= 0 || elapsedS <= 0 || total < pairs)
        {
            return suffix;
        }

        var remainH = (total - pairs) * (elapsedS / work) / 3600.0;
        return suffix
            + " eta_h=" + remainH.ToString("0.00", CultureInfo.InvariantCulture);
    }

    internal const string TsvHeader =
        "origin\tdest\tstatus\tcost\thops\tfirst\tlast\tpick\tlatch\tfirst_ne_last";

    internal readonly struct FrogTsvResume
    {
        public FrogTsvResume(long skipPairs, bool append)
        {
            SkipPairs = skipPairs;
            Append = append;
        }

        public long SkipPairs { get; }

        public bool Append { get; }
    }

    /// <summary>
    /// Drop a torn last line, then count complete data rows. Unusable files are deleted
    /// so the caller starts a fresh TSV. Cursor is the TSV, not progress.txt.
    /// </summary>
    internal static FrogTsvResume PrepareTsvResume(string tsvPath)
    {
        if (!File.Exists(tsvPath) || new FileInfo(tsvPath).Length == 0)
        {
            return new FrogTsvResume(0, false);
        }

        TrimIncompleteLastLine(tsvPath);
        if (new FileInfo(tsvPath).Length == 0)
        {
            File.Delete(tsvPath);
            return new FrogTsvResume(0, false);
        }

        string? header;
        long rows = 0;
        using (var reader = new StreamReader(tsvPath, new UTF8Encoding(false), false))
        {
            header = reader.ReadLine();
            if (string.Equals(header, TsvHeader, StringComparison.Ordinal))
            {
                while (reader.ReadLine() != null)
                {
                    rows++;
                }
            }
        }

        if (!string.Equals(header, TsvHeader, StringComparison.Ordinal))
        {
            File.Delete(tsvPath);
            return new FrogTsvResume(0, false);
        }

        return new FrogTsvResume(rows, true);
    }

    internal static void TrimIncompleteLastLine(string tsvPath)
    {
        using var fs = new FileStream(tsvPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        if (fs.Length == 0)
        {
            return;
        }

        fs.Seek(-1, SeekOrigin.End);
        if (fs.ReadByte() == '\n')
        {
            return;
        }

        long keep = 0;
        for (var i = fs.Length - 2; i >= 0; i--)
        {
            fs.Seek(i, SeekOrigin.Begin);
            if (fs.ReadByte() == '\n')
            {
                keep = i + 1;
                break;
            }
        }

        fs.SetLength(keep);
    }

    internal static void ScanFrogTsv(
        string tsvPath,
        HashSet<string> namedHighlight,
        out long planned,
        out long noPath,
        out long withLast,
        out long noJunction,
        out long firstNeLast,
        List<string> namedLines,
        List<string> disagreeHead)
    {
        planned = 0;
        noPath = 0;
        withLast = 0;
        noJunction = 0;
        firstNeLast = 0;
        using var reader = new StreamReader(tsvPath, new UTF8Encoding(false), false);
        reader.ReadLine();
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            var c = line.Split('\t');
            if (c.Length < 10)
            {
                continue;
            }

            var status = c[2];
            if (status == "NoPath" || status == "NoOrigin" || status == "NoDestination")
            {
                noPath++;
            }
            else
            {
                planned++;
                if (c[6].Length == 0)
                {
                    noJunction++;
                }
                else
                {
                    withLast++;
                }

                if (c[9] == "1")
                {
                    firstNeLast++;
                    if (disagreeHead.Count < 8000)
                    {
                        disagreeHead.Add(c[0] + "\t" + c[1] + "\t" + c[5] + "\t" + c[6] + "\t" + c[7] + "\t" + c[8]);
                    }
                }
            }

            if (namedHighlight.Contains(c[0]) && namedHighlight.Contains(c[1]))
            {
                namedLines.Add(line);
            }
        }
    }

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
        var totalPairs = (long)tracks.Count * (tracks.Count - 1);
        var resume = PrepareTsvResume(tsvPath);
        long pairs = 0;
        long planned = 0;
        long noPath = 0;
        long same = 0;
        long withLast = 0;
        long firstNeLast = 0;
        long noJunction = 0;
        var namedLines = new List<string>();
        var disagreeHead = new List<string>(8000);
        if (resume.SkipPairs > 0)
        {
            ScanFrogTsv(
                tsvPath,
                namedHighlight,
                out planned,
                out noPath,
                out withLast,
                out noJunction,
                out firstNeLast,
                namedLines,
                disagreeHead);
        }

        var sw = Stopwatch.StartNew();

        using (var tsv = new StreamWriter(tsvPath, resume.Append, new UTF8Encoding(false), 1 << 20))
        {
            if (!resume.Append)
            {
                tsv.WriteLine(TsvHeader);
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
                        tsv.Flush();
                        var elapsedS = sw.Elapsed.TotalSeconds;
                        File.WriteAllText(
                            progressPath,
                            "slug=" + slug
                            + " pairs=" + pairs.ToString(CultureInfo.InvariantCulture)
                            + " planned=" + planned.ToString(CultureInfo.InvariantCulture)
                            + " nopath=" + noPath.ToString(CultureInfo.InvariantCulture)
                            + " first_ne_last=" + firstNeLast.ToString(CultureInfo.InvariantCulture)
                            + " elapsed_s=" + elapsedS.ToString("0.0", CultureInfo.InvariantCulture)
                            + ProgressEtaSuffix(pairs, totalPairs, elapsedS, resume.SkipPairs)
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
