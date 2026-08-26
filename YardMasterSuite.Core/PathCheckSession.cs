namespace YardMasterSuite.Core;

/// <summary>
/// Session-only path destination track id (**6.11** / v1 End dest).
/// Not the Maps desk dest — that is <see cref="RouteDestSession"/> (**8.1**).
/// Sharing them made Set dest run PathCheck BFS on every `#Y` origin change.
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
