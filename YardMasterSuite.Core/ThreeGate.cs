using System;

namespace YardMasterSuite.Core;

public enum ThreeGateAbortReason
{
    None = 0,
    Integrity,
    StateRegistry,
    Safety,
    SoftWrite,
}

public readonly struct ThreeGateResult
{
    public ThreeGateResult(bool applied, ThreeGateAbortReason abortReason)
    {
        Applied = applied;
        AbortReason = abortReason;
    }

    public bool Applied { get; }
    public ThreeGateAbortReason AbortReason { get; }

    public static ThreeGateResult Ok() => new(true, ThreeGateAbortReason.None);

    public static ThreeGateResult Abort(ThreeGateAbortReason reason) =>
        new(false, reason);
}

/// <summary>
/// Integrity → State Registry → Safety → Soft Write. Fail closed.
/// </summary>
public static class ThreeGate
{
    public static ThreeGateResult TryApply(
        bool integrityOk,
        bool stateRegistryOk,
        bool safetyOk,
        Func<bool> softWrite)
    {
        if (softWrite is null)
        {
            throw new ArgumentNullException(nameof(softWrite));
        }

        if (!integrityOk)
        {
            return ThreeGateResult.Abort(ThreeGateAbortReason.Integrity);
        }

        if (!stateRegistryOk)
        {
            return ThreeGateResult.Abort(ThreeGateAbortReason.StateRegistry);
        }

        if (!safetyOk)
        {
            return ThreeGateResult.Abort(ThreeGateAbortReason.Safety);
        }

        try
        {
            return softWrite()
                ? ThreeGateResult.Ok()
                : ThreeGateResult.Abort(ThreeGateAbortReason.SoftWrite);
        }
        catch (Exception)
        {
            return ThreeGateResult.Abort(ThreeGateAbortReason.SoftWrite);
        }
    }
}
