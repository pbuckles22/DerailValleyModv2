using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// Round-trip text for headless corridor replay. Game dumps this once per
/// graph-ready / Set dest; tests parse it back into <see cref="PathEdge"/> + pose.
/// </summary>
public static class RouteHarvestCodec
{
    public const string Header = "YMS-HARVEST 1";

    public static string Format(
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int>? selected,
        IReadOnlyList<RouteHarvestJunction>? junctions = null,
        string? yardId = null,
        string? originTrackId = null,
        string? destTrackId = null,
        PathPlanMode mode = PathPlanMode.Yard,
        string? pinJunctionId = null,
        float? pinX = null,
        float? pinZ = null,
        float? noseX = null,
        float? noseZ = null,
        float? fwdX = null,
        float? fwdZ = null,
        float? consistLengthM = null,
        bool? pinIsBehind = null)
    {
        var sb = new StringBuilder(4096);
        sb.Append(Header).Append('\n');
        if (!string.IsNullOrEmpty(yardId))
        {
            sb.Append("yard ").Append(yardId).Append('\n');
        }

        if (!string.IsNullOrEmpty(originTrackId))
        {
            sb.Append("origin ").Append(originTrackId).Append('\n');
        }

        if (!string.IsNullOrEmpty(destTrackId))
        {
            sb.Append("dest ").Append(destTrackId).Append('\n');
        }

        sb.Append("mode ").Append(mode == PathPlanMode.Yard ? "Yard" : "World").Append('\n');
        if (!string.IsNullOrEmpty(pinJunctionId))
        {
            sb.Append("pin ").Append(pinJunctionId).Append('\n');
        }

        if (pinX.HasValue && pinZ.HasValue)
        {
            sb.Append("pinXZ ").Append(F(pinX.Value)).Append(' ').Append(F(pinZ.Value)).Append('\n');
        }

        if (noseX.HasValue && noseZ.HasValue)
        {
            sb.Append("noseXZ ").Append(F(noseX.Value)).Append(' ').Append(F(noseZ.Value)).Append('\n');
        }

        if (fwdX.HasValue && fwdZ.HasValue)
        {
            sb.Append("fwdXZ ").Append(F(fwdX.Value)).Append(' ').Append(F(fwdZ.Value)).Append('\n');
        }

        if (consistLengthM.HasValue)
        {
            sb.Append("length ").Append(F(consistLengthM.Value)).Append('\n');
        }

        if (pinIsBehind.HasValue)
        {
            sb.Append("pinBehind ").Append(pinIsBehind.Value ? "1" : "0").Append('\n');
        }

        if (selected != null)
        {
            foreach (var kv in selected)
            {
                sb.Append("sel ").Append(kv.Key).Append(' ').Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        if (junctions != null)
        {
            for (var i = 0; i < junctions.Count; i++)
            {
                var j = junctions[i];
                sb.Append("junc ").Append(j.Id).Append(' ').Append(F(j.X)).Append(' ').Append(F(j.Z)).Append(' ').Append(j.SelectedBranch.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        if (edges != null)
        {
            for (var i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                sb.Append("edge ")
                    .Append(e.FromTrackId).Append(' ')
                    .Append(e.ToTrackId).Append(' ')
                    .Append(e.JunctionId ?? "-").Append(' ')
                    .Append(e.RequiredBranch.ToString(CultureInfo.InvariantCulture)).Append(' ')
                    .Append(F(e.Cost)).Append(' ')
                    .Append(e.RequiresReverse ? "1" : "0")
                    .Append('\n');
            }
        }

        return sb.ToString();
    }

    public static bool TryParse(string? text, out RouteHarvestSnapshot harvest)
    {
        harvest = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var edges = new List<PathEdge>();
        var selected = new Dictionary<string, int>(StringComparer.Ordinal);
        var junctions = new List<RouteHarvestJunction>();
        string? yard = null;
        string? origin = null;
        string? dest = null;
        var mode = PathPlanMode.Yard;
        string? pin = null;
        float? pinX = null, pinZ = null, noseX = null, noseZ = null, fwdX = null, fwdZ = null, length = null;
        bool? pinBehind = null;
        var sawHeader = false;

        var lines = text!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (!sawHeader)
            {
                if (!line.StartsWith("YMS-HARVEST", StringComparison.Ordinal))
                {
                    return false;
                }

                sawHeader = true;
                continue;
            }

            var sp = line.IndexOf(' ');
            var key = sp < 0 ? line : line.Substring(0, sp);
            var rest = sp < 0 ? string.Empty : line.Substring(sp + 1).Trim();
            switch (key)
            {
                case "yard":
                    yard = rest;
                    break;
                case "origin":
                    origin = rest;
                    break;
                case "dest":
                    dest = rest;
                    break;
                case "mode":
                    mode = string.Equals(rest, "World", StringComparison.OrdinalIgnoreCase)
                        ? PathPlanMode.World
                        : PathPlanMode.Yard;
                    break;
                case "pin":
                    pin = rest;
                    break;
                case "pinXZ":
                    TryTwo(rest, out pinX, out pinZ);
                    break;
                case "noseXZ":
                    TryTwo(rest, out noseX, out noseZ);
                    break;
                case "fwdXZ":
                    TryTwo(rest, out fwdX, out fwdZ);
                    break;
                case "length":
                    if (float.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out var L))
                    {
                        length = L;
                    }

                    break;
                case "pinBehind":
                    pinBehind = rest == "1";
                    break;
                case "sel":
                    TrySel(rest, selected);
                    break;
                case "junc":
                    TryJunc(rest, junctions);
                    break;
                case "edge":
                    TryEdge(rest, edges);
                    break;
            }
        }

        if (!sawHeader)
        {
            return false;
        }

        harvest = new RouteHarvestSnapshot(
            edges,
            selected,
            junctions,
            yard,
            origin,
            dest,
            mode,
            pin,
            pinX,
            pinZ,
            noseX,
            noseZ,
            fwdX,
            fwdZ,
            length,
            pinBehind);
        return true;
    }

    private static string F(float v) => v.ToString("G9", CultureInfo.InvariantCulture);

    private static void TryTwo(string rest, out float? a, out float? b)
    {
        a = b = null;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 2)
        {
            return;
        }

        if (float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            a = x;
            b = z;
        }
    }

    private static void TrySel(string rest, Dictionary<string, int> selected)
    {
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 2)
        {
            return;
        }

        if (int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            selected[p[0]] = b;
        }
    }

    private static void TryJunc(string rest, List<RouteHarvestJunction> junctions)
    {
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 4)
        {
            return;
        }

        if (float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            && float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
            && int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var branch))
        {
            junctions.Add(new RouteHarvestJunction(p[0], x, z, branch));
        }
    }

    private static void TryEdge(string rest, List<PathEdge> edges)
    {
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 6)
        {
            return;
        }

        var junc = p[2] == "-" ? null : p[2];
        if (!int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var branch)
            || !float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var cost))
        {
            return;
        }

        var rev = p[5] == "1";
        edges.Add(new PathEdge(p[0], p[1], junc, branch, cost, rev));
    }

    /// <summary>
    /// Player.log → HTP ticks. Ingests <c>T2 speed change</c> /
    /// <c>T2 speed init</c> and <c>T2 controls: thr=</c> (UMM prefix OK).
    /// Does not require <see cref="Header"/>.
    /// </summary>
    public static bool TryParsePidLog(string? text, out HtpTickState[] ticks)
    {
        ticks = Array.Empty<HtpTickState>();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var list = new List<HtpTickState>(64);
        var speed = 0;
        var throttle = 0f;
        var independent = 0f;
        var lines = text!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (TryReadSpeedKmh(line, out var kmh))
            {
                speed = kmh;
                list.Add(new HtpTickState(speed, throttle, independent));
                continue;
            }

            if (TryReadControls(line, out var t, out var indy))
            {
                throttle = t;
                independent = indy;
                list.Add(new HtpTickState(speed, throttle, independent));
            }
        }

        if (list.Count == 0)
        {
            return false;
        }

        ticks = list.ToArray();
        return true;
    }

    private static bool TryReadSpeedKmh(string line, out int kmh)
    {
        kmh = 0;
        var i = line.IndexOf("T2 speed change: ", StringComparison.Ordinal);
        if (i < 0)
        {
            i = line.IndexOf("T2 speed init: ", StringComparison.Ordinal);
            if (i < 0)
            {
                return false;
            }

            i += "T2 speed init: ".Length;
        }
        else
        {
            i += "T2 speed change: ".Length;
        }

        var n = 0;
        var any = false;
        var neg = false;
        if (i < line.Length && line[i] == '-')
        {
            neg = true;
            i++;
        }

        while (i < line.Length && line[i] >= '0' && line[i] <= '9')
        {
            any = true;
            n = (n * 10) + (line[i] - '0');
            i++;
        }

        if (!any)
        {
            return false;
        }

        kmh = neg ? -n : n;
        return true;
    }

    private static bool TryReadControls(string line, out float throttle, out float independent)
    {
        throttle = 0f;
        independent = 0f;
        if (line.IndexOf("T2 controls:", StringComparison.Ordinal) < 0)
        {
            return false;
        }

        var raw = line.IndexOf("raw=", StringComparison.Ordinal);
        if (raw >= 0 && TryReadRawPair(line, raw + 4, out throttle, out independent))
        {
            return true;
        }

        if (!TryReadPctField(line, "thr=", out var thrPct)
            || !TryReadPctField(line, "indy=", out var indyPct))
        {
            return false;
        }

        throttle = thrPct / 100f;
        independent = indyPct / 100f;
        return true;
    }

    private static bool TryReadRawPair(string line, int start, out float throttle, out float independent)
    {
        throttle = 0f;
        independent = 0f;
        var comma = line.IndexOf(',', start);
        if (comma < 0)
        {
            return false;
        }

        if (!float.TryParse(
                line.Substring(start, comma - start),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out throttle))
        {
            return false;
        }

        var next = comma + 1;
        var comma2 = line.IndexOf(',', next);
        var end = comma2 < 0 ? line.Length : comma2;
        return float.TryParse(
            line.Substring(next, end - next),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out independent);
    }

    private static bool TryReadPctField(string line, string key, out int pct)
    {
        pct = 0;
        var i = line.IndexOf(key, StringComparison.Ordinal);
        if (i < 0)
        {
            return false;
        }

        i += key.Length;
        var n = 0;
        var any = false;
        while (i < line.Length && line[i] >= '0' && line[i] <= '9')
        {
            any = true;
            n = (n * 10) + (line[i] - '0');
            i++;
        }

        if (!any)
        {
            return false;
        }

        pct = n;
        return true;
    }
}

public readonly struct RouteHarvestJunction
{
    public RouteHarvestJunction(string id, float x, float z, int selectedBranch)
    {
        Id = id ?? string.Empty;
        X = x;
        Z = z;
        SelectedBranch = selectedBranch;
    }

    public string Id { get; }
    public float X { get; }
    public float Z { get; }
    public int SelectedBranch { get; }
}

public readonly struct RouteHarvestSnapshot
{
    public RouteHarvestSnapshot(
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> selected,
        IReadOnlyList<RouteHarvestJunction> junctions,
        string? yardId,
        string? originTrackId,
        string? destTrackId,
        PathPlanMode mode,
        string? pinJunctionId,
        float? pinX,
        float? pinZ,
        float? noseX,
        float? noseZ,
        float? fwdX,
        float? fwdZ,
        float? consistLengthM,
        bool? pinIsBehind)
    {
        Edges = edges;
        Selected = selected;
        Junctions = junctions;
        YardId = yardId;
        OriginTrackId = originTrackId;
        DestTrackId = destTrackId;
        Mode = mode;
        PinJunctionId = pinJunctionId;
        PinX = pinX;
        PinZ = pinZ;
        NoseX = noseX;
        NoseZ = noseZ;
        FwdX = fwdX;
        FwdZ = fwdZ;
        ConsistLengthM = consistLengthM;
        PinIsBehind = pinIsBehind;
    }

    public IReadOnlyList<PathEdge> Edges { get; }
    public IReadOnlyDictionary<string, int> Selected { get; }
    public IReadOnlyList<RouteHarvestJunction> Junctions { get; }
    public string? YardId { get; }
    public string? OriginTrackId { get; }
    public string? DestTrackId { get; }
    public PathPlanMode Mode { get; }
    public string? PinJunctionId { get; }
    public float? PinX { get; }
    public float? PinZ { get; }
    public float? NoseX { get; }
    public float? NoseZ { get; }
    public float? FwdX { get; }
    public float? FwdZ { get; }
    public float? ConsistLengthM { get; }
    public bool? PinIsBehind { get; }
}
