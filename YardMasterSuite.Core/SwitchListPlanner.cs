namespace YardMasterSuite.Core;

/// <summary>Switch List step kinds — Align target per step (**8.3** + **8.5**).</summary>
public enum SwitchListStepKind
{
    Prep,
    TurnAround,
    ReverseInto,
    /// <summary>TT multi-leg approach track before turn-around (**8.7** clearance pin).</summary>
    Pivot,
    Transit,
    Delivery,
}

/// <summary>DTO for Switch List planning — Unity maps DV Job → this (Core stays game-free).</summary>
public sealed class JobSummary
{
    public string JobId { get; set; } = "";
    public string? JobTypeLabel { get; set; }
    public string? OriginYardId { get; set; }
    public string? DestYardId { get; set; }
    public string? OriginTrackId { get; set; }
    public string? DestTrackId { get; set; }
    /// <summary>Optional arrival / through track for Transit; defaults to <see cref="DestTrackId"/>.</summary>
    public string? DestArrivalTrackId { get; set; }
    public bool NeedsTurnAround { get; set; }
    public string? TurntableTrackId { get; set; }
    /// <summary>Pivot approach track before TT when face-into-Exit (**13.1**).</summary>
    public string? TurntablePivotTrackId { get; set; }
    /// <summary>Face-into-Exit TT inject: approach leg is Set Reverse (**13.1**).</summary>
    public bool TurntableApproachNeedsReverse { get; set; }
    /// <summary>TT → Prep sawtooth: Past switch until CLEARED after TurnAround (**13.1**).</summary>
    public string? PrepApproachTrackId { get; set; }
    /// <summary>Reverse-into spur Align leg (**8.5**); typically penultimate job dest or final when last hop reverse.</summary>
    public bool NeedsReverseInto { get; set; }
    public string? ReverseIntoTrackId { get; set; }
}

/// <summary>One Align leg on a Switch List.</summary>
public sealed class SwitchListStep
{
    public SwitchListStep(
        int index,
        SwitchListStepKind kind,
        string? destYardId,
        string destTrackId,
        string label,
        bool? bindNeedsReverse = null)
    {
        Index = index;
        Kind = kind;
        DestYardId = destYardId;
        DestTrackId = destTrackId;
        Label = label;
        BindNeedsReverse = bindNeedsReverse;
    }

    public int Index { get; }
    public SwitchListStepKind Kind { get; }
    public string? DestYardId { get; }
    public string DestTrackId { get; }
    public string Label { get; }
    /// <summary>Planner bind-time Set word; live cab geometry must not flip this (**13.1**).</summary>
    public bool? BindNeedsReverse { get; }
}

/// <summary>Pure job → ordered Switch List steps (**8.3** / **8.5**).</summary>
public static class SwitchListPlanner
{
    /// <summary>
    /// Fail closed (null) when origin/dest tracks missing, or orientation flags lack tracks.
    /// Order: [Past switch until CLEARED] → [TurnAround] → [Past switch until CLEARED] →
    /// Prep → [ReverseInto] → Transit → Delivery.
    /// Leave-TT clearance is its own row so Prep stays Human/Done (not a stuffed pin).
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<SwitchListStep>? Build(JobSummary? job)
    {
        if (job == null)
        {
            return null;
        }

        var origin = Normalize(job.OriginTrackId);
        var dest = Normalize(job.DestTrackId);
        if (origin == null || dest == null)
        {
            return null;
        }

        string? turntable = null;
        if (job.NeedsTurnAround)
        {
            turntable = Normalize(job.TurntableTrackId);
            if (turntable == null)
            {
                return null;
            }
        }

        string? reverseInto = null;
        if (job.NeedsReverseInto)
        {
            reverseInto = Normalize(job.ReverseIntoTrackId);
            if (reverseInto == null)
            {
                return null;
            }
        }

        var arrival = Normalize(job.DestArrivalTrackId) ?? dest;
        var steps = new System.Collections.Generic.List<SwitchListStep>(6);
        var i = 1;

        if (turntable != null)
        {
            var approach = Normalize(job.TurntablePivotTrackId);
            if (approach != null && !Same(approach, turntable))
            {
                var approachReverse = job.TurntableApproachNeedsReverse;
                steps.Add(new SwitchListStep(
                    i++,
                    SwitchListStepKind.Transit,
                    job.OriginYardId,
                    approach,
                    SwitchListDriveFacing.FormatDriveLabel(
                        approachReverse,
                        "Past switch",
                        approach)
                        + " until CLEARED",
                    bindNeedsReverse: approachReverse ? true : null));
            }

            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.TurnAround,
                job.OriginYardId,
                turntable,
                SwitchListDriveFacing.TurnAroundOnTurntable,
                bindNeedsReverse: false));

            var leave = Normalize(job.PrepApproachTrackId);
            if (leave != null && !Same(leave, origin))
            {
                steps.Add(new SwitchListStep(
                    i++,
                    SwitchListStepKind.Transit,
                    job.OriginYardId,
                    leave,
                    SwitchListDriveFacing.FormatDriveLabel(false, "Past switch", leave)
                        + " until CLEARED"));
            }
        }

        steps.Add(new SwitchListStep(i++, SwitchListStepKind.Prep, job.OriginYardId, origin, "Prep → " + origin));

        if (reverseInto != null
            && !Same(reverseInto, origin)
            && !Same(reverseInto, turntable))
        {
            var riYard = Same(reverseInto, dest) || Same(reverseInto, arrival)
                ? job.DestYardId
                : (job.OriginYardId ?? job.DestYardId);
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.ReverseInto,
                riYard,
                reverseInto,
                "Reverse into → " + reverseInto));
        }

        steps.Add(new SwitchListStep(
            i++,
            SwitchListStepKind.Transit,
            job.DestYardId,
            arrival,
            "Transit → " + arrival));

        steps.Add(new SwitchListStep(
            i,
            SwitchListStepKind.Delivery,
            job.DestYardId,
            dest,
            "Delivery → " + dest));

        return steps;
    }

    /// <summary>
    /// Route tab multi-leg when sawtooth pin + backing into dest (**8.7**).
    /// Fail closed (null) for straight single-leg paths.
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<SwitchListStep>? BuildFromRoute(
        string? yardId,
        string? destTrackId,
        PathPlanResult? plan,
        bool pinNeedsReverse,
        bool destNeedsReverse)
    {
        if (plan == null || !NeedsRouteSwitchList(plan, destNeedsReverse))
        {
            return null;
        }

        var dest = Normalize(destTrackId);
        if (dest == null || plan.TrackIds.Count == 0)
        {
            return null;
        }

        var yard = string.IsNullOrWhiteSpace(yardId) ? null : yardId!.Trim();
        var steps = new System.Collections.Generic.List<SwitchListStep>(3);
        var i = 1;
        var needsReverse = destNeedsReverse || plan.LastHopRequiresReverse;
        var destSetReverse = RouteDestFacingPolicy.DestNeedsReverse(
            pinNeedsReverse,
            destNeedsReverse);

        if (SwitchListRouteLeg.ShouldArmPin(plan))
        {
            var approachTrack = PickSawtoothApproachTrack(plan);
            if (approachTrack != null)
            {
                steps.Add(new SwitchListStep(
                    i++,
                    SwitchListStepKind.Transit,
                    yard,
                    approachTrack,
                    SwitchListDriveFacing.FormatDriveLabel(pinNeedsReverse, "Past switch", approachTrack)
                        + " until CLEARED"));
            }
        }

        if (needsReverse)
        {
            steps.Add(new SwitchListStep(
                i,
                SwitchListStepKind.ReverseInto,
                yard,
                dest,
                SwitchListDriveFacing.FormatDriveLabel(
                    destSetReverse,
                    destSetReverse ? "Reverse into" : "into",
                    dest)));
        }
        else if (steps.Count > 0)
        {
            steps.Add(new SwitchListStep(
                i,
                SwitchListStepKind.Transit,
                yard,
                dest,
                SwitchListDriveFacing.FormatDriveLabel(destSetReverse, "Transit", dest)));
        }

        return steps.Count > 0 ? steps : null;
    }

    /// <summary>
    /// Migrate Route tab to Switch List when sawtooth clearance and backing are both required.
    /// </summary>
    public static bool NeedsRouteSwitchList(PathPlanResult? plan, bool destNeedsReverse)
    {
        if (plan == null)
        {
            return false;
        }

        var needsReverse = destNeedsReverse || plan.LastHopRequiresReverse;
        return SwitchListRouteLeg.ShouldArmPin(plan) && needsReverse;
    }

    /// <summary>
    /// Sawtooth approach only. Flips-without-first-stop is Align-on-Prep, not a
    /// Past switch row — live misaligned switches at Load must not invent a
    /// phantom TT clearance step.
    /// </summary>
    public static string? TryPickTurntableApproachTrack(PathPlanResult? planToTurntable)
    {
        if (planToTurntable?.JunctionFirstStop is not PathJunctionFirstStop stop)
        {
            return null;
        }

        if (string.IsNullOrEmpty(stop.JunctionId?.Trim()))
        {
            return null;
        }

        return PickSawtoothApproachTrack(planToTurntable);
    }

    /// <summary>
    /// Loco-side of the sawtooth pin along this corridor — not
    /// <see cref="PathJunctionFirstStop.FromTrackId"/> when that field is the
    /// far hop (<c>#Y-#S989#T</c>). Align dest on S989 is the cheap Recheck
    /// that steals pin 990152.
    /// </summary>
    public static string? PickSawtoothApproachTrack(PathPlanResult plan)
    {
        var origin = plan.TrackIds.Count > 0 ? Normalize(plan.TrackIds[0]) : null;
        var dest = plan.TrackIds.Count > 1
            ? Normalize(plan.TrackIds[plan.TrackIds.Count - 1])
            : null;
        if (origin != null
            && dest != null
            && string.Equals(origin, dest, System.StringComparison.Ordinal))
        {
            origin = null;
        }

        if (plan.JunctionFirstStop is PathJunctionFirstStop stop)
        {
            var farHop = Normalize(stop.FromTrackId);
            var conflictTo = Normalize(stop.ToTrackId);
            if (origin != null
                && farHop != null
                && !string.Equals(origin, farHop, System.StringComparison.Ordinal)
                && !string.Equals(origin, conflictTo, System.StringComparison.Ordinal))
            {
                return origin;
            }

            if (farHop != null
                && (dest == null || !string.Equals(farHop, dest, System.StringComparison.Ordinal)))
            {
                return farHop;
            }
        }

        return origin ?? dest;
    }

    /// <summary>
    /// Town Turntable Align (manual multi-leg) — optional pivot then turntable.
    /// Fail closed without a turntable track id.
    /// Labels include Set Forward / Set Reverse from facing flags.
    /// </summary>
    public static System.Collections.Generic.IReadOnlyList<SwitchListStep>? BuildTownTurntable(
        string? yardId,
        string? turntableTrackId,
        string? pivotTrackId = null,
        bool pivotNeedsReverse = false,
        bool turntableNeedsReverse = false,
        bool insertFacingBeforeTurntable = false)
    {
        var tt = Normalize(turntableTrackId);
        if (tt == null)
        {
            return null;
        }

        var yard = string.IsNullOrWhiteSpace(yardId) ? null : yardId!.Trim();
        var steps = new System.Collections.Generic.List<SwitchListStep>(4);
        var i = 1;
        var pivot = Normalize(pivotTrackId);
        if (pivot != null
            && !string.Equals(pivot, tt, System.StringComparison.OrdinalIgnoreCase))
        {
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.Pivot,
                yard,
                pivot,
                SwitchListDriveFacing.FormatDriveLabel(pivotNeedsReverse, "Pivot", pivot)
                    + " until CLEARED"));
        }

        if (insertFacingBeforeTurntable
            && pivot != null
            && pivotNeedsReverse != turntableNeedsReverse)
        {
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.Prep,
                yard,
                tt,
                SwitchListDriveFacing.FormatFacingOnlyLabel(turntableNeedsReverse)));
        }

        steps.Add(new SwitchListStep(
            i,
            SwitchListStepKind.TurnAround,
            yard,
            tt,
            SwitchListDriveFacing.FormatTurnAroundLabel(turntableNeedsReverse)));
        return steps;
    }

    private static string? Normalize(string? id)
    {
        var t = id?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    private static bool Same(string? a, string? b) =>
        a != null
        && b != null
        && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
}
