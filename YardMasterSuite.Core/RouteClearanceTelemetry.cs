namespace YardMasterSuite.Core;

public struct RouteClearanceTelemetryCache
{
    public bool Seeded;
    public RouteClearancePhase Phase;
    public int CaptionKey;
}

/// <summary>Change-only T2 for route pin / CLEARED (poll-cached companion).</summary>
public static class RouteClearanceTelemetry
{
    public static string? Observe(
        RouteClearancePhase phase,
        string? caption,
        ref RouteClearanceTelemetryCache cache)
    {
        var key = CaptionKey(caption);
        if (cache.Seeded && cache.Phase == phase && cache.CaptionKey == key)
        {
            return null;
        }

        cache.Seeded = true;
        cache.Phase = phase;
        cache.CaptionKey = key;

        if (phase == RouteClearancePhase.Idle)
        {
            return "T2 route-pin: idle";
        }

        if (phase == RouteClearancePhase.Cleared)
        {
            return "T2 route-pin: CLEARED";
        }

        return "T2 route-pin: At switch";
    }

    private static int CaptionKey(string? caption)
    {
        if (string.IsNullOrEmpty(caption))
        {
            return 0;
        }

        if (caption == "CLEARED")
        {
            return 2;
        }

        return 1;
    }
}
