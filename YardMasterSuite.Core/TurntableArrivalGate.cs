namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.4</b> drive-to-TT dest arrival — Stop GO so consist center kisses table mid,
/// then auto-spin. Precision / predictive-brake testbed before <b>9.2</b>.
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

    public static float HalfConsistMeters(float consistLengthMeters) =>
        consistLengthMeters > 0f && !float.IsNaN(consistLengthMeters) && !float.IsInfinity(consistLengthMeters)
            ? consistLengthMeters * 0.5f
            : 0f;

    /// <summary>Leading-bogie along that puts consist center on table mid.</summary>
    public static float LeadingAlongForConsistMid(float trackLengthMeters, float consistLengthMeters) =>
        MidpointAlongMeters(trackLengthMeters) + HalfConsistMeters(consistLengthMeters);

    /// <summary>
    /// Meters until consist center reaches table mid. <paramref name="alongMeters"/>
    /// is the leading bogie. Either table end counts down (absolute).
    /// </summary>
    public static float RemToConsistMidMeters(
        float alongMeters,
        float trackLengthMeters,
        float consistLengthMeters)
    {
        var rem = MidpointAlongMeters(trackLengthMeters)
            - (alongMeters - HalfConsistMeters(consistLengthMeters));
        return rem < 0f ? -rem : rem;
    }

    /// <summary>Off-rail: remaining to dest entry plus consist-center aim on the table.</summary>
    public static float OffRailRemMeters(
        float corridorRemMeters,
        float consistLengthMeters,
        float halfTableMeters = 12.5f)
    {
        var corr = corridorRemMeters < 0f ? 0f : corridorRemMeters;
        var half = halfTableMeters > 0f ? halfTableMeters : 0f;
        return corr + half + HalfConsistMeters(consistLengthMeters);
    }

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
        float speedKmh = 0f,
        float consistLengthMeters = 0f)
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
            uniqueTrack,
            consistLengthMeters);
        if (remToMid is float rem
            && YardStopKinematics.ShouldStartStop(rem, speedKmh, MidpointToleranceMeters))
        {
            return TurntableArrival.AtTrack;
        }

        return remToMid is float ? TurntableArrival.OffTrack : TurntableArrival.Ambiguous;
    }

    /// <summary>
    /// On dest rail: meters until consist center reaches table mid.
    /// Split bogies still report rem (kiss); OnTable latch still needs uniqueTrack.
    /// </summary>
    public static float? RemToMidOnDestTrack(
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack,
        float consistLengthMeters = 0f)
    {
        _ = uniqueTrack;
        if (string.IsNullOrWhiteSpace(destTrackId)
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

        return RemToConsistMidMeters(along, trackLengthMeters, consistLengthMeters);
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

    public static bool UniqueOnDest { get; private set; }

    /// <summary>Rem to consist-center vs table mid; null when unknown.</summary>
    public static float? RemToMidMeters { get; private set; }

    public static void ObserveUniqueTrack(bool uniqueTrack) => UniqueOnDest = uniqueTrack;

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
        UniqueOnDest = false;
        RemToMidMeters = null;
    }
}
