namespace YardMasterSuite.Core;

/// <summary>**13.2.2** / <b>13.4</b> Prep dest-track arrival — fail-closed when the rail is ambiguous.</summary>
public enum PrepTrackArrival
{
    OffTrack,
    Ambiguous,
    AtTrack,
}

/// <summary>
/// Along-track pose on the Prep dest id → start Stop GO so crawl lands near spur end
/// (pad before bumper / cars). Junction / missing span / split bogies stay Ambiguous.
/// Same rem ≤ d_stop recipe as TT (Gemini tactical — not full 9.2).
/// </summary>
public static class PrepTrackArrivalGate
{
    /// <summary>Aim this many meters before track end (stand-in until consist length is owned).</summary>
    public const float AimPadMeters = 8f;

    /// <summary>Band around aim — same width as TT mid tol.</summary>
    public const float AimToleranceMeters = 2f;

    public static float AimAlongMeters(float trackLengthMeters)
    {
        var aim = trackLengthMeters - AimPadMeters;
        return aim < 0f ? 0f : aim;
    }

    public static PrepTrackArrival Evaluate(
        SwitchListStepKind? kind,
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack,
        float speedKmh = 0f)
    {
        if (kind != SwitchListStepKind.Prep)
        {
            return PrepTrackArrival.OffTrack;
        }

        if (!uniqueTrack)
        {
            return PrepTrackArrival.Ambiguous;
        }

        if (string.IsNullOrWhiteSpace(destTrackId) || string.IsNullOrWhiteSpace(locoTrackId))
        {
            return PrepTrackArrival.Ambiguous;
        }

        if (float.IsNaN(spanMeters)
            || float.IsInfinity(spanMeters)
            || float.IsNaN(trackLengthMeters)
            || float.IsInfinity(trackLengthMeters)
            || trackLengthMeters <= 0f)
        {
            return PrepTrackArrival.Ambiguous;
        }

        if (!string.Equals(destTrackId, locoTrackId, System.StringComparison.Ordinal))
        {
            return PrepTrackArrival.OffTrack;
        }

        var along = TrackPathSpan.WithinTrackMeters(spanMeters, trackLengthMeters, travelIncreasingSpan: true);
        if (float.IsNaN(along) || along < 0f || along > trackLengthMeters)
        {
            return PrepTrackArrival.Ambiguous;
        }

        var aim = AimAlongMeters(trackLengthMeters);
        var rem = aim - along;
        if (YardStopKinematics.ShouldStartStop(rem, speedKmh, AimToleranceMeters))
        {
            return PrepTrackArrival.AtTrack;
        }

        return PrepTrackArrival.OffTrack;
    }

    public static bool ShouldAdvanceToAtSpur(PrepTrackArrival arrival) =>
        arrival == PrepTrackArrival.AtTrack;

    public static string FormatDeskCue(string? destTrackId)
    {
        var id = destTrackId?.Trim();
        return string.IsNullOrEmpty(id) ? "at track" : "at track " + id;
    }

    public static string FormatLatchLog(float alongMeters, float trackLengthMeters, float speedKmh)
    {
        var along = (int)System.Math.Round(alongMeters);
        var len = (int)System.Math.Round(trackLengthMeters);
        var spd = (int)System.Math.Round(speedKmh < 0f ? -speedKmh : speedKmh);
        return SwitchListRunnerTelemetry.PrepAtTrack
            + " along=" + along
            + " len=" + len
            + " spd=" + spd;
    }
}
