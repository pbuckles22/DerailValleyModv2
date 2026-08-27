namespace YardMasterSuite.Core;

/// <summary>Maps route HUD chips for Align Route (**8.2**).</summary>
public static class RoutePlanDisplay
{
    public static string? FormatPathChip(PathPlanResult? plan)
    {
        if (plan == null)
        {
            return null;
        }

        return PathCheckDisplay.Format(plan.ToCheckResult());
    }

    /// <summary>Desk / log line: Path + ETA (+ optional rem).</summary>
    public static string? FormatHudLine(PathPlanResult? plan, float? etaSeconds, float? remainingMeters)
    {
        var chip = FormatPathChip(plan);
        if (chip == null || etaSeconds is not float eta)
        {
            return chip;
        }

        return RouteEtaDisplay.HudPathChip(chip, eta)
            + (remainingMeters is float rem && rem >= 0f
                ? " | " + RouteEtaDisplay.FormatRemainingDistance(rem)
                : string.Empty);
    }
}
