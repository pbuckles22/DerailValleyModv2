using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// One-shot raw local graph dump for HTP (9.1.3 Win 0). Tracks + junctions +
/// boards within 2.5 km. Not a Unity hop list. Win 1 wraps these rows as Core types.
/// </summary>
public static class TrackGraphHarvestCodec
{
    public const string Header = "YMS-GRAPH 1";

    public static string Format(
        string? origin,
        float locoX,
        float locoY,
        float locoZ,
        float forwardX,
        float forwardZ,
        HarvestedTrack[]? tracks,
        int trackCount,
        HarvestedJunction[]? junctions,
        int junctionCount,
        HarvestedGraphBoard[]? boards,
        int boardCount)
    {
        var trackN = ClampCount(tracks, trackCount);
        var juncN = ClampCount(junctions, junctionCount);
        var boardN = ClampCount(boards, boardCount);
        var sb = new StringBuilder(4096);
        sb.Append(Header).Append('\n');
        if (!string.IsNullOrEmpty(origin))
        {
            sb.Append("origin ").Append(origin).Append('\n');
        }

        sb.Append("loco ")
            .Append(F(locoX)).Append(' ')
            .Append(F(locoY)).Append(' ')
            .Append(F(locoZ)).Append(' ')
            .Append(F(forwardX)).Append(' ')
            .Append(F(forwardZ)).Append('\n');
        sb.Append("radiusM ").Append(((int)TrackGraphHarvestPolicy.RadiusMeters).ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("trackN ").Append(trackN.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("juncN ").Append(juncN.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("boardN ").Append(boardN.ToString(CultureInfo.InvariantCulture)).Append('\n');

        for (var i = 0; i < trackN; i++)
        {
            var t = tracks![i];
            sb.Append("track ")
                .Append(t.Id.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(F(t.InX)).Append(' ')
                .Append(F(t.InZ)).Append(' ')
                .Append(F(t.OutX)).Append(' ')
                .Append(F(t.OutZ)).Append(' ')
                .Append(F(t.LengthMeters)).Append('\n');
        }

        for (var i = 0; i < juncN; i++)
        {
            var j = junctions![i];
            sb.Append("junc ")
                .Append(j.Id.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(j.StemId.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(j.LeftId.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(j.RightId.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(j.SelectedBranch.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        for (var i = 0; i < boardN; i++)
        {
            var b = boards![i];
            sb.Append("board ")
                .Append(b.Id.ToString(CultureInfo.InvariantCulture)).Append(' ')
                .Append(F(b.X)).Append(' ')
                .Append(F(b.Z)).Append(' ')
                .Append(F(b.ThroughKmh)).Append(' ')
                .Append(F(b.DivergeKmh)).Append(' ')
                .Append(F(b.FacingX)).Append(' ')
                .Append(F(b.FacingZ)).Append(' ')
                .Append(b.IsDual ? '1' : '0').Append(' ')
                .Append(b.JunctionNearby ? '1' : '0').Append('\n');
        }

        return sb.ToString();
    }

    public static bool TryParse(string? text, out TrackGraphHarvestSnapshot snapshot)
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
        float locoX = 0f, locoY = 0f, locoZ = 0f, forwardX = 0f, forwardZ = 0f;
        var radiusM = TrackGraphHarvestPolicy.RadiusMeters;
        var trackN = 0;
        var juncN = 0;
        var boardN = 0;
        var tracks = new List<HarvestedTrack>(64);
        var junctions = new List<HarvestedJunction>(32);
        var boards = new List<HarvestedGraphBoard>(32);

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
                case "loco":
                    if (!TryLoco(rest, out locoX, out locoY, out locoZ, out forwardX, out forwardZ))
                    {
                        return false;
                    }

                    break;
                case "radiusM":
                    if (!float.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out radiusM))
                    {
                        return false;
                    }

                    break;
                case "trackN":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out trackN))
                    {
                        return false;
                    }

                    break;
                case "juncN":
                    if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out juncN))
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
                case "track":
                    if (!TryTrack(rest, out var track))
                    {
                        return false;
                    }

                    tracks.Add(track);
                    break;
                case "junc":
                    if (!TryJunc(rest, out var junc))
                    {
                        return false;
                    }

                    junctions.Add(junc);
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

        if (tracks.Count != trackN || junctions.Count != juncN || boards.Count != boardN)
        {
            return false;
        }

        snapshot = new TrackGraphHarvestSnapshot(
            origin,
            locoX,
            locoY,
            locoZ,
            forwardX,
            forwardZ,
            radiusM,
            trackN,
            juncN,
            boardN,
            tracks,
            junctions,
            boards);
        return true;
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

    private static bool TryLoco(
        string rest,
        out float x,
        out float y,
        out float z,
        out float fx,
        out float fz)
    {
        x = y = z = fx = fz = 0f;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return p.Length >= 5
            && float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
            && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
            && float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z)
            && float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out fx)
            && float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out fz);
    }

    private static bool TryTrack(string rest, out HarvestedTrack track)
    {
        track = default;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 6)
        {
            return false;
        }

        if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var inX)
            || !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var inZ)
            || !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var outX)
            || !float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var outZ)
            || !float.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var len))
        {
            return false;
        }

        track = new HarvestedTrack(id, inX, inZ, outX, outZ, len);
        return true;
    }

    private static bool TryJunc(string rest, out HarvestedJunction junc)
    {
        junc = default;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 5)
        {
            return false;
        }

        if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || !int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var stem)
            || !int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var left)
            || !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var right)
            || !int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var selected))
        {
            return false;
        }

        junc = new HarvestedJunction(id, stem, left, right, selected);
        return true;
    }

    private static bool TryBoard(string rest, out HarvestedGraphBoard board)
    {
        board = default;
        var p = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length < 9)
        {
            return false;
        }

        if (!int.TryParse(p[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            || !float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
            || !float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)
            || !float.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var through)
            || !float.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var diverge)
            || !float.TryParse(p[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var fx)
            || !float.TryParse(p[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var fz)
            || !int.TryParse(p[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dual)
            || !int.TryParse(p[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var junc))
        {
            return false;
        }

        board = new HarvestedGraphBoard(id, x, z, through, diverge, fx, fz, dual != 0, junc != 0);
        return true;
    }
}

public readonly struct HarvestedTrack
{
    public HarvestedTrack(int id, float inX, float inZ, float outX, float outZ, float lengthMeters)
    {
        Id = id;
        InX = inX;
        InZ = inZ;
        OutX = outX;
        OutZ = outZ;
        LengthMeters = lengthMeters;
    }

    public int Id { get; }
    public float InX { get; }
    public float InZ { get; }
    public float OutX { get; }
    public float OutZ { get; }
    public float LengthMeters { get; }
}

public readonly struct HarvestedJunction
{
    public HarvestedJunction(int id, int stemId, int leftId, int rightId, int selectedBranch)
    {
        Id = id;
        StemId = stemId;
        LeftId = leftId;
        RightId = rightId;
        SelectedBranch = selectedBranch;
    }

    public int Id { get; }
    public int StemId { get; }
    public int LeftId { get; }
    public int RightId { get; }
    public int SelectedBranch { get; }
}

public readonly struct HarvestedGraphBoard
{
    public HarvestedGraphBoard(
        int id,
        float x,
        float z,
        float throughKmh,
        float divergeKmh,
        float facingX,
        float facingZ,
        bool isDual,
        bool junctionNearby)
    {
        Id = id;
        X = x;
        Z = z;
        ThroughKmh = throughKmh;
        DivergeKmh = divergeKmh;
        FacingX = facingX;
        FacingZ = facingZ;
        IsDual = isDual;
        JunctionNearby = junctionNearby;
    }

    public int Id { get; }
    public float X { get; }
    public float Z { get; }
    public float ThroughKmh { get; }
    public float DivergeKmh { get; }
    public float FacingX { get; }
    public float FacingZ { get; }
    public bool IsDual { get; }
    public bool JunctionNearby { get; }
}

/// <summary>Parsed <see cref="TrackGraphHarvestCodec"/> snapshot for later Core walks.</summary>
public readonly struct TrackGraphHarvestSnapshot
{
    public TrackGraphHarvestSnapshot(
        string? origin,
        float locoX,
        float locoY,
        float locoZ,
        float forwardX,
        float forwardZ,
        float radiusMeters,
        int trackN,
        int juncN,
        int boardN,
        IReadOnlyList<HarvestedTrack> tracks,
        IReadOnlyList<HarvestedJunction> junctions,
        IReadOnlyList<HarvestedGraphBoard> boards)
    {
        Origin = origin;
        LocoX = locoX;
        LocoY = locoY;
        LocoZ = locoZ;
        ForwardX = forwardX;
        ForwardZ = forwardZ;
        RadiusMeters = radiusMeters;
        TrackN = trackN;
        JuncN = juncN;
        BoardN = boardN;
        Tracks = tracks;
        Junctions = junctions;
        Boards = boards;
    }

    public string? Origin { get; }
    public float LocoX { get; }
    public float LocoY { get; }
    public float LocoZ { get; }
    public float ForwardX { get; }
    public float ForwardZ { get; }
    public float RadiusMeters { get; }
    public int TrackN { get; }
    public int JuncN { get; }
    public int BoardN { get; }
    public IReadOnlyList<HarvestedTrack> Tracks { get; }
    public IReadOnlyList<HarvestedJunction> Junctions { get; }
    public IReadOnlyList<HarvestedGraphBoard> Boards { get; }
}
