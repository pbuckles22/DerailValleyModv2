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

    private static bool _dismissed;

    public static bool HasLatch => !string.IsNullOrEmpty(_id);

    public static string? Id => _id;

    public static bool TravelUsesReverse => _reverse;

    /// <summary>AR / desk pin while the past-switch step is active. Next dismisses.</summary>
    public static bool ShowPin => HasLatch && !_dismissed;

    public static bool DisplayDismissed => _dismissed;

    public static void Clear()
    {
        _id = null;
        _reverse = false;
        _dismissed = false;
    }

    /// <summary>
    /// Hide the frog pin after Next off past-switch. Latch stays so Recheck
    /// cannot steal 990152. Set dest arms a new pin.
    /// </summary>
    public static void DismissDisplay()
    {
        if (HasLatch)
        {
            _dismissed = true;
        }
    }

    /// <summary>
    /// Replace the latched frog without clearing travel axis ownership.
    /// Used when corridor-to-TT first-stop disagrees with past-switch approach.
    /// </summary>
    public static void Relatch(string? pinId, bool travelUsesReverse)
    {
        var id = pinId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        _id = id;
        _reverse = travelUsesReverse;
        _dismissed = false;
    }

    /// <summary>
    /// New Switch List bind owns a new pin lifecycle. Returns the dropped
    /// latch id (for T2) or null when nothing was showing/latched.
    /// </summary>
    public static string? ResetForNewSwitchList()
    {
        var id = _id;
        var had = HasLatch;
        Clear();
        return had ? id : null;
    }

    public static bool IsArmedForClearance(PathPlanResult? plan)
    {
        if (_dismissed)
        {
            return false;
        }

        if (HasLatch)
        {
            return true;
        }

        return SwitchListRouteLeg.ShouldArmPin(plan);
    }

    public static void Observe(
        string? computeReason,
        PathPlanResult? plan,
        bool pinIsBehind = false,
        System.Func<string, bool>? junctionAlreadyCleared = null)
    {
        if (!IsSetDest(computeReason))
        {
            return;
        }

        var pin = SwitchListRouteLeg.PickPinJunctionId(plan);

        // Past switch: dest-side last only when the first-stop is behind or
        // missing. Leave-TT extra pin is the ahead sawtooth (cab 2.13.2.5.8).
        if (SwitchListSession.CurrentStep != null)
        {
            var step = SwitchListSession.CurrentStep;
            if (SwitchListRunner.StepNeedsPinClearance(step.Kind)
                || step.BindNeedsReverse == true)
            {
                var observe = junctionAlreadyCleared != null
                    ? RouteStepDestPolicy.PickFirstUnspentJunctionId(plan, junctionAlreadyCleared)
                    : RouteStepDestPolicy.PickPastSwitchObservePin(plan, pinIsBehind);
                if (!string.IsNullOrEmpty(observe))
                {
                    pin = observe;
                }

                // set-dest on a new pin-leg: do not abort when inbound frogs
                // are spent. Recheck must still not steal; set-dest arms the
                // named switch (Gemini: atomic swap after Dismiss).
            }
        }

        if (string.IsNullOrEmpty(pin))
        {
            return;
        }

        _id = pin;
        _reverse = pinIsBehind;
        _dismissed = false;
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
