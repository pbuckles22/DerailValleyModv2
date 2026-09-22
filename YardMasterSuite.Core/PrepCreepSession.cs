namespace YardMasterSuite.Core;

/// <summary>
/// <b>13.2.4</b> live Prep creep sensors + sticky hold after couple Stop GO.
/// </summary>
public static class PrepCreepSession
{
    public static bool WantsCoupleStop { get; private set; }

    public static bool TipCoupled { get; private set; }

    /// <summary>
    /// Cab 22.50: kiss Stop GO parks reverser Neutral, so live tip aim
    /// drops. Keep the knuckle until the list leaves Prep.
    /// </summary>
    private static bool _knuckleLatched;

    /// <summary>After StopGoAtCouple — block yard-chain ArmGo until step advances / clear.</summary>
    public static bool HoldAfterCoupleStop { get; private set; }

    /// <summary>Approach was above 40 t. Couple-hold then snaps the train brake.</summary>
    public static bool HeavyKnuckle { get; private set; }

    /// <summary>
    /// Partner in the slide window is another job. Hold stays until the step
    /// changes so creep cannot shove that cut.
    /// </summary>
    public static bool PartnerRefused { get; private set; }

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
        TipClearanceMeters = clearanceMeters is float rem
            && !float.IsNaN(rem)
            && !float.IsInfinity(rem)
            && rem >= 0f
            ? rem
            : null;
        _ = speedKmh;
        if (mechanicallyCoupled)
        {
            _knuckleLatched = true;
        }

        TipCoupled = mechanicallyCoupled || _knuckleLatched;
        WantsCoupleStop = PrepCoupleExitGate.ShouldStopGoOnPrepApproach(
            SwitchListRunMode.Go,
            SwitchListSession.CurrentStep,
            mechanicallyCoupled,
            spurPickupComplete,
            PrepSpurPickupSession.UnattachedOnPrepSpur,
            TipClearanceMeters);

        if (mechanicallyCoupled || spurPickupComplete)
        {
            LatchCoupleHold();
        }
    }

    /// <summary>
    /// Stopped short of the knuckle: clear the couple hold so yard-chain can
    /// Arm GO at <see cref="PrepCreepPolicy.CreepRequestKmh"/> again.
    /// </summary>
    public static bool TryResumeAfterShortStop(float speedKmh)
    {
        if (!PrepCreepPolicy.ShouldDropHoldForShortStop(
                HoldAfterCoupleStop,
                TipCoupled,
                speedKmh,
                TipClearanceMeters,
                PartnerRefused))
        {
            return false;
        }

        ClearHold();
        return true;
    }

    /// <summary>
    /// Coupler tick may Stop GO immediately (do not wait for desk yard-chain poll).
    /// A Prep consist grow latches the hold before this call; that hold arms
    /// the stop when the laser has not set <see cref="WantsCoupleStop"/>.
    /// </summary>
    public static bool TryStopGoIfNeeded(SwitchListStep? step)
    {
        if (!(WantsCoupleStop || HoldAfterCoupleStop)
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

    public static void LatchHeavyKnuckle() => HeavyKnuckle = true;

    public static void LatchPartnerRefused()
    {
        PartnerRefused = true;
        HoldAfterCoupleStop = true;
    }

    public static void LatchKnuckle()
    {
        _knuckleLatched = true;
        TipCoupled = true;
    }

    public static void ClearHold() => HoldAfterCoupleStop = false;

    public static void Clear()
    {
        WantsCoupleStop = false;
        TipCoupled = false;
        _knuckleLatched = false;
        HoldAfterCoupleStop = false;
        HeavyKnuckle = false;
        PartnerRefused = false;
        TipClearanceMeters = null;
    }
}
