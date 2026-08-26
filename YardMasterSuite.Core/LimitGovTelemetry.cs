namespace YardMasterSuite.Core;

public struct LimitGovLogCache
{
    public bool WasCapping;
    public ThreeGateAbortReason LastAbort;
    public int LastLimitRounded;
}

/// <summary>
/// Discrete Player.log lines for <b>7.5</b> Limit auto-throttle. Change-only; interned.
/// </summary>
public static class LimitGovTelemetry
{
    public const string SoftCap = "T2 limit-gov: soft-cap";
    public const string CapRelease = "T2 limit-gov: cap release";
    public const string AbortIntegrity = "T2 limit-gov: abort Integrity";
    public const string AbortStateRegistry = "T2 limit-gov: abort StateRegistry";
    public const string AbortSoftWrite = "T2 limit-gov: abort SoftWrite";

    public static string? NextLog(
        bool applied,
        ThreeGateAbortReason abort,
        int limitRounded,
        ref LimitGovLogCache cache)
    {
        if (applied)
        {
            if (cache.WasCapping && cache.LastLimitRounded == limitRounded)
            {
                return null;
            }

            cache.WasCapping = true;
            cache.LastAbort = ThreeGateAbortReason.None;
            cache.LastLimitRounded = limitRounded;
            return SoftCap;
        }

        if (!cache.WasCapping && abort == cache.LastAbort)
        {
            return null;
        }

        if (cache.WasCapping)
        {
            cache.WasCapping = false;
            cache.LastLimitRounded = 0;
            cache.LastAbort = abort;
            return abort is ThreeGateAbortReason.None or ThreeGateAbortReason.Safety
                ? CapRelease
                : LineAbort(abort);
        }

        cache.LastAbort = abort;
        return null;
    }

    private static string LineAbort(ThreeGateAbortReason abort) =>
        abort switch
        {
            ThreeGateAbortReason.StateRegistry => AbortStateRegistry,
            ThreeGateAbortReason.SoftWrite => AbortSoftWrite,
            _ => AbortIntegrity,
        };
}
