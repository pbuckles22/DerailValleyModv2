namespace YardMasterSuite.Core;

/// <summary>
/// Set dest owns the 8.7 pin and reverse travel axis. Recheck must not steal
/// the pin. Live <c>IsPinBehind</c> must not flip the axis after you pass
/// (HTP latches reverse from the first pose; Unity used to re-read every poll).
/// </summary>
public static class RoutePinLatch
{
    private static string? _id;
    private static bool _reverse;

    public static bool HasLatch => !string.IsNullOrEmpty(_id);

    public static string? Id => _id;

    public static bool TravelUsesReverse => _reverse;

    public static void Clear()
    {
        _id = null;
        _reverse = false;
    }

    public static void Observe(string? computeReason, PathPlanResult? plan, bool pinIsBehind = false)
    {
        if (!IsSetDest(computeReason))
        {
            return;
        }

        var pin = SwitchListRouteLeg.PickPinJunctionId(plan);
        if (string.IsNullOrEmpty(pin))
        {
            return;
        }

        _id = pin;
        _reverse = pinIsBehind;
    }

    public static string? EffectivePin(PathPlanResult? livePlan)
    {
        if (!string.IsNullOrEmpty(_id))
        {
            return _id;
        }

        return SwitchListRouteLeg.PickPinJunctionId(livePlan);
    }

    public static bool EffectiveReverse(bool livePinIsBehind) =>
        HasLatch ? _reverse : livePinIsBehind;

    public static string? FormatLatchLog()
    {
        if (!HasLatch)
        {
            return null;
        }

        return "T2 route-pin: latch " + _id + " reverse=" + (_reverse ? "1" : "0");
    }

    public static bool IsSetDest(string? computeReason) =>
        string.Equals(computeReason, "set-dest", System.StringComparison.Ordinal);
}
