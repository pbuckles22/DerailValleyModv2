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
    public const string StillApproach = "T2 route-pin: still-approach ";

    public const string TailAlongPrefix = "T2 route-pin: tail-along past=";

    public static string FormatTailAlong(float pastMeters, string? hopId)
    {
        var hop = string.IsNullOrEmpty(hopId) ? "?" : hopId!.Trim();
        var past = float.IsNaN(pastMeters) || float.IsInfinity(pastMeters)
            ? "?"
            : ((int)(pastMeters + (pastMeters < 0f ? -0.5f : 0.5f))).ToString();
        return TailAlongPrefix + past + " hop=" + hop;
    }

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

        if (phase != RouteClearancePhase.AtSwitch)
        {
            return null;
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
