namespace YardMasterSuite.Core;

public struct RouteTelemetryCache
{
    public bool Seeded;
    public bool HasPlan;
    public PathCheckStatus Status;
    public int Misaligned;
    public float EtaSeconds;
}

public enum RouteTelemetryLogKind
{
    Init = 0,
    Change = 1,
    Cleared = 2,
    Align = 3,
}

/// <summary>Bucket T2 for Maps route HUD — not every frame.</summary>
public static class RouteTelemetry
{
    public static bool Observe(
        bool hasPlan,
        PathCheckStatus status,
        int misaligned,
        float etaSeconds,
        ref RouteTelemetryCache cache)
    {
        if (!hasPlan)
        {
            status = PathCheckStatus.NoDestination;
            misaligned = 0;
            etaSeconds = 0f;
        }

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.HasPlan = hasPlan;
            cache.Status = status;
            cache.Misaligned = misaligned;
            cache.EtaSeconds = etaSeconds;
            return hasPlan;
        }

        if (cache.HasPlan == hasPlan
            && cache.Status == status
            && cache.Misaligned == misaligned
            && cache.EtaSeconds == etaSeconds)
        {
            return false;
        }

        cache.HasPlan = hasPlan;
        cache.Status = status;
        cache.Misaligned = misaligned;
        cache.EtaSeconds = etaSeconds;
        return true;
    }

    public static RouteTelemetryLogKind ResolveLogKind(bool wasSeeded, bool wasPlan, bool hasPlan)
    {
        if (!hasPlan)
        {
            return RouteTelemetryLogKind.Cleared;
        }

        return !wasSeeded || !wasPlan ? RouteTelemetryLogKind.Init : RouteTelemetryLogKind.Change;
    }

    public static string? NextLog(
        RouteTelemetryLogKind kind,
        PathPlanResult? plan,
        float etaSeconds,
        string? facing = null)
    {
        if (kind == RouteTelemetryLogKind.Cleared)
        {
            return "T2 route cleared";
        }

        if (kind == RouteTelemetryLogKind.Align)
        {
            return null;
        }

        var chip = RoutePlanDisplay.FormatPathChip(plan) ?? "—";
        var eta = RouteEtaDisplay.Format(etaSeconds) ?? "ETA —";
        var line = kind == RouteTelemetryLogKind.Init
            ? "T2 route init: "
            : "T2 route change: ";
        line += chip + " | " + eta;
        if (!string.IsNullOrEmpty(facing))
        {
            line += " | " + facing;
        }

        return line;
    }

    public static string FormatAlign(bool applied, int thrown, string? detail = null)
    {
        var line = "T2 align: threw " + thrown;
        if (!applied)
        {
            line = "T2 align: abort";
        }

        return string.IsNullOrEmpty(detail) ? line : line + " " + detail;
    }
}
