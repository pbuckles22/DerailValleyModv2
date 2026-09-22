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
    /// Maps dest the loco actually Sets on list-load / list-next. CLEARED
    /// frog rows Set this-leg label dest (B4L), not look-ahead TT / next Prep.
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
        if (SwitchListPinFacing.IsClearedFrogPin(step))
        {
            var frogDest = step.DestTrackId?.Trim();
            if (string.IsNullOrEmpty(frogDest))
            {
                return false;
            }

            trackId = frogDest;
            return true;
        }

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
    /// Origin track from previous distinct <see cref="SwitchListStep.DestTrackId"/>.
    /// List-load pin board uses this so after-Prep #6 walks B1S→B4L, not
    /// Maps look-ahead C4S.
    /// </summary>
    public static string? WalkFromLabelTrack(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? destTrackId)
    {
        var dest = destTrackId?.Trim();
        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            return null;
        }

        for (var i = currentIndex - 1; i >= 0; i--)
        {
            var prev = steps[i].DestTrackId?.Trim();
            if (!string.IsNullOrEmpty(prev)
                && !string.Equals(prev, dest, System.StringComparison.OrdinalIgnoreCase))
            {
                return prev;
            }
        }

        return steps[currentIndex].DestTrackId?.Trim();
    }

    /// <summary>
    /// Other end of a CLEARED-frog leg. Previous distinct dest, else the next
    /// distinct dest (inbound Past dest is this row — next is the table).
    /// </summary>
    public static string? WalkOppositeEndTrack(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? destTrackId)
    {
        var dest = destTrackId?.Trim();
        var from = WalkFromLabelTrack(steps, currentIndex, dest);
        if (!string.IsNullOrEmpty(from)
            && !string.Equals(from, dest, System.StringComparison.OrdinalIgnoreCase))
        {
            return from;
        }

        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            return null;
        }

        for (var j = currentIndex + 1; j < steps.Count; j++)
        {
            var next = steps[j].DestTrackId?.Trim();
            if (!string.IsNullOrEmpty(next)
                && !string.Equals(next, dest, System.StringComparison.OrdinalIgnoreCase))
            {
                return next;
            }
        }

        return null;
    }

    /// <summary>
    /// Prior Past-switch dest (scan back). After Prep B1S that is B4L — the
    /// pull-out frog lives on that walk, not dest-side last of B1S→C4S.
    /// </summary>
    public static string? WalkPriorPastDest(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? excludeTrackId = null)
    {
        if (steps == null || currentIndex < 1)
        {
            return null;
        }

        var exclude = excludeTrackId?.Trim();
        for (var i = currentIndex - 1; i >= 0; i--)
        {
            var step = steps[i];
            if (step == null || !SwitchListRunner.StepNeedsPinClearance(step.Kind))
            {
                continue;
            }

            var dest = step.DestTrackId?.Trim();
            if (string.IsNullOrEmpty(dest))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(exclude)
                && string.Equals(dest, exclude, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return dest;
        }

        return null;
    }

    /// <summary>
    /// Corridor dest for the numbered CLEARED frog. After Prep, pull-out toward
    /// the prior Past dest when that track is not the spur you are sitting on.
    /// List dest stays the next pickup/haul.
    /// </summary>
    public static string? WalkClearedFrogWalkDest(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? fromTrackId,
        string? listDestTrackId)
    {
        var listDest = listDestTrackId?.Trim();
        if (!PreferCorridorDestSidePin(steps, currentIndex))
        {
            return listDest;
        }

        var from = fromTrackId?.Trim();
        var prior = WalkPriorPastDest(steps, currentIndex, from);
        if (!string.IsNullOrEmpty(prior)
            && !string.Equals(prior, listDest, System.StringComparison.OrdinalIgnoreCase))
        {
            return prior;
        }

        return listDest;
    }

    /// <summary>
    /// After Reverse Prep: first unspent frog inbound from the table/ladder
    /// into this spur — same side as 1+4, not AT 1+4, not spur→ladder first-stop
    /// (cab 22.18: B1S→B4L first-stop sits on the C4S logs).
    /// </summary>
    public static string? WalkAfterPrepPin(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? inboundOriginTrackId,
        string? spurTrackId,
        string? destYardId,
        System.Func<string, bool>? isSpent,
        SpatialGraph spatial = default)
    {
        var origin = inboundOriginTrackId?.Trim();
        var spur = spurTrackId?.Trim();
        if (string.IsNullOrEmpty(origin) || string.IsNullOrEmpty(spur))
        {
            return null;
        }

        var plan = PathPlan.Find(
            edges,
            selected,
            origin,
            spur,
            destYardId: destYardId,
            mode: PathPlanMode.Yard,
            spatial: spatial);
        if (plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        var live = PickFirstUnspentJunctionId(plan, isSpent);
        if (!string.IsNullOrEmpty(live))
        {
            return live;
        }

        return WalkFirstStopPin(edges, selected, origin, spur, destYardId, spatial);
    }

    /// <summary>
    /// Unused: after-Prep pins use <see cref="WalkAfterPrepPin"/>. Keep the
    /// hook so inbound TT-lead first-stop stays on steps 1+4.
    /// </summary>
    public static bool WalkClearedFrogUsesFirstStop(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string? walkDestTrackId,
        string? listDestTrackId)
    {
        _ = walkDestTrackId;
        _ = listDestTrackId;
        _ = steps;
        _ = currentIndex;
        return false;
    }

    /// <summary>
    /// Frog pin for a Past-switch row. Dest-side of from→dest; if that walk
    /// has no junction, dest-side the other way. When the other end is a
    /// turntable, the pin is the first frog toward the table (same switch
    /// inbound and leave).
    /// </summary>
    public static string? WalkClearedFrogPin(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? fromTrackId,
        string? destTrackId,
        string? destYardId,
        bool oppositeEndIsTurntable,
        SpatialGraph spatial = default)
    {
        var from = fromTrackId?.Trim();
        var dest = destTrackId?.Trim();
        if (string.IsNullOrEmpty(from)
            || string.IsNullOrEmpty(dest)
            || string.Equals(from, dest, System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (oppositeEndIsTurntable)
        {
            return WalkFirstStopPin(edges, selected, dest, from, destYardId, spatial)
                ?? WalkDestSidePin(edges, selected, from, dest, destYardId, spatial)
                ?? WalkDestSidePin(edges, selected, dest, from, destYardId, spatial);
        }

        return WalkDestSidePin(edges, selected, from, dest, destYardId, spatial)
            ?? WalkDestSidePin(edges, selected, dest, from, destYardId, spatial);
    }

    public static bool TrackIsTurntableOnList(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        string? trackId)
    {
        var id = trackId?.Trim();
        if (steps == null || string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.Kind != SwitchListStepKind.TurnAround)
            {
                continue;
            }

            if (string.Equals(step.DestTrackId?.Trim(), id, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Dest-side last junction on a yard walk. Null when NoPath or no pin.
    /// </summary>
    public static string? WalkDestSidePin(
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? fromTrackId,
        string? destTrackId,
        string? destYardId,
        SpatialGraph spatial = default)
    {
        var plan = PathPlan.Find(
            edges,
            selected,
            fromTrackId,
            destTrackId,
            destYardId: destYardId,
            mode: PathPlanMode.Yard,
            spatial: spatial);
        if (plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        return PickLastJunctionId(plan);
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
        string? destYardId,
        SpatialGraph spatial = default)
    {
        var plan = PathPlan.Find(
            edges,
            selected,
            fromTrackId,
            mapsDestTrackId,
            destYardId: destYardId,
            mode: PathPlanMode.Yard,
            spatial: spatial);
        if (plan.Status == PathCheckStatus.NoPath)
        {
            return null;
        }

        return SwitchListRouteLeg.PickPinJunctionId(plan);
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
        if (plan?.Junctions != null)
        {
            for (var i = plan.Junctions.Count - 1; i >= 0; i--)
            {
                var id = plan.Junctions[i].JunctionId?.Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }
        }

        return plan?.JunctionFirstStop?.JunctionId?.Trim();
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

        if (steps[currentIndex - 1].Kind != SwitchListStepKind.Prep)
        {
            return false;
        }

        // After last pickup, haul to the loader uses dest-side last — not inbound
        // C-ladder WalkAfterPrepPin (cab 22.56: C4S→B4L latched 1003098).
        return !NextStepIsLoaderSpot(steps, currentIndex);
    }

    /// <summary>
    /// True when the next row is Into-loader on this dest (warehouse haul).
    /// </summary>
    public static bool NextStepIsLoaderSpot(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex)
    {
        if (steps == null || currentIndex < 0 || currentIndex + 1 >= steps.Count)
        {
            return false;
        }

        var dest = steps[currentIndex].DestTrackId?.Trim();
        if (string.IsNullOrEmpty(dest))
        {
            return false;
        }

        var next = steps[currentIndex + 1];
        return SwitchListWarehouseLegs.IsLoaderSpot(next.Label)
            && string.Equals(
                next.DestTrackId?.Trim(),
                dest,
                System.StringComparison.OrdinalIgnoreCase);
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
