namespace YardMasterSuite.Core;

/// <summary>Switch List step kinds — Align target per step (**8.3** + **8.5**).</summary>
public enum SwitchListStepKind
{
    Prep,
    TurnAround,
    ReverseInto,
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
    /// <summary>Reverse-into spur Align leg (**8.5**); typically penultimate job dest or final when last hop reverse.</summary>
    public bool NeedsReverseInto { get; set; }
    public string? ReverseIntoTrackId { get; set; }
}

/// <summary>One Align leg on a Switch List.</summary>
public sealed class SwitchListStep
{
    public SwitchListStep(int index, SwitchListStepKind kind, string? destYardId, string destTrackId, string label)
    {
        Index = index;
        Kind = kind;
        DestYardId = destYardId;
        DestTrackId = destTrackId;
        Label = label;
    }

    public int Index { get; }
    public SwitchListStepKind Kind { get; }
    public string? DestYardId { get; }
    public string DestTrackId { get; }
    public string Label { get; }
}

/// <summary>Pure job → ordered Switch List steps (**8.3** / **8.5**).</summary>
public static class SwitchListPlanner
{
    /// <summary>
    /// Fail closed (null) when origin/dest tracks missing, or orientation flags lack tracks.
    /// Order: [TurnAround] → Prep → [ReverseInto] → Transit → Delivery.
    /// Table before Prep so the player turns 180° then backs into pickup (v1 3.7b).
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
        var steps = new System.Collections.Generic.List<SwitchListStep>(5);
        var i = 1;

        if (turntable != null)
        {
            steps.Add(new SwitchListStep(
                i++,
                SwitchListStepKind.TurnAround,
                job.OriginYardId,
                turntable,
                "Turn around → " + turntable));
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
                SwitchListStepKind.Transit,
                yard,
                pivot,
                SwitchListDriveFacing.FormatDriveLabel(pivotNeedsReverse, "Pivot", pivot)));
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
            SwitchListDriveFacing.FormatDriveLabel(turntableNeedsReverse, "Turn around", tt)));
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
