namespace YardMasterSuite.Core;

/// <summary>
/// Which cab Set word to show: toward the pin before CLEARED, toward dest after.
/// </summary>
public static class RouteFacingPhasePolicy
{
    public static bool FacingNeedsReverse(
        RouteClearancePhase phase,
        bool pinArmedForClearance,
        bool pinLatched,
        bool pinTravelReverse,
        bool pinBehindLive,
        bool destBehindLive)
    {
        var cleared = phase == RouteClearancePhase.Cleared;
        if (cleared || !pinArmedForClearance)
        {
            var pinNeedsReverse = pinLatched ? pinTravelReverse : pinBehindLive;
            return RouteDestFacingPolicy.DestNeedsReverse(pinNeedsReverse, destBehindLive);
        }

        return pinLatched ? pinTravelReverse : pinBehindLive;
    }
}
