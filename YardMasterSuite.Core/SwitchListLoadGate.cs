namespace YardMasterSuite.Core;

/// <summary>
/// Cab 2.13.2.5.22.13: Load before TT inject bound a 7-row SL-55 (spin only).
/// Engineer lock is inbound Past + to-TT + spin + leave Past (10 rows).
/// </summary>
public static class SwitchListLoadGate
{
    public const int EngineerSl55StepCount = 10;

    public static string FormatWaitGraph() => "T2 switch-list: wait graph";

    public static string FormatWaitInject() => "T2 switch-list: wait inject TurnAround";

    /// <summary>Two-pickup same-yard shunt (SL-55). Must not bind a 6/7-row stub.</summary>
    public static bool ExpectsEngineerTenRow(JobSummary? job)
    {
        if (job == null)
        {
            return false;
        }

        var pickups = SwitchListPickupTracks.Resolve(job);
        if (pickups.Count < 2)
        {
            return false;
        }

        var originYard = job.OriginYardId?.Trim();
        var destYard = job.DestYardId?.Trim();
        return !string.IsNullOrEmpty(originYard)
            && string.Equals(originYard, destYard, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool HasCompleteTurnAroundLegs(JobSummary? job)
    {
        if (job == null || !job.NeedsTurnAround)
        {
            return false;
        }

        var tt = job.TurntableTrackId?.Trim();
        var pivot = job.TurntablePivotTrackId?.Trim();
        if (string.IsNullOrEmpty(tt) || string.IsNullOrEmpty(pivot))
        {
            return false;
        }

        var pickups = SwitchListPickupTracks.Resolve(job);
        var origin = pickups.Count > 0 ? pickups[0] : job.OriginTrackId;
        return SwitchListPlanner.LeaveTurntablePastTrack(
            pivot,
            job.PrepApproachTrackId,
            tt,
            origin) != null;
    }

    /// <summary>
    /// Cab 22.14: inject logged TT with no approach/leave (S1775, 7-row spin).
    /// Engineer inbound/leave dest is the reverse-into staging track (B4L).
    /// </summary>
    public static bool TryFillMissingEngineerLegs(JobSummary? job)
    {
        if (job == null || !ExpectsEngineerTenRow(job) || !job.NeedsTurnAround)
        {
            return false;
        }

        if (HasCompleteTurnAroundLegs(job))
        {
            return true;
        }

        var staging = job.ReverseIntoTrackId?.Trim();
        if (string.IsNullOrEmpty(staging))
        {
            return false;
        }

        if (string.IsNullOrEmpty(job.TurntablePivotTrackId?.Trim()))
        {
            job.TurntablePivotTrackId = staging;
            job.TurntableApproachNeedsReverse = true;
        }

        if (string.IsNullOrEmpty(job.PrepApproachTrackId?.Trim()))
        {
            job.PrepApproachTrackId = staging;
        }

        return HasCompleteTurnAroundLegs(job);
    }

    public static string FormatFilledPivot(string? pivot) =>
        "T2 switch-list: engineer pivot " + (string.IsNullOrEmpty(pivot) ? "?" : pivot);

    public static bool ShouldBind(JobSummary? job, System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps)
    {
        if (job == null || steps == null || steps.Count == 0)
        {
            return false;
        }

        if (ExpectsEngineerTenRow(job))
        {
            return HasCompleteTurnAroundLegs(job) && steps.Count >= EngineerSl55StepCount;
        }

        if (job.NeedsTurnAround)
        {
            return HasCompleteTurnAroundLegs(job);
        }

        return true;
    }
}
