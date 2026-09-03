namespace YardMasterSuite.Core;

/// <summary>**13.2.2** Prep dest-track arrival — fail-closed when the rail is ambiguous.</summary>
public enum PrepTrackArrival
{
    OffTrack,
    Ambiguous,
    AtTrack,
}

/// <summary>
/// Along-track pose on the Prep dest id → at-track. Junction / missing span /
/// split bogies stay Ambiguous and never auto-advance to at-spur.
/// </summary>
public static class PrepTrackArrivalGate
{
    public static PrepTrackArrival Evaluate(
        SwitchListStepKind? kind,
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack)
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

        return PrepTrackArrival.AtTrack;
    }

    public static bool ShouldAdvanceToAtSpur(PrepTrackArrival arrival) =>
        arrival == PrepTrackArrival.AtTrack;

    public static string FormatDeskCue(string? destTrackId)
    {
        var id = destTrackId?.Trim();
        return string.IsNullOrEmpty(id) ? "at track" : "at track " + id;
    }
}
