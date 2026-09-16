using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Mid-job Switch List across save/load. World leave wipes drive sessions
/// (PID poison); this snapshot is not in <see cref="YmsRouteSessions.ClearAll"/>.
/// Desk Clear forgets it so the HUD list does not come back.
/// </summary>
public static class SwitchListResumeSession
{
    private static string? _jobId;
    private static SwitchListStep[]? _steps;
    private static int _index;

    public static bool HasResume =>
        _steps != null && _steps.Length > 0 && !string.IsNullOrEmpty(_jobId);

    public static void Capture(string? jobId, IReadOnlyList<SwitchListStep>? steps, int index)
    {
        var id = jobId?.Trim();
        if (string.IsNullOrEmpty(id) || steps == null || steps.Count == 0)
        {
            Forget();
            return;
        }

        if (index < 0)
        {
            index = 0;
        }

        if (index >= steps.Count)
        {
            Forget();
            return;
        }

        var copy = new SwitchListStep[steps.Count];
        for (var i = 0; i < steps.Count; i++)
        {
            copy[i] = steps[i];
        }

        _jobId = id;
        _steps = copy;
        _index = index;
    }

    public static void CaptureLive()
    {
        if (!SwitchListSession.HasActive || SwitchListSession.IsComplete)
        {
            Forget();
            return;
        }

        Capture(SwitchListSession.JobId, SwitchListSession.Steps, SwitchListSession.CurrentIndex);
    }

    public static bool TryRestore()
    {
        if (!HasResume)
        {
            return false;
        }

        SwitchListSession.Bind(_jobId!, _steps!);
        return SwitchListSession.TrySeek(_index);
    }

    public static void Forget()
    {
        _jobId = null;
        _steps = null;
        _index = 0;
    }
}
