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
    /// track so Set dest latches the sawtooth pin — except pull-out after
    /// Prep, which stays on this row's dest (engineer bible).
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

        // Engineer: after Prep, this Past-switch is the pull-out frog (this
        // DestTrackId). Looking ahead to the next Prep plants a behind pin
        // and throws Reverse into the cut.
        if (IsPullOutAfterPrep(steps, currentIndex))
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
    /// Past-switch immediately after Prep: pull out of that spur. Pin is the
    /// mouth hop, not JunctionFirstStop toward named B4L (1+4 sawtooth).
    /// </summary>
    public static bool IsPullOutAfterPrep(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex)
    {
        if (steps == null || currentIndex <= 0 || currentIndex >= steps.Count)
        {
            return false;
        }

        var current = steps[currentIndex];
        return current != null
            && SwitchListRunner.StepNeedsPinClearance(current.Kind)
            && steps[currentIndex - 1].Kind == SwitchListStepKind.Prep;
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
    /// Maps dest on list-load / list-next. Leave-TT pin-legs still Set the
    /// later corridor (TT / first Prep). Pull-out after Prep Sets this row.
    /// </summary>
    public static bool TryMapsDestForListProgress(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? reason,
        out string? trackId,
        out MapsDestKind kind,
        out bool pinCorridor) =>
        TryMapsDestForListProgress(
            steps, currentIndex, reason, out _, out trackId, out kind, out pinCorridor);

    public static bool TryMapsDestForListProgress(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? reason,
        out string? yardId,
        out string? trackId,
        out MapsDestKind kind,
        out bool pinCorridor)
    {
        kind = DestCommandKindAfterRetarget(reason);
        yardId = null;
        trackId = null;
        pinCorridor = false;
        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            return false;
        }

        var step = steps[currentIndex];
        if (ShouldSetPinCorridorDest(reason)
            && TryPinCorridorDest(steps, currentIndex, out var pinYard, out var corridor)
            && !string.IsNullOrEmpty(corridor))
        {
            trackId = corridor;
            yardId = string.IsNullOrWhiteSpace(pinYard) ? step.DestYardId : pinYard;
            pinCorridor = true;
            return true;
        }

        var dest = step.DestTrackId?.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            return false;
        }

        trackId = dest;
        yardId = step.DestYardId;
        return true;
    }

    /// <summary>
    /// Unity <c>ApplyStepDest</c> gate. Pin-leg list-load still Sets the
    /// corridor (Recheck-to-approach steal). Other holds stay held.
    /// </summary>
    public static bool ShouldApplyListProgressDest(
        string? reason,
        RouteClearancePhase phase,
        SwitchListStepKind? stepKind,
        bool pinCorridor)
    {
        if (ShouldRetargetMapsDest(reason, phase, stepKind))
        {
            return true;
        }

        return Parse(reason) == RouteStepDestReason.JobListLoad && pinCorridor;
    }

    /// <summary>
    /// Origin track for a Maps dest walk: previous distinct Maps dest, else
    /// this row's label dest (approach).
    /// </summary>
    public static string? WalkFromTrack(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? mapsDestTrackId)
    {
        var maps = mapsDestTrackId?.Trim();
        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            return null;
        }

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            if (!TryMapsDestForListProgress(steps, i, "list-next", out var prev, out _, out _))
            {
                continue;
            }

            var p = prev?.Trim();
            if (!string.IsNullOrEmpty(p)
                && !string.Equals(p, maps, System.StringComparison.Ordinal))
            {
                return p;
            }
        }

        return steps[currentIndex].DestTrackId?.Trim();
    }

    /// <summary>
    /// Harvest-graph first-stop pin from live-style origin to Maps dest.
    /// Yard mode. Null when NoPath or no pin.
    /// </summary>
    public static string? WalkFirstStopPin(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? fromTrackId,
        string? mapsDestTrackId,
        string? destYardId)
    {
        var plan = PathPlan.Find(
            edges,
            selected,
            fromTrackId,
            mapsDestTrackId,
            destYardId: destYardId,
            mode: PathPlanMode.Yard);
        if (plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        return SwitchListRouteLeg.PickPinJunctionId(plan);
    }

    /// <summary>
    /// Named dest-side last on origin → this-leg dest. Cab 22.3/22.6: yellow
    /// pin on B4L, not the Prep spur mouth (22.7 S961 short of B4L).
    /// </summary>
    public static string? WalkPullOutThroatPin(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? fromTrackId,
        string? mapsDestTrackId,
        string? destYardId)
    {
        var plan = PathPlan.Find(
            edges,
            selected,
            fromTrackId,
            mapsDestTrackId,
            destYardId: destYardId,
            mode: PathPlanMode.Yard);
        if (plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        return PickLastJunctionId(plan);
    }

    /// <summary>
    /// Pull-out Observe: dest-side last only when this plan ends on the
    /// pin-leg dest (named B4L). A longer corridor's last frog is C4S.
    /// </summary>
    public static string? PickPullOutNamedDestPin(PathPlanResult? plan, string? destTrackId)
    {
        var dest = destTrackId?.Trim();
        if (plan?.TrackIds == null || plan.TrackIds.Count == 0 || string.IsNullOrEmpty(dest))
        {
            return PickLastJunctionId(plan);
        }

        var lastTrack = plan.TrackIds[plan.TrackIds.Count - 1]?.Trim();
        if (string.Equals(lastTrack, dest, System.StringComparison.Ordinal))
        {
            return PickLastJunctionId(plan);
        }

        return SwitchListRouteLeg.PickPinJunctionId(plan);
    }

    /// <summary>
    /// First junction along the corridor hops, ignoring sawtooth first-stop.
    /// </summary>
    public static string? PickFirstPathJunctionId(PathPlanResult? plan)
    {
        if (plan?.Junctions == null)
        {
            return SwitchListRouteLeg.PickPinJunctionId(plan);
        }

        for (var i = 0; i < plan.Junctions.Count; i++)
        {
            var id = plan.Junctions[i].JunctionId?.Trim();
            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }
        }

        return SwitchListRouteLeg.PickPinJunctionId(plan);
    }

    /// <summary>
    /// Past-switch Observe pin. Ahead first-stop is the leave-TT extra pin
    /// (cab 2.13.2.5.8: dest-side last 989918 CLEARED under the loco). Dest-side
    /// last wins when that first-stop is behind <b>and</b> the active step dest
    /// is this corridor's last track (C4S Path OK 989976 → 1003160). Past B4L
    /// with Maps dest C4S must not steal dest-side.
    /// </summary>
    public static string? PickPastSwitchObservePin(PathPlanResult? plan, bool pinIsBehind)
    {
        var first = SwitchListRouteLeg.PickPinJunctionId(plan);
        var last = PickLastJunctionId(plan);
        if (string.IsNullOrEmpty(first))
        {
            return last;
        }

        if (pinIsBehind
            && !string.IsNullOrEmpty(last)
            && ObserveDestSideAllowed(plan))
        {
            return last;
        }

        return first;
    }

    /// <summary>
    /// Dest-side last is the C4S Path OK fallback only when the live step names
    /// that same dest. No step (plain Set dest) stays fail-open.
    /// </summary>
    public static bool ObserveDestSideAllowed(PathPlanResult? plan)
    {
        var stepDest = SwitchListSession.CurrentStep?.DestTrackId?.Trim();
        if (string.IsNullOrEmpty(stepDest))
        {
            return true;
        }

        if (plan?.TrackIds == null || plan.TrackIds.Count == 0)
        {
            return true;
        }

        var lastTrack = plan.TrackIds[plan.TrackIds.Count - 1]?.Trim();
        return !string.IsNullOrEmpty(lastTrack)
            && string.Equals(stepDest, lastTrack, System.StringComparison.Ordinal);
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
    /// Prep is not a pin rule. Label dest ≠ Maps dest ⇒ pin is the approach
    /// walk (loco → named switch), never Maps dest-side last.
    /// </summary>
    public static bool PreferCorridorDestSidePin(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex)
    {
        _ = steps;
        _ = currentIndex;
        return false;
    }

    /// <summary>
    /// Relatch pin after Set dest. Always the approach-to-label-dest frog.
    /// Maps corridor dest-side is a different string (Prep dest), not this pin.
    /// </summary>
    public static string? PickRelatchPastSwitchPin(
        PathPlanResult? approachPlan,
        PathPlanResult? corridorPlan,
        bool preferCorridorDestSide)
    {
        _ = preferCorridorDestSide;
        _ = corridorPlan;
        if (IsPullOutAfterPrep(SwitchListSession.Steps, SwitchListSession.CurrentIndex))
        {
            return PickPullOutNamedDestPin(approachPlan, SwitchListSession.CurrentStep?.DestTrackId);
        }

        return SwitchListRouteLeg.PickPinJunctionId(approachPlan);
    }

    /// <summary>
    /// Cab 2.13.2.5.22.6: approach-relatch <c>reverse=1</c> after B1S couple
    /// overwrote Set Forward pull-out. Planner bind wins; live behind is only
    /// the fallback when bind is unset.
    /// </summary>
    public static bool RelatchTravelUsesReverse(SwitchListStep? step, bool liveTargetBehind)
    {
        if (step?.BindNeedsReverse is bool bind)
        {
            return bind;
        }

        return liveTargetBehind;
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

    /// <summary>
    /// Pull-out after Prep: spend along hops from the spur, never sawtooth
    /// first-stop (that is the 1+4 B4L frog).
    /// </summary>
    public static string? PickFirstUnspentPathJunctionId(
        PathPlanResult? plan,
        System.Func<string, bool>? isSpent)
    {
        if (plan?.Junctions == null)
        {
            return PickFirstPathJunctionId(plan);
        }

        string? picked = null;
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
        _ = preferCorridorDestSide;
        _ = corridorPlan;
        if (isSpent != null)
        {
            return IsPullOutAfterPrep(SwitchListSession.Steps, SwitchListSession.CurrentIndex)
                ? PickLastUnspentJunctionId(approachPlan, isSpent)
                : PickFirstUnspentJunctionId(approachPlan, isSpent);
        }

        return PickRelatchPastSwitchPin(approachPlan, corridorPlan, preferCorridorDestSide: false);
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
