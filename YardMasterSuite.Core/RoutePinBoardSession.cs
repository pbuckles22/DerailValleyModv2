namespace YardMasterSuite.Core;

/// <summary>Latched pin board from last list-load. Not per-frame.</summary>
public static class RoutePinBoardSession
{
    private static readonly RoutePinBoardEntry[] Entries = new RoutePinBoardEntry[RoutePinBoard.Capacity];
    private static readonly RoutePinBoardMarker[] Markers = new RoutePinBoardMarker[RoutePinBoard.Capacity];
    private static int _entryCount;
    private static int _markerCount;

    public static int EntryCount => _entryCount;

    public static int MarkerCount => _markerCount;

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

    public static string? PinIdForStep(int stepIndex) =>
        RoutePinBoard.PinIdForStep(Entries, _entryCount, stepIndex);

    public static int Rebuild(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? destYardId,
        string? originTrackId = null,
        SpatialGraph spatial = default)
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
            RoutePinBoard.Capacity,
            originTrackId,
            spatial);
        _markerCount = RoutePinBoard.Flatten(
            Entries,
            _entryCount,
            Markers,
            RoutePinBoard.Capacity);
        return _entryCount;
    }

    public static bool LaterStepOwnsPin(int currentStepIndex, string? pinId) =>
        RoutePinBoard.LaterStepOwnsPin(Entries, _entryCount, pinId, currentStepIndex);

    /// <summary>Drop spent step numbers from captions; keep later shared frogs.</summary>
    public static void DropSpentThrough(int completedStepIndex)
    {
        if (_entryCount <= 0)
        {
            return;
        }

        _markerCount = RoutePinBoard.FlattenRemaining(
            Entries,
            _entryCount,
            Markers,
            RoutePinBoard.Capacity,
            completedStepIndex);
    }

    public static void Clear()
    {
        _entryCount = 0;
        _markerCount = 0;
    }
}
