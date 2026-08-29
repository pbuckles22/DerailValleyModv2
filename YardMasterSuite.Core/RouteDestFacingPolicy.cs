namespace YardMasterSuite.Core;

/// <summary>
/// Dest Set word at bind, not origin crow-flies.
/// Smoke: B4L dest is behind at Set dest, but reverse through the pin
/// leaves dest ahead after the frog (Set Forward into TT).
/// </summary>
public static class RouteDestFacingPolicy
{
    public static bool DestNeedsReverse(bool pinNeedsReverse, bool destCrowFliesBehind) =>
        pinNeedsReverse ? false : destCrowFliesBehind;
}
