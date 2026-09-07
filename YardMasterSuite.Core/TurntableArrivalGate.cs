namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.4</b> drive-to-TT dest arrival — start Stop GO so the crawl lands near TT midpoint
/// (spin stays human). Precision / predictive-brake testbed before <b>9.2</b>.
/// </summary>
public enum TurntableArrival
{
    OffTrack,
    Ambiguous,
    AtTrack,
}

public static class TurntableArrivalGate
{
    /// <summary>Gemini HTP band around <c>L_TT/2</c>.</summary>
    public const float MidpointToleranceMeters = 2f;

    public static float MidpointAlongMeters(float trackLengthMeters) =>
        trackLengthMeters * 0.5f;

    public static float YardStoppingDistanceMeters(float speedKmh) =>
        YardStopKinematics.StoppingDistanceMeters(speedKmh);

    public static bool StepWantsArrival(SwitchListStep? step) =>
        step != null && SwitchListDriveFacing.IsDriveToTurntable(step.Label);

    public static TurntableArrival Evaluate(
        SwitchListStep? step,
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack,
        float speedKmh = 0f)
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

        var remToMid = RemToMidOnDestTrack(
            destTrackId,
            locoTrackId,
            spanMeters,
            trackLengthMeters,
            uniqueTrack);
        if (remToMid is float rem
            && YardStopKinematics.ShouldStartStop(rem, speedKmh, MidpointToleranceMeters))
        {
            return TurntableArrival.AtTrack;
        }

        return remToMid is float ? TurntableArrival.OffTrack : TurntableArrival.Ambiguous;
    }

    /// <summary>
    /// On dest rail: meters to mid, floored at 0 (past-mid is already the kiss).
    /// Null when off the table.
    /// </summary>
    public static float? RemToMidOnDestTrack(
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack)
    {
        if (!uniqueTrack
            || string.IsNullOrWhiteSpace(destTrackId)
            || string.IsNullOrWhiteSpace(locoTrackId)
            || !string.Equals(destTrackId, locoTrackId, System.StringComparison.Ordinal)
            || float.IsNaN(spanMeters)
            || float.IsInfinity(spanMeters)
            || float.IsNaN(trackLengthMeters)
            || float.IsInfinity(trackLengthMeters)
            || trackLengthMeters <= 0f)
        {
            return null;
        }

        var along = TrackPathSpan.WithinTrackMeters(spanMeters, trackLengthMeters, travelIncreasingSpan: true);
        if (float.IsNaN(along) || along < 0f || along > trackLengthMeters)
        {
            return null;
        }

        var rem = MidpointAlongMeters(trackLengthMeters) - along;
        return rem < 0f ? 0f : rem;
    }

    public static bool ShouldLatchOnTable(TurntableArrival arrival) =>
        arrival == TurntableArrival.AtTrack;

    public static string FormatDeskCue(string? destTrackId)
    {
        var id = destTrackId?.Trim();
        return string.IsNullOrEmpty(id) ? "on TT" : "on TT " + id;
    }

    public static string FormatLatchLog(float alongMeters, float trackLengthMeters, float speedKmh)
    {
        var along = (int)System.Math.Round(alongMeters);
        var len = (int)System.Math.Round(trackLengthMeters);
        var spd = (int)System.Math.Round(speedKmh < 0f ? -speedKmh : speedKmh);
        return SwitchListRunnerTelemetry.TurntableAtTrack
            + " along=" + along
            + " len=" + len
            + " spd=" + spd;
    }
}

public static class TurntableArrivalSession
{
    public static bool OnTable { get; private set; }

    /// <summary>Rem to TT midpoint when on dest rail; null off-table / unknown.</summary>
    public static float? RemToMidMeters { get; private set; }

    public static void ObserveRemToMid(float? remToMidMeters)
    {
        if (remToMidMeters is float r
            && !float.IsNaN(r)
            && !float.IsInfinity(r)
            && r >= 0f)
        {
            RemToMidMeters = r;
            return;
        }

        RemToMidMeters = null;
    }

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

    public static void Clear()
    {
        OnTable = false;
        RemToMidMeters = null;
    }
}
