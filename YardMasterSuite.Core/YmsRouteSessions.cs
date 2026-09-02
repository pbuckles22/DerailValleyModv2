namespace YardMasterSuite.Core;

/// <summary>
/// Wipe Maps / Switch List / pin drive state. Static sessions survive UMM
/// across save load unless cleared on world leave — smoke: second load armed
/// PID and drove the wrong way without Set dest.
/// </summary>
public static class YmsRouteSessions
{
    public static void ClearAll()
    {
        RouteDestSession.Clear();
        SwitchListSession.Clear();
        RoutePlanSession.Clear();
        RoutePinLatch.Clear();
        RouteClearanceSession.Clear();
        PathCheckSession.Clear();
        PidCruiseSession.Reset();
        SwitchListRunnerSession.Clear();
    }
}
