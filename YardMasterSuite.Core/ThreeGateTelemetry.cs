namespace YardMasterSuite.Core;

public struct ThreeGateLogCache
{
    public bool Seeded;
    public bool LastApplied;
    public ThreeGateAbortReason LastReason;
    public string? LastWriteId;
}

/// <summary>
/// Discrete Player.log lines for Three-Gate (7.1). Apply on a press; abort is change-only.
/// </summary>
public static class ThreeGateTelemetry
{
    public const string WriteReverser = "reverser";
    public const string WriteTmFuse = "tm-fuse";

    private const string ApplyReverser = "T2 three-gate: apply write=reverser";
    private const string ApplyTmFuse = "T2 three-gate: apply write=tm-fuse";
    private const string AbortIntegrityReverser = "T2 three-gate: abort Integrity write=reverser";
    private const string AbortStateReverser = "T2 three-gate: abort StateRegistry write=reverser";
    private const string AbortSafetyReverser = "T2 three-gate: abort Safety write=reverser";
    private const string AbortSoftReverser = "T2 three-gate: abort SoftWrite write=reverser";
    private const string AbortIntegrityTm = "T2 three-gate: abort Integrity write=tm-fuse";
    private const string AbortStateTm = "T2 three-gate: abort StateRegistry write=tm-fuse";
    private const string AbortSafetyTm = "T2 three-gate: abort Safety write=tm-fuse";
    private const string AbortSoftTm = "T2 three-gate: abort SoftWrite write=tm-fuse";

    public static string? NextLog(
        ThreeGateResult result,
        string writeId,
        bool logApply,
        ref ThreeGateLogCache cache)
    {
        if (result.Applied)
        {
            cache.Seeded = true;
            cache.LastApplied = true;
            cache.LastReason = ThreeGateAbortReason.None;
            cache.LastWriteId = writeId;
            return logApply ? LineApply(writeId) : null;
        }

        var sameAbort = cache.Seeded
            && !cache.LastApplied
            && cache.LastReason == result.AbortReason
            && cache.LastWriteId == writeId;
        cache.Seeded = true;
        cache.LastApplied = false;
        cache.LastReason = result.AbortReason;
        cache.LastWriteId = writeId;
        return sameAbort ? null : LineAbort(result.AbortReason, writeId);
    }

    private static string LineApply(string writeId)
    {
        if (writeId == WriteTmFuse)
        {
            return ApplyTmFuse;
        }

        return ApplyReverser;
    }

    private static string LineAbort(ThreeGateAbortReason reason, string writeId)
    {
        var tm = writeId == WriteTmFuse;
        switch (reason)
        {
            case ThreeGateAbortReason.StateRegistry:
                return tm ? AbortStateTm : AbortStateReverser;
            case ThreeGateAbortReason.Safety:
                return tm ? AbortSafetyTm : AbortSafetyReverser;
            case ThreeGateAbortReason.SoftWrite:
                return tm ? AbortSoftTm : AbortSoftReverser;
            default:
                return tm ? AbortIntegrityTm : AbortIntegrityReverser;
        }
    }
}
