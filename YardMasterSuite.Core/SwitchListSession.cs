using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Active Switch List binding (3.6) — selected job + current step for Align.</summary>
public static class SwitchListSession
{
    private static IReadOnlyList<SwitchListStep>? _steps;
    private static string? _jobId;
    private static int _index;

    public static bool HasActive => _steps != null && _steps.Count > 0 && !string.IsNullOrEmpty(_jobId);

    public static string? JobId => _jobId;

    public static IReadOnlyList<SwitchListStep>? Steps => _steps;

    public static int CurrentIndex => _index;

    /// <summary>True after advancing past the last step.</summary>
    public static bool IsComplete => HasActive && _index >= _steps!.Count;

    public static SwitchListStep? CurrentStep =>
        HasActive && _index >= 0 && _index < _steps!.Count ? _steps[_index] : null;

    /// <summary>Step after <see cref="CurrentStep"/>, or null at the last row.</summary>
    public static SwitchListStep? PeekNext =>
        HasActive && _steps != null && _index + 1 >= 0 && _index + 1 < _steps.Count
            ? _steps[_index + 1]
            : null;

    public static string? CurrentAlignTrackId => CurrentStep?.DestTrackId;

    public static void Bind(string jobId, IReadOnlyList<SwitchListStep> steps)
    {
        var id = jobId?.Trim();
        if (string.IsNullOrEmpty(id) || steps == null || steps.Count == 0)
        {
            Clear();
            return;
        }

        _jobId = id;
        _steps = steps;
        _index = 0;
        SwitchListRunnerSession.OnStepEntered(CurrentStep);
    }

    public static bool TryAdvance()
    {
        if (!HasActive || _steps == null)
        {
            return false;
        }

        if (SwitchListRunner.TryManualNext(
                SwitchListRunnerSession.Mode,
                hasNextStep: PeekNext != null) != SwitchListRunnerResult.Ok)
        {
            return false;
        }

        if (_index >= _steps.Count - 1)
        {
            _index = _steps.Count;
            return false;
        }

        _index++;
        SwitchListRunnerSession.OnStepEntered(CurrentStep);
        return true;
    }

    /// <summary>
    /// **13.2.2** / <b>13.4</b>: Prep dest rem-to-aim ≤ d_stop → at-spur latch. Does not Next.
    /// </summary>
    public static bool TryArrivePrepTrack(
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack,
        float speedKmh = 0f)
    {
        var arrival = PrepTrackArrivalGate.Evaluate(
            CurrentStep?.Kind,
            destTrackId,
            locoTrackId,
            spanMeters,
            trackLengthMeters,
            uniqueTrack,
            speedKmh);
        return PrepTrackArrivalSession.TryArrive(arrival);
    }

    /// <summary>
    /// <b>13.4</b> drive-to-TT → on-table latch when rem-to-mid ≤ d_stop (or in mid band).
    /// Does not Next.
    /// </summary>
    public static bool TryArriveTurntable(
        string? destTrackId,
        string? locoTrackId,
        float spanMeters,
        float trackLengthMeters,
        bool uniqueTrack,
        float speedKmh = 0f)
    {
        var arrival = TurntableArrivalGate.Evaluate(
            CurrentStep,
            destTrackId,
            locoTrackId,
            spanMeters,
            trackLengthMeters,
            uniqueTrack,
            speedKmh);
        return TurntableArrivalSession.TryArrive(arrival);
    }

    /// <summary>**13.2.1:** couple-success event → step index++ on Prep only.</summary>
    public static bool TryAdvanceOnCoupleSuccess(bool coupleSuccess)
    {
        if (!SwitchListRunner.ShouldAdvanceOnCoupleSuccess(
                CurrentStep?.Kind,
                SwitchListRunnerSession.Mode,
                PeekNext != null,
                coupleSuccess))
        {
            return false;
        }

        return TryAdvance();
    }

    public static void Clear()
    {
        _jobId = null;
        _steps = null;
        _index = 0;
        SwitchListRunnerSession.Clear();
        PrepTrackArrivalSession.Clear();
        TurntableArrivalSession.Clear();
        PrepCreepSession.Clear();
    }
}
