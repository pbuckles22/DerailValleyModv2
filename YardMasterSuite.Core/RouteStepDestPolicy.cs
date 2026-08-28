namespace YardMasterSuite.Core;

/// <summary>Why Maps dest would be rewritten from a Switch List step.</summary>
public enum RouteStepDestReason : byte
{
    JobListLoad = 0,
    RouteBind = 1,
    Align = 2,
    Next = 3,
}

/// <summary>
/// Past-switch Switch List must not Recheck Maps dest until CLEARED + Next.
/// Smoke: route-bind Recheck to #Y-#S989#T stole the Turntable sawtooth pin.
/// </summary>
public static class RouteStepDestPolicy
{
    public static RouteStepDestReason Parse(string? reason)
    {
        if (reason == "route-bind")
        {
            return RouteStepDestReason.RouteBind;
        }

        if (reason == "list-align")
        {
            return RouteStepDestReason.Align;
        }

        if (reason == "list-next")
        {
            return RouteStepDestReason.Next;
        }

        return RouteStepDestReason.JobListLoad;
    }

    public static bool ShouldRetargetMapsDest(RouteStepDestReason reason, RouteClearancePhase phase)
    {
        switch (reason)
        {
            case RouteStepDestReason.RouteBind:
            case RouteStepDestReason.Align:
                return false;
            case RouteStepDestReason.Next:
                return phase == RouteClearancePhase.Cleared;
            default:
                return true;
        }
    }

    public static bool ShouldRetargetMapsDest(string? reason, RouteClearancePhase phase) =>
        ShouldRetargetMapsDest(Parse(reason), phase);
}
