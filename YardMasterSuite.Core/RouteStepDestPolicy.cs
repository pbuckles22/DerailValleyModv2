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
    /// Past-switch Align dest is the later TurnAround / ReverseInto / Prep
    /// track so Set dest latches the sawtooth pin. Step label stays on the
    /// approach track (B4L / TT). Recheck to that label is Path OK / no pin.
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
                && next.Kind != SwitchListStepKind.ReverseInto
                && next.Kind != SwitchListStepKind.Prep)
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

    /// <summary>
    /// Landing on a past-switch row must Set dest to the later pin-corridor
    /// track (TT / Prep). Recheck to the approach label is the B4L / C1O steal.
    /// </summary>
    public static bool ShouldSetPinCorridorDest(RouteStepDestReason reason) =>
        reason is RouteStepDestReason.JobListLoad or RouteStepDestReason.Next;

    public static bool ShouldSetPinCorridorDest(string? reason) =>
        ShouldSetPinCorridorDest(Parse(reason));

    /// <summary>
    /// list-next / list-load must Set dest so <see cref="RoutePinLatch.Observe"/>
    /// runs. Recheck is ignored by Observe (cab 2.13.2.5.22.2 no pin after
    /// couple-next).
    /// </summary>
    public static MapsDestKind DestCommandKindAfterRetarget(string? reason)
    {
        var parsed = Parse(reason);
        return parsed is RouteStepDestReason.JobListLoad or RouteStepDestReason.Next
            ? MapsDestKind.Set
            : MapsDestKind.Recheck;
    }

    /// <summary>
    /// Maps dest the loco actually Sets on list-load / list-next. Pin-legs
    /// use the later corridor track (TT / Prep), not the approach label.
    /// English still prints <see cref="SwitchListStep.DestTrackId"/>.
    /// </summary>
    public static bool TryMapsDestForListProgress(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? reason,
        out string? trackId,
        out MapsDestKind kind,
        out bool pinCorridor)
    {
        kind = DestCommandKindAfterRetarget(reason);
        trackId = null;
        pinCorridor = false;
        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            return false;
        }

        var step = steps[currentIndex];
        if (ShouldSetPinCorridorDest(reason)
            && TryPinCorridorDest(steps, currentIndex, out _, out var corridor)
            && !string.IsNullOrEmpty(corridor))
        {
            trackId = corridor;
            pinCorridor = true;
            return true;
        }

        var dest = step.DestTrackId?.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            return false;
        }

        trackId = dest;
        return true;
    }

    /// <summary>
    /// Past-switch Observe pin. Ahead first-stop is the leave-TT extra pin
    /// (cab 2.13.2.5.8: dest-side last 989918 CLEARED under the loco). Dest-side
    /// last wins when that first-stop is behind (C4S 989976) or missing (Path OK).
    /// </summary>
    public static string? PickPastSwitchObservePin(PathPlanResult? plan, bool pinIsBehind)
    {
        var first = SwitchListRouteLeg.PickPinJunctionId(plan);
        var last = PickLastJunctionId(plan);
        if (string.IsNullOrEmpty(first))
        {
            return last;
        }

        if (pinIsBehind && !string.IsNullOrEmpty(last))
        {
            return last;
        }

        return first;
    }

    /// <summary>
    /// Dest-side frog: last junction traversed on this corridor. Path OK
    /// (aligned, no flips, no sawtooth first-stop) still needs this pin on
    /// a Past-switch approach.
    /// </summary>
    public static string? PickLastJunctionId(PathPlanResult? plan)
    {
        if (plan?.Junctions == null || plan.Junctions.Count == 0)
        {
            return null;
        }

        for (var i = plan.Junctions.Count - 1; i >= 0; i--)
        {
            var id = plan.Junctions[i].JunctionId?.Trim();
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>
    /// CLEARED frog for a past-switch row. Prefer <paramref name="approachLegPlan"/>
    /// (loco → step dest). Corridor-to-TT first-stop is FH-82-correct only when it
    /// matches that approach; SL-55 can first-stop a different frog (cab: latch
    /// 990152, never At switch / CLEARED while rolling past the list frog).
    /// </summary>
    public static string? PickPastSwitchPinJunctionId(
        PathPlanResult? approachLegPlan,
        PathPlanResult? corridorToPinDestPlan)
    {
        var approachPin = SwitchListRouteLeg.PickPinJunctionId(approachLegPlan);
        if (!string.IsNullOrEmpty(approachPin))
        {
            return approachPin;
        }

        return SwitchListRouteLeg.PickPinJunctionId(corridorToPinDestPlan);
    }

    /// <summary>
    /// True when corridor-to-TT first-stop pin disagrees with the approach-leg pin.
    /// HTP: SL-55 must not keep the TT pin when the past-switch leg owns another frog.
    /// </summary>
    public static bool CorridorPinDisagreesWithApproach(
        PathPlanResult? approachLegPlan,
        PathPlanResult? corridorToPinDestPlan)
    {
        var approachPin = SwitchListRouteLeg.PickPinJunctionId(approachLegPlan);
        var corridorPin = SwitchListRouteLeg.PickPinJunctionId(corridorToPinDestPlan);
        if (string.IsNullOrEmpty(approachPin) || string.IsNullOrEmpty(corridorPin))
        {
            return false;
        }

        return !string.Equals(approachPin, corridorPin, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// After a Prep couple, the next Past-switch extra pin is dest-side of the
    /// later Prep corridor — not the B4L approach frog already behind in the
    /// spur (cab 2.13.2.5.9: relatch 989976→1003030 reverse=1, instant CLEARED,
    /// then At switch 315 m that never CLEARED).
    /// </summary>
    public static bool PreferCorridorDestSidePin(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex)
    {
        if (steps == null || currentIndex < 1 || currentIndex >= steps.Count)
        {
            return false;
        }

        var current = steps[currentIndex];
        if (!SwitchListRunner.StepNeedsPinClearance(current.Kind))
        {
            return false;
        }

        return steps[currentIndex - 1].Kind == SwitchListStepKind.Prep;
    }

    /// <summary>
    /// Relatch pin after Set dest. Pull-out from Prep uses dest-side last.
    /// Inbound / leave-TT still prefers the approach-leg first-stop.
    /// </summary>
    public static string? PickRelatchPastSwitchPin(
        PathPlanResult? approachPlan,
        PathPlanResult? corridorPlan,
        bool preferCorridorDestSide)
    {
        if (preferCorridorDestSide)
        {
            return PickPastSwitchObservePin(corridorPlan, pinIsBehind: true);
        }

        return PickPastSwitchPinJunctionId(approachPlan, corridorPlan);
    }

    /// <summary>
    /// World past-switch stop: first corridor frog the consist has not already
    /// CLEARED. Spent first-stop / dest-side last is the leave-TT 989918 and
    /// B4L 1003030 keep-going. <paramref name="isSpent"/> false/unknown keeps
    /// the candidate (fail open).
    /// </summary>
    public static string? PickFirstUnspentJunctionId(
        PathPlanResult? plan,
        System.Func<string, bool>? isSpent)
    {
        if (plan == null)
        {
            return null;
        }

        string? picked = null;
        Consider(plan.JunctionFirstStop?.JunctionId, isSpent, ref picked);
        if (plan.Junctions == null)
        {
            return picked;
        }

        for (var i = 0; i < plan.Junctions.Count; i++)
        {
            Consider(plan.Junctions[i].JunctionId, isSpent, ref picked);
        }

        return picked;
    }

    public static string? PickRelatchPastSwitchPin(
        PathPlanResult? approachPlan,
        PathPlanResult? corridorPlan,
        bool preferCorridorDestSide,
        System.Func<string, bool>? isSpent)
    {
        if (preferCorridorDestSide)
        {
            if (isSpent != null)
            {
                var destSide = PickPastSwitchObservePin(corridorPlan, pinIsBehind: true);
                if (!string.IsNullOrEmpty(destSide) && !isSpent(destSide!))
                {
                    return destSide;
                }

                var corridorLive = PickLastUnspentJunctionId(corridorPlan, isSpent);
                if (!string.IsNullOrEmpty(corridorLive))
                {
                    return corridorLive;
                }
            }

            return PickRelatchPastSwitchPin(approachPlan, corridorPlan, preferCorridorDestSide: true);
        }

        if (isSpent != null)
        {
            var live = PickFirstUnspentJunctionId(approachPlan, isSpent)
                ?? PickFirstUnspentJunctionId(corridorPlan, isSpent);
            if (!string.IsNullOrEmpty(live))
            {
                return live;
            }
        }

        return PickRelatchPastSwitchPin(approachPlan, corridorPlan, preferCorridorDestSide);
    }

    public static string? PickLastUnspentJunctionId(
        PathPlanResult? plan,
        System.Func<string, bool>? isSpent)
    {
        if (plan == null)
        {
            return null;
        }

        if (plan.Junctions != null)
        {
            for (var i = plan.Junctions.Count - 1; i >= 0; i--)
            {
                var id = plan.Junctions[i].JunctionId?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (isSpent == null || !isSpent(id!))
                {
                    return id;
                }
            }
        }

        var first = plan.JunctionFirstStop?.JunctionId?.Trim();
        if (!string.IsNullOrEmpty(first) && (isSpent == null || !isSpent(first!)))
        {
            return first;
        }

        return null;
    }

    private static void Consider(
        string? raw,
        System.Func<string, bool>? isSpent,
        ref string? picked)
    {
        if (picked != null)
        {
            return;
        }

        var id = raw?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (isSpent != null && isSpent(id!))
        {
            return;
        }

        picked = id;
    }

    public static bool ShouldRetargetMapsDest(string? reason, RouteClearancePhase phase) =>
        ShouldRetargetMapsDest(Parse(reason), phase);

    public static bool ShouldRetargetMapsDest(
        string? reason,
        RouteClearancePhase phase,
        SwitchListStepKind? stepKind) =>
        ShouldRetargetMapsDest(Parse(reason), phase, stepKind);
}
