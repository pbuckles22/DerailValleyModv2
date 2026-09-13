namespace YardMasterSuite.Core;

/// <summary>Latched pin board from last list-load / Set dest. Not per-frame.</summary>
public static class RoutePinBoardSession
{
    private static readonly RoutePinBoardEntry[] Entries = new RoutePinBoardEntry[RoutePinBoard.Capacity];
    private static readonly RoutePinBoardMarker[] Markers = new RoutePinBoardMarker[RoutePinBoard.Capacity];
    private static int _entryCount;
    private static int _markerCount;

    public static int EntryCount => _entryCount;

    public static int MarkerCount => _markerCount;

    public static int SplitCount => RoutePinBoard.CountSplits(Entries, _entryCount);

    public static bool HasBoard => _entryCount > 0;

    public static bool TryGetEntry(int index, out RoutePinBoardEntry entry)
    {
        if (index < 0 || index >= _entryCount)
        {
            entry = default;
            return false;
        }

        entry = Entries[index];
        return true;
    }

    public static bool TryGetMarker(int index, out RoutePinBoardMarker marker)
    {
        if (index < 0 || index >= _markerCount)
        {
            marker = default;
            return false;
        }

        marker = Markers[index];
        return true;
    }

    public static string? CaptionForPin(string? pinId) =>
        RoutePinBoard.CaptionForPin(Markers, _markerCount, pinId);

    public static int Rebuild(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? destYardId)
    {
        Clear();
        if (!SwitchListSession.HasActive || edges == null || selected == null)
        {
            return 0;
        }

        _entryCount = RoutePinBoard.Collect(
            SwitchListSession.Steps,
            edges,
            selected,
            destYardId,
            Entries,
            RoutePinBoard.Capacity);
        _markerCount = RoutePinBoard.Flatten(
            Entries,
            _entryCount,
            Markers,
            RoutePinBoard.Capacity);
        return _entryCount;
    }

    public static void Clear()
    {
        _entryCount = 0;
        _markerCount = 0;
    }
}
