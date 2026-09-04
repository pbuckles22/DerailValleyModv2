namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.4</b> drive-to-TT dest arrival — loco on TT rail → Stop GO (spin stays human).
/// Precision stop / predictive brake testbed before <b>9.2</b>.
/// </summary>
public enum TurntableArrival
{
    OffTrack,
    Ambiguous,
    AtTrack,
}

public static class TurntableArrivalGate
{
    public static bool StepWantsArrival(SwitchListStep? step) =>
        step != null && SwitchListDriveFacing.IsDriveToTurntable(step.Label);

    public static TurntableArrival Evaluate(
        SwitchListStep? step,
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack)
    {
        if (!StepWantsArrival(step))
        {
            return TurntableArrival.OffTrack;
        }

        if (!uniqueTrack)
        {
            return TurntableArrival.Ambiguous;
        }

        if (string.IsNullOrWhiteSpace(destTrackId) || string.IsNullOrWhiteSpace(locoTrackId))
        {
            return TurntableArrival.Ambiguous;
        }

        if (float.IsNaN(spanMeters)
            || float.IsInfinity(spanMeters)
            || float.IsNaN(trackLengthMeters)
            || float.IsInfinity(trackLengthMeters)
            || trackLengthMeters <= 0f)
        {
            return TurntableArrival.Ambiguous;
        }

        if (!string.Equals(destTrackId, locoTrackId, System.StringComparison.Ordinal))
        {
            return TurntableArrival.OffTrack;
        }

        var along = TrackPathSpan.WithinTrackMeters(spanMeters, trackLengthMeters, travelIncreasingSpan: true);
        if (float.IsNaN(along) || along < 0f || along > trackLengthMeters)
        {
            return TurntableArrival.Ambiguous;
        }

        return TurntableArrival.AtTrack;
    }

    public static bool ShouldLatchOnTable(TurntableArrival arrival) =>
        arrival == TurntableArrival.AtTrack;

    public static string FormatDeskCue(string? destTrackId)
    {
        var id = destTrackId?.Trim();
        return string.IsNullOrEmpty(id) ? "on TT" : "on TT " + id;
    }
}

public static class TurntableArrivalSession
{
    public static bool OnTable { get; private set; }

    /// <summary>
    /// Rising-edge AtTrack latch. Sticky until <see cref="Clear"/> —
    /// OffTrack / Ambiguous must not drop (cab: stop-tt then re-arm).
    /// </summary>
    public static bool TryArrive(TurntableArrival arrival)
    {
        if (!TurntableArrivalGate.ShouldLatchOnTable(arrival))
        {
            return false;
        }

        if (OnTable)
        {
            return false;
        }

        OnTable = true;
        return true;
    }

    public static void Clear() => OnTable = false;
}
