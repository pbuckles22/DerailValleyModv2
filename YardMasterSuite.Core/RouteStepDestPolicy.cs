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

    public static bool ShouldRetargetMapsDest(RouteStepDestReason reason, RouteClearancePhase phase) =>
        ShouldRetargetMapsDest(reason, phase, stepKind: null);

    public static bool ShouldRetargetMapsDest(
        RouteStepDestReason reason,
        RouteClearancePhase phase,
        SwitchListStepKind? stepKind)
    {
        switch (reason)
        {
            case RouteStepDestReason.RouteBind:
                return false;
            case RouteStepDestReason.Align:
                return stepKind.HasValue
                    && !SwitchListRunner.StepNeedsPinClearance(stepKind.Value);
            case RouteStepDestReason.Next:
                return phase == RouteClearancePhase.Cleared
                    || RoutePinLatch.DisplayDismissed;
            default:
                return !stepKind.HasValue
                    || !SwitchListRunner.StepNeedsPinClearance(stepKind.Value);
        }
    }

    /// <summary>
    /// Past-switch Align dest is the later TurnAround / ReverseInto track so
    /// Set dest latches the sawtooth pin. Step label stays on the approach
    /// track (B4L). Recheck to B4L is Path OK / no pin.
    /// </summary>
    public static bool TryPinCorridorDest(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        out string? yardId,
        out string? trackId)
    {
        yardId = null;
        trackId = null;
        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            return false;
        }

        var current = steps[currentIndex];
        if (!SwitchListRunner.StepNeedsPinClearance(current.Kind))
        {
            return false;
        }

        for (var i = currentIndex + 1; i < steps.Count; i++)
        {
            var next = steps[i];
            if (next.Kind != SwitchListStepKind.TurnAround
                && next.Kind != SwitchListStepKind.ReverseInto)
            {
                continue;
            }

            var track = next.DestTrackId?.Trim();
            if (string.IsNullOrEmpty(track))
            {
                return false;
            }

            yardId = string.IsNullOrWhiteSpace(next.DestYardId) ? current.DestYardId : next.DestYardId;
            trackId = track;
            return true;
        }

        return false;
    }

    public static bool ShouldRetargetMapsDest(string? reason, RouteClearancePhase phase) =>
        ShouldRetargetMapsDest(Parse(reason), phase);

    public static bool ShouldRetargetMapsDest(
        string? reason,
        RouteClearancePhase phase,
        SwitchListStepKind? stepKind) =>
        ShouldRetargetMapsDest(Parse(reason), phase, stepKind);
}
