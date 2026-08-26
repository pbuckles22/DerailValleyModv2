namespace YardMasterSuite.Core;

public struct AutoCoupleLogCache
{
    public bool WasActive;
    public bool WasDone;
    public AutoCoupleAction LastAction;
    public ThreeGateAbortReason LastAbort;
}

/// <summary>
/// Discrete Player.log lines for <b>7.4</b> auto-coupler. Change-only; interned.
/// </summary>
public static class AutoCoupleTelemetry
{
    public const string Couple = "T2 autocouple: couple";
    public const string Finish = "T2 autocouple: finish";
    public const string Done = "T2 autocouple: done";
    public const string AbortIntegrity = "T2 autocouple: abort Integrity";
    public const string AbortStateRegistry = "T2 autocouple: abort StateRegistry";
    public const string AbortSafety = "T2 autocouple: abort Safety";
    public const string AbortSoftWrite = "T2 autocouple: abort SoftWrite";

    public static string? NextLog(
        bool applied,
        bool linkComplete,
        AutoCoupleAction action,
        ThreeGateAbortReason abort,
        ref AutoCoupleLogCache cache)
    {
        if (applied)
        {
            if (linkComplete)
            {
                if (cache.WasDone)
                {
                    return null;
                }

                cache.WasActive = false;
                cache.WasDone = true;
                cache.LastAction = action;
                cache.LastAbort = ThreeGateAbortReason.None;
                return Done;
            }

            if (cache.WasActive && cache.LastAction == action)
            {
                return null;
            }

            cache.WasActive = true;
            cache.WasDone = false;
            cache.LastAction = action;
            cache.LastAbort = ThreeGateAbortReason.None;
            return action == AutoCoupleAction.Finish ? Finish : Couple;
        }

        if (linkComplete)
        {
            if (cache.WasDone || !cache.WasActive)
            {
                return null;
            }

            cache.WasActive = false;
            cache.WasDone = true;
            cache.LastAbort = ThreeGateAbortReason.None;
            return Done;
        }

        if (cache.WasDone || !cache.WasActive)
        {
            return null;
        }

        cache.WasActive = false;
        cache.LastAbort = abort;
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
