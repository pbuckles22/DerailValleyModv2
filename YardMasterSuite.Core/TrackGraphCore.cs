namespace YardMasterSuite.Core;

/// <summary>
/// 9.1.3 Win 1 — dump rows as Core types for the later walker. No pathfinding here.
/// </summary>
public readonly struct CoreTrack
{
    public CoreTrack(int id, float inX, float inZ, float outX, float outZ, float lengthMeters)
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

public readonly struct CoreJunction
{
    public CoreJunction(int id, int stemId, int leftId, int rightId, int selectedBranch)
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

public static class TrackGraphCore
{
    public static CoreTrack Track(in HarvestedTrack row) =>
        new(row.Id, row.InX, row.InZ, row.OutX, row.OutZ, row.LengthMeters);

    public static CoreJunction Junction(in HarvestedJunction row) =>
        new(row.Id, row.StemId, row.LeftId, row.RightId, row.SelectedBranch);

    public static ParsedPostedBoard ToPostedBoard(in HarvestedGraphBoard board)
    {
        var rightX = board.FacingZ;
        var rightZ = -board.FacingX;
        return new ParsedPostedBoard(
            board.Id,
            board.X,
            y: 0f,
            board.Z,
            board.FacingX,
            board.FacingZ,
            rightX,
            rightZ,
            board.ThroughKmh,
            board.DivergeKmh,
            board.IsDual,
            board.JunctionNearby);
    }

    public static CoreTrack[] Tracks(in TrackGraphHarvestSnapshot snap)
    {
        var n = snap.Tracks.Count;
        var dest = new CoreTrack[n];
        for (var i = 0; i < n; i++)
        {
            dest[i] = Track(snap.Tracks[i]);
        }

        return dest;
    }

    public static CoreJunction[] Junctions(in TrackGraphHarvestSnapshot snap)
    {
        var n = snap.Junctions.Count;
        var dest = new CoreJunction[n];
        for (var i = 0; i < n; i++)
        {
            dest[i] = Junction(snap.Junctions[i]);
        }

        return dest;
    }

    /// <summary>9.1.3 Win 4 — dump boards as Evaluate roster. Allocates for HTP; Unity tick pooling is Win 5.</summary>
    public static ParsedPostedBoard[] Boards(in TrackGraphHarvestSnapshot snap)
    {
        var n = snap.Boards.Count;
        var dest = new ParsedPostedBoard[n];
        for (var i = 0; i < n; i++)
        {
            dest[i] = ToPostedBoard(snap.Boards[i]);
        }

        return dest;
    }

    public static int CopyBranches(
        CoreJunction[]? junctions,
        int count,
        JunctionBranchState[]? into)
    {
        if (junctions == null || into == null || count <= 0)
        {
            return 0;
        }

        var n = count;
        if (n > junctions.Length)
        {
            n = junctions.Length;
        }

        if (n > into.Length)
        {
            n = into.Length;
        }

        for (var i = 0; i < n; i++)
        {
            var j = junctions[i];
            into[i] = new JunctionBranchState(j.Id, j.SelectedBranch);
        }

        return n;
    }
}
