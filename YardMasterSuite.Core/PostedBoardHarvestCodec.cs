using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// One-shot corridor boards dump for HTP (9.1.2 Win 2). Gather from SW start;
/// path segs + posted boards along the leave corridor (not yard-id filtered).
/// </summary>
public static class PostedBoardHarvestCodec
{
    public const string Header = "YMS-BOARDS 1";

    public static string Format(
        string? origin,
        float noseX,
        float noseZ,
        float fwdX,
        float fwdZ,
        PathSegmentAlong[]? segments,
        int segmentCount,
        ParsedPostedBoard[]? boards,
        int boardCount)
    {
        var pathN = ClampCount(segments, segmentCount);
        var boardN = ClampCount(boards, boardCount);
        var dualN = 0;
        var facingN = 0;
        for (var i = 0; i < boardN; i++)
        {
            var b = boards![i];
            if (b.IsDual)
            {
                dualN++;
            }

            if (FacesTravel(in b, fwdX, fwdZ))
            {
                facingN++;
            }
        }

        var sb = new StringBuilder(2048);
        sb.Append(Header).Append('\n');
        if (!string.IsNullOrEmpty(origin))
        {
            sb.Append("origin ").Append(origin).Append('\n');
        }

        sb.Append("noseXZ ").Append(F(noseX)).Append(' ').Append(F(noseZ)).Append('\n');
        sb.Append("fwdXZ ").Append(F(fwdX)).Append(' ').Append(F(fwdZ)).Append('\n');
        sb.Append("pathN ").Append(pathN.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("boardN ").Append(boardN.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("dualN ").Append(dualN.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("facingN ").Append(facingN.ToString(CultureInfo.InvariantCulture)).Append('\n');

        for (var i = 0; i < pathN; i++)
        {
            var s = segments![i];
            sb.Append("seg ")
                .Append(F(s.EntryDistanceMeters)).Append(' ')
                .Append(F(s.EntryX)).Append(' ')
                .Append(F(s.EntryY)).Append(' ')
                .Append(F(s.EntryZ)).Append(' ')
                .Append(F(s.HintX)).Append(' ')
                .Append(F(s.HintZ)).Append(' ')
                .Append(F(s.LengthMeters)).Append('\n');
        }

        for (var i = 0; i < boardN; i++)
        {
            var b = boards![i];
            sb.Append("board ")
                .Append(b.InstanceId.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(F(b.X)).Append(' ')
                .Append(F(b.Y)).Append(' ')
                .Append(F(b.Z)).Append(' ')
                .Append(F(b.ForwardX)).Append(' ')
                .Append(F(b.ForwardZ)).Append(' ')
                .Append(F(b.RightX)).Append(' ')
                .Append(F(b.RightZ)).Append(' ')
                .Append(F(b.ThroughKmh)).Append(' ')
                .Append(F(b.DivergeKmh)).Append(' ')
                .Append(b.IsDual ? '1' : '0').Append(' ')
                .Append(b.JunctionNearby ? '1' : '0').Append('\n');
        }

        return sb.ToString();
    }

    public static bool TryParse(string? text, out PostedBoardHarvestSnapshot snapshot)
    {
        snapshot = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var lines = text!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || !string.Equals(lines[0], Header, StringComparison.Ordinal))
        {
            return false;
        }

        string? origin = null;
        float noseX = 0f, noseZ = 0f, fwdX = 0f, fwdZ = 0f;
        var pathN = 0;
        var boardN = 0;
        var dualN = 0;
        var facingN = 0;
        var segs = new List<PathSegmentAlong>(16);
        var boards = new List<ParsedPostedBoard>(32);

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            var sp = line.IndexOf(' ');
            var key = sp < 0 ? line : line.Substring(0, sp);
            var rest = sp < 0 ? string.Empty : line.Substring(sp + 1);
            switch (key)
            {
                case "origin":
                    origin = rest;
                    break;
                case "noseXZ":
                    if (!TryTwo(rest, out noseX, out noseZ))
                    {
                        return false;
                    }

                    break;
                case "fwdXZ":
                    if (!TryTwo(rest, out fwdX, out fwdZ))
                    {
                        return false;
                    }

                    break;
                case "pathN":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out pathN))
                    {
                        return false;
                    }

                    break;
                case "boardN":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out boardN))
                    {
                        return false;
                    }

                    break;
                case "dualN":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out dualN))
                    {
                        return false;
                    }

                    break;
                case "facingN":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out facingN))
                    {
                        return false;
                    }

                    break;
                case "seg":
                    if (!TrySeg(rest, out var seg))
                    {
                        return false;
                    }

                    segs.Add(seg);
                    break;
                case "board":
                    if (!TryBoard(rest, out var board))
                    {
                        return false;
                    }

                    boards.Add(board);
                    break;
            }
        }

        if (segs.Count != pathN || boards.Count != boardN)
        {
            return false;
        }

        snapshot = new PostedBoardHarvestSnapshot(
            origin,
            noseX,
            noseZ,
            fwdX,
            fwdZ,
            pathN,
            boardN,
            dualN,
            facingN,
            segs,
            boards);
        return true;
    }

    public static bool FacesTravel(in ParsedPostedBoard board, float travelX, float travelZ)
    {
        var dot = (board.ForwardX * travelX) + (board.ForwardZ * travelZ);
        return dot <= -SpeedLimitBoardFacing.MinForwardAlign;
    }

    /// <summary>
    /// Strongly the back of the sign (travel · board-forward ≥ 0.5).
    /// Unknown / edge-on is not away — on-path Next may still govern.
    /// </summary>
    public static bool FacesAway(in ParsedPostedBoard board, float travelX, float travelZ)
    {
        var dot = (board.ForwardX * travelX) + (board.ForwardZ * travelZ);
        return dot >= SpeedLimitBoardFacing.MinForwardAlign;
    }

    private static int ClampCount<T>(T[]? items, int count)
    {
        if (items == null || count <= 0)
        {
            return 0;
        }

        return count > items.Length ? items.Length : count;
    }

    private static string F(float v) => v.ToString("G9", CultureInfo.InvariantCulture);

    private static bool TryTwo(string rest, out float a, out float b)
    {
        a = b = 0f;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return p.Length >= 2
            && float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out a)
            && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out b);
    }

    private static bool TrySeg(string rest, out PathSegmentAlong seg)
    {
        seg = default;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 7)
        {
            return false;
        }

        if (!float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var entryAbs)
            || !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var ex)
            || !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var ey)
            || !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var ez)
            || !float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var hx)
            || !float.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var hz)
            || !float.TryParse(p[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var len))
        {
            return false;
        }

        seg = new PathSegmentAlong(entryAbs, ex, ey, ez, hx, hz, len);
        return true;
    }

    private static bool TryBoard(string rest, out ParsedPostedBoard board)
    {
        board = default;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 12)
        {
            return false;
        }

        if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)
            || !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
            || !float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var fx)
            || !float.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var fz)
            || !float.TryParse(p[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var rx)
            || !float.TryParse(p[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var rz)
            || !float.TryParse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var through)
            || !float.TryParse(p[9], NumberStyles.Float, CultureInfo.InvariantCulture, out var diverge)
            || !int.TryParse(p[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dual)
            || !int.TryParse(p[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out var junc))
        {
            return false;
        }

        board = new ParsedPostedBoard(
            id, x, y, z, fx, fz, rx, rz, through, diverge, dual != 0, junc != 0);
        return true;
    }
}

/// <summary>Parsed <see cref="PostedBoardHarvestCodec"/> snapshot for HTP walks.</summary>
public readonly struct PostedBoardHarvestSnapshot
{
    public PostedBoardHarvestSnapshot(
        string? origin,
        float noseX,
        float noseZ,
        float fwdX,
        float fwdZ,
        int pathN,
        int boardN,
        int dualN,
        int facingN,
        IReadOnlyList<PathSegmentAlong> segments,
        IReadOnlyList<ParsedPostedBoard> boards)
    {
        Origin = origin;
        NoseX = noseX;
        NoseZ = noseZ;
        FwdX = fwdX;
        FwdZ = fwdZ;
        PathN = pathN;
        BoardN = boardN;
        DualN = dualN;
        FacingN = facingN;
        Segments = segments;
        Boards = boards;
    }

    public string? Origin { get; }
    public float NoseX { get; }
    public float NoseZ { get; }
    public float FwdX { get; }
    public float FwdZ { get; }
    public int PathN { get; }
    public int BoardN { get; }
    public int DualN { get; }
    public int FacingN { get; }
    public IReadOnlyList<PathSegmentAlong> Segments { get; }
    public IReadOnlyList<ParsedPostedBoard> Boards { get; }
}

/// <summary>One-shot gate: Maps leg + path + roster, write once per session.</summary>
public static class PostedBoardHarvestPolicy
{
    public static bool ShouldWrite(
        bool alreadyWritten,
        bool mapsLeg,
        int pathSegmentCount,
        int boardCount) =>
        !alreadyWritten && mapsLeg && pathSegmentCount > 0 && boardCount > 0;
}
