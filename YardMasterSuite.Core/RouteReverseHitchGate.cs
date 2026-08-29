namespace YardMasterSuite.Core;

/// <summary>
/// Cab gold is <c>feature=0</c>. Smoke 2.8.7.26 reverse-to-CLEARED with the
/// desk open scored <c>feature=52</c>. 2.8.7.27 skipped OnGUI until CLEARED
/// then auto-showed (<c>feature=24/26</c>). Force-close <c>_visible</c> on
/// Approaching/AtSwitch <em>while moving</em>; stay closed at CLEARED until
/// Ctrl+Insert. Standstill Set dest stays AtSwitch and must not close the desk
/// (smoke 2.8.7.28). Also skips backup overlap while quiet.
/// </summary>
public static class RouteReverseHitchGate
{
    public const float MovingKmh = 1f;

    public static bool ConsistIsMoving(float speedKmh) => speedKmh >= MovingKmh;

    public static bool QuietCabDuringPinReverse(
        bool boardedLoco,
        bool travelUsesReverse,
        RouteClearancePhase phase,
        bool consistMoving)
    {
        if (!boardedLoco || !travelUsesReverse || !consistMoving)
        {
            return false;
        }

        return phase == RouteClearancePhase.Approaching
            || phase == RouteClearancePhase.AtSwitch;
    }
}
