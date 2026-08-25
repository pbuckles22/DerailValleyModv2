namespace YardMasterSuite.Core;

/// <summary>Why job-car AR may change inventory identity (pickup/swap/drop).</summary>
public enum JobCarArScanReason
{
    /// <summary>Same held job — do not re-bind inventory; live cars still poll.</summary>
    Keep = 0,

    /// <summary>Picked up or swapped paperwork — resolve pins once.</summary>
    Scan = 1,

    /// <summary>Dropped / no job in hand — clear AR.</summary>
    Clear = 2,
}

/// <summary>
/// Inventory identity is event-like: scan on pickup/swap, clear on drop.
/// Live car positions and GO-hide still rebuild on the Unity 0.25 s poll while Keep.
/// </summary>
public static class JobCarArScanPolicy
{
    public static JobCarArScanReason Decide(string? lastScannedJobId, string? currentHeldJobId)
    {
        var held = Normalize(currentHeldJobId);
        var last = Normalize(lastScannedJobId);

        if (held == null)
        {
            return last == null ? JobCarArScanReason.Keep : JobCarArScanReason.Clear;
        }

        if (string.Equals(last, held, System.StringComparison.Ordinal))
        {
            return JobCarArScanReason.Keep;
        }

        return JobCarArScanReason.Scan;
    }

    private static string? Normalize(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return id!.Trim();
    }
}
