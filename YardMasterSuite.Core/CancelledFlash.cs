namespace YardMasterSuite.Core;

/// <summary>Abandoned/Expired taken job → red Cancelled for <see cref="DurationSeconds"/>.</summary>
public struct CancelledFlashState
{
    public float Until;
    public string JobId;
}

public static class CancelledFlash
{
    public const float DurationSeconds = 8f;

    public static void Note(ref CancelledFlashState state, string? jobId, float now)
    {
        state.JobId = jobId ?? string.Empty;
        state.Until = now + DurationSeconds;
    }

    public static void Clear(ref CancelledFlashState state)
    {
        state.Until = -1f;
        state.JobId = string.Empty;
    }

    public static bool TryConsume(
        ref CancelledFlashState state,
        float now,
        bool liveTakenNotCancelled,
        out string? jobId)
    {
        jobId = null;
        if (state.Until < 0f || now > state.Until)
        {
            Clear(ref state);
            return false;
        }

        if (liveTakenNotCancelled)
        {
            Clear(ref state);
            return false;
        }

        jobId = string.IsNullOrEmpty(state.JobId) ? null : state.JobId;
        return true;
    }
}
