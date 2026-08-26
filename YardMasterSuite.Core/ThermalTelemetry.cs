namespace YardMasterSuite.Core;

public struct ThermalCapLogCache
{
    public bool WasCapping;
    public ThreeGateAbortReason LastAbort;
    public int LastCapKind;
}

/// <summary>
/// Discrete Player.log lines for <b>7.2</b> thermal soft-cap. Change-only; interned.
/// </summary>
public static class ThermalTelemetry
{
    public const string SoftCapWarning = "T2 thermal: soft-cap → 0.75 (Warning)";
    public const string SoftCapCritical = "T2 thermal: soft-cap → 0.55 (Critical)";
    public const string SoftCapHot = "T2 thermal: soft-cap → 0.55 (Hot)";
    public const string CapRelease = "T2 thermal: cap release";
    public const string AbortIntegrity = "T2 thermal: abort Integrity";
    public const string AbortStateRegistry = "T2 thermal: abort StateRegistry";
    public const string AbortSafety = "T2 thermal: abort Safety";
    public const string AbortSoftWrite = "T2 thermal: abort SoftWrite";

    public const int KindNone = 0;
    public const int KindWarning = 1;
    public const int KindCritical = 2;
    public const int KindHot = 3;

    public static int CapKind(MotorCabTempBand? band) =>
        band switch
        {
            MotorCabTempBand.Warning => KindWarning,
            MotorCabTempBand.Critical => KindCritical,
            MotorCabTempBand.WarningAndCritical => KindCritical,
            _ => KindHot,
        };

    public static string LineForKind(int capKind) =>
        capKind switch
        {
            KindWarning => SoftCapWarning,
            KindCritical => SoftCapCritical,
            _ => SoftCapHot,
        };

    public static string? NextLog(
        bool applied,
        ThreeGateAbortReason abort,
        int capKind,
        ref ThermalCapLogCache cache)
    {
        if (applied)
        {
            if (cache.WasCapping && cache.LastCapKind == capKind)
            {
                return null;
            }

            cache.WasCapping = true;
            cache.LastAbort = ThreeGateAbortReason.None;
            cache.LastCapKind = capKind;
            return LineForKind(capKind);
        }

        if (!cache.WasCapping && abort == cache.LastAbort)
        {
            return null;
        }

        if (cache.WasCapping)
        {
            cache.WasCapping = false;
            cache.LastCapKind = KindNone;
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
            ThreeGateAbortReason.Safety => AbortSafety,
            ThreeGateAbortReason.SoftWrite => AbortSoftWrite,
            _ => AbortIntegrity,
        };
}
