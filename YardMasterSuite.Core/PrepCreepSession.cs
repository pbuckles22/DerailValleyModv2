namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.2.4</b> live Prep creep sensors + sticky hold after couple Stop GO.
/// </summary>
public static class PrepCreepSession
{
    public static bool WantsCoupleStop { get; private set; }

    /// <summary>After StopGoAtCouple — block yard-chain ArmGo until step advances / clear.</summary>
    public static bool HoldAfterCoupleStop { get; private set; }

    /// <summary>Last tip clearance from the coupler tick — Prep laser rem.</summary>
    public static float? TipClearanceMeters { get; private set; }

    public static void Observe(float? clearanceMeters, float speedKmh, bool mechanicallyCoupled) =>
        Observe(clearanceMeters, speedKmh, mechanicallyCoupled, spurPickupComplete: false);

    public static void Observe(
        float? clearanceMeters,
        float speedKmh,
        bool mechanicallyCoupled,
        bool spurPickupComplete)
    {
        _ = speedKmh;
        TipClearanceMeters = clearanceMeters is float rem
            && !float.IsNaN(rem)
            && !float.IsInfinity(rem)
            && rem >= 0f
            ? rem
            : null;
        var hooked = mechanicallyCoupled || spurPickupComplete;
        WantsCoupleStop = hooked
            || (TipClearanceMeters is float tip
                && tip <= BackupProximityDisplay.CoupleNearRangeMeters);

        // Knuckle made / this spur's job cars on the hook — never re-arm
        // Prep GO (open tip after the set would shove into the next cut).
        if (hooked)
        {
            LatchCoupleHold();
        }
    }

    /// <summary>
    /// Coupler tick may Stop GO immediately (do not wait for desk yard-chain poll).
    /// </summary>
    public static bool TryStopGoIfNeeded(SwitchListStep? step)
    {
        if (!WantsCoupleStop
            || SwitchListRunnerSession.Mode != SwitchListRunMode.Go
            || step == null
            || step.Kind != SwitchListStepKind.Prep)
        {
            return false;
        }

        if (SwitchListRunnerSession.TryStopGo() != SwitchListRunnerResult.Ok)
        {
            return false;
        }

        LatchCoupleHold();
        return true;
    }

    public static void LatchCoupleHold() => HoldAfterCoupleStop = true;

    public static void ClearHold() => HoldAfterCoupleStop = false;

    public static void Clear()
    {
        WantsCoupleStop = false;
        HoldAfterCoupleStop = false;
        TipClearanceMeters = null;
    }
}
