namespace YardMasterSuite.Core;

public struct AutoBrakeLogCache
{
    public bool WasApplying;
    public ThreeGateAbortReason LastAbort;
}

/// <summary>
/// Discrete Player.log lines for <b>7.3</b> auto-brake. Change-only; interned.
/// </summary>
public static class AutoBrakeTelemetry
{
    public const string Applying = "T2 autobrake: applying";
    public const string ApplyDone = "T2 autobrake: apply done";
    public const string AbortIntegrity = "T2 autobrake: abort Integrity";
    public const string AbortStateRegistry = "T2 autobrake: abort StateRegistry";
    public const string AbortSafety = "T2 autobrake: abort Safety";
    public const string AbortSoftWrite = "T2 autobrake: abort SoftWrite";

    public static string? NextLog(
        bool applying,
        bool sessionNeedsWork,
        ThreeGateAbortReason abort,
        ref AutoBrakeLogCache cache)
    {
        if (applying)
        {
            if (cache.WasApplying)
            {
                return null;
            }

            cache.WasApplying = true;
            cache.LastAbort = ThreeGateAbortReason.None;
            return Applying;
        }

        if (!cache.WasApplying)
        {
            return null;
        }

        cache.WasApplying = false;
        cache.LastAbort = abort;
        if (!sessionNeedsWork)
        {
            return ApplyDone;
        }

        return LineAbort(abort);
    }

    private static string LineAbort(ThreeGateAbortReason abort) =>
        abort switch
        {
            ThreeGateAbortReason.StateRegistry => AbortStateRegistry,
            ThreeGateAbortReason.Safety => AbortSafety,
            ThreeGateAbortReason.SoftWrite => AbortSoftWrite,
            _ => AbortIntegrity,
        };
}
