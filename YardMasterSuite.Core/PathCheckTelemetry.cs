namespace YardMasterSuite.Core;

public struct PathCheckCache
{
    public bool Seeded;
    public bool HasDest;
    public PathCheckStatus Status;
    public int Misaligned;
}

public enum PathCheckLogKind
{
    Init = 0,
    Change = 1,
    Cleared = 2,
}

/// <summary>
/// Unity-free Path chip gate. HUD updates when dest / status / switch count
/// changes; T2 is init / change / cleared — not every LateUpdate tick.
/// </summary>
public static class PathCheckTelemetry
{
    public static bool Observe(
        bool hasDest,
        PathCheckStatus status,
        int misaligned,
        ref PathCheckCache cache)
    {
        if (!hasDest)
        {
            status = PathCheckStatus.NoDestination;
            misaligned = 0;
        }

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.HasDest = hasDest;
            cache.Status = status;
            cache.Misaligned = misaligned;
            return hasDest;
        }

        if (cache.HasDest == hasDest
            && cache.Status == status
            && cache.Misaligned == misaligned)
        {
            return false;
        }

        cache.HasDest = hasDest;
        cache.Status = status;
        cache.Misaligned = misaligned;
        return true;
    }

    public static string? NextLog(PathCheckLogKind kind, PathCheckStatus status, int misaligned)
    {
        if (kind == PathCheckLogKind.Cleared)
        {
            return "T2 path cleared";
        }

        var chip = Chip(status, misaligned);
        if (kind == PathCheckLogKind.Init)
        {
            return "T2 path init: " + chip;
        }

        return "T2 path change: " + chip;
    }

    public static PathCheckLogKind ResolveLogKind(bool wasSeeded, bool wasDest, bool hasDest)
    {
        if (!hasDest)
        {
            return PathCheckLogKind.Cleared;
        }

        return !wasSeeded || !wasDest ? PathCheckLogKind.Init : PathCheckLogKind.Change;
    }

    private static string Chip(PathCheckStatus status, int misaligned)
    {
        var formatted = PathCheckDisplay.Format(
            new PathCheckResult(status, System.Array.Empty<string>(), System.Array.Empty<PathJunctionEval>(), misaligned));
        return formatted ?? "—";
    }
}
