namespace YardMasterSuite.Core;

/// <summary>
/// Align after Next must not throw from the Set dest origin plan.
/// Smoke: B4L Path OK + already clear while the loco sat on S113 looking at a wrong branch.
/// </summary>
public static class RouteAlignOrigin
{
    public static bool NeedsRecompute(string? plannedOriginTrackId, string? liveOriginTrackId)
    {
        var planned = plannedOriginTrackId?.Trim();
        var live = liveOriginTrackId?.Trim();
        if (string.IsNullOrEmpty(planned) || string.IsNullOrEmpty(live))
        {
            return true;
        }

        return !string.Equals(planned, live, System.StringComparison.Ordinal);
    }
}
