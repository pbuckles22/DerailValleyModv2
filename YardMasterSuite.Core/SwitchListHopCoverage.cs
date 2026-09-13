namespace YardMasterSuite.Core;

/// <summary>
/// Harvest hop → Switch List Past-switch. Frog-matrix walks are not the list.
/// A Transit to dest without until-CLEARED does not cover frogs on that hop.
/// </summary>
public static class SwitchListHopCoverage
{
    public static bool HopHasFrog(PathPlanResult? plan)
    {
        if (plan == null
            || plan.Status == PathCheckStatus.NoPath
            || plan.Status == PathCheckStatus.NoOrigin
            || plan.Status == PathCheckStatus.NoDestination)
        {
            return false;
        }

        if (plan.Junctions != null && plan.Junctions.Count > 0)
        {
            return true;
        }

        return plan.JunctionFirstStop != null
            || !string.IsNullOrEmpty(SwitchListRouteLeg.PickPinJunctionId(plan));
    }

    public static bool StepIsPastSwitchClearance(SwitchListStep? step)
    {
        if (step == null || !SwitchListRunner.StepNeedsPinClearance(step.Kind))
        {
            return false;
        }

        var label = step.Label ?? "";
        return label.IndexOf("Past switch", System.StringComparison.Ordinal) >= 0
            || label.IndexOf("until CLEARED", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// True when some Past-switch / until-CLEARED row walks from
    /// <paramref name="fromTrackId"/>.
    /// </summary>
    public static bool CoversFrogHopOrigin(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        string? fromTrackId)
    {
        var from = fromTrackId?.Trim();
        if (steps == null || string.IsNullOrEmpty(from))
        {
            return false;
        }

        for (var i = 0; i < steps.Count; i++)
        {
            if (!StepIsPastSwitchClearance(steps[i]))
            {
                continue;
            }

            if (!RouteStepDestPolicy.TryMapsDestForListProgress(
                    steps,
                    i,
                    "list-next",
                    out var maps,
                    out _,
                    out _))
            {
                continue;
            }

            var origin = RouteStepDestPolicy.WalkFromTrack(steps, i, maps);
            if (string.Equals(origin, from, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// First hop that has frogs on the harvest graph but no Past-switch row
    /// from that origin. Null when every frog-hop is covered.
    /// </summary>
    public static string? FirstUncoveredFrogHop(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected,
        string? destYardId,
        System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, string>> hops)
    {
        if (hops == null)
        {
            return null;
        }

        for (var i = 0; i < hops.Count; i++)
        {
            var from = hops[i].Item1;
            var to = hops[i].Item2;
            var yardTo = PathRouteConstraints.YardIdOf(to)
                ?? PathRouteConstraints.YardIdOf(from)
                ?? destYardId;
            var mode = PathPlanModeSelect.ForTrip(from, to, destYardOverride: yardTo);
            var plan = PathPlan.Find(
                edges,
                selected,
                from,
                to,
                destYardId: yardTo,
                yardFor: PathRouteConstraints.YardIdOf,
                mode: mode);
            if (!HopHasFrog(plan))
            {
                continue;
            }

            if (!CoversFrogHopOrigin(steps, from))
            {
                return from + " → " + to;
            }
        }

        return null;
    }

    /// <summary>
    /// Ticket hops the list must cover when the harvest walk has frogs:
    /// TT approach, pickup→pickup, last pickup→dest (any yard).
    /// NoPath on a local dump is not a frog (HopHasFrog false).
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<System.ValueTuple<string, string>> JobFrogHops(
        JobSummary? job)
    {
        var hops = new System.Collections.Generic.List<System.ValueTuple<string, string>>(8);
        if (job == null)
        {
            return hops;
        }

        var pivot = Trim(job.TurntablePivotTrackId);
        var tt = Trim(job.TurntableTrackId);
        if (job.NeedsTurnAround && pivot != null && tt != null && !Same(pivot, tt))
        {
            hops.Add((pivot, tt));
        }

        var pickups = SwitchListPickupTracks.Resolve(job);
        for (var i = 0; i + 1 < pickups.Count; i++)
        {
            hops.Add((pickups[i], pickups[i + 1]));
        }

        var dest = Trim(job.DestArrivalTrackId) ?? Trim(job.DestTrackId);
        if (pickups.Count > 0 && dest != null && !Same(pickups[pickups.Count - 1], dest))
        {
            hops.Add((pickups[pickups.Count - 1], dest));
        }

        return hops;
    }

    public static string? FirstUncoveredFrogHop(
        JobSummary? job,
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        System.Collections.Generic.IReadOnlyList<PathEdge> edges,
        System.Collections.Generic.IReadOnlyDictionary<string, int> selected)
    {
        if (job == null)
        {
            return null;
        }

        var yard = string.IsNullOrWhiteSpace(job.DestYardId)
            ? job.OriginYardId
            : job.DestYardId;
        return FirstUncoveredFrogHop(
            steps,
            edges,
            selected,
            yard,
            JobFrogHops(job));
    }

    private static string? Trim(string? id)
    {
        var t = id?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    private static bool Same(string a, string b) =>
        string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
}
