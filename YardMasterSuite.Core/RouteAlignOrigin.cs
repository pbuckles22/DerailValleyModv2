namespace YardMasterSuite.Core;

/// <summary>
/// Align after Next must not throw from the Set dest origin plan.
/// Smoke: B4L Path OK + already clear while the loco sat on S113 looking at a wrong branch.
/// Win 1: when a frozen plan exists, Align reads it — origin move does not call Find.
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

    /// <summary>
    /// True only when Align has no usable frozen phone. Origin move and pin dismiss
    /// do not rewrite the route.
    /// </summary>
    public static bool ShouldFindOnAlign(PathPlanResult? plan) =>
        ShouldFindOnAlign(
            plan,
            displayDismissed: false,
            plannedOriginTrackId: null,
            liveOriginTrackId: null);

    /// <summary>
    /// <paramref name="displayDismissed"/> and origin args stay for call-site clarity;
    /// they do not force a Find while <paramref name="plan"/> is Aligned or Misaligned.
    /// </summary>
    public static bool ShouldFindOnAlign(
        PathPlanResult? plan,
        bool displayDismissed,
        string? plannedOriginTrackId,
        string? liveOriginTrackId)
    {
        _ = displayDismissed;
        _ = plannedOriginTrackId;
        _ = liveOriginTrackId;
        if (plan == null)
        {
            return true;
        }

        return plan.Status == PathCheckStatus.NoPath
            || plan.Status == PathCheckStatus.NoOrigin;
    }
}
