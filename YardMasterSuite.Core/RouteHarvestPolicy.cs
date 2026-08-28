namespace YardMasterSuite.Core;

/// <summary>Gather-once: corridor.txt is the Set dest snapshot, not Recheck last-write-wins.</summary>
public static class RouteHarvestPolicy
{
    public static bool ShouldWriteCorridor(string? computeReason) =>
        RoutePinLatch.IsSetDest(computeReason);
}
