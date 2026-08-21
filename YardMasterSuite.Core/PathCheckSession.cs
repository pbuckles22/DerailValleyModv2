namespace YardMasterSuite.Core;

/// <summary>
/// Session-only path destination track id (**6.11** / v1 End dest).
/// Not persisted. Cleared on mod disable.
/// </summary>
public static class PathCheckSession
{
    private static string? _dest;

    public static bool HasDestination => _dest != null;

    public static string? DestinationTrackId => _dest;

    public static void SetDestination(string? trackId)
    {
        var t = trackId?.Trim();
        _dest = string.IsNullOrEmpty(t) ? null : t;
    }

    public static void Clear() => _dest = null;
}
