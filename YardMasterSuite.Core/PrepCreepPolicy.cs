namespace YardMasterSuite.Core;

/// <summary>
/// Prep: 25-kiss dump, then <see cref="CreepRequestKmh"/> in
/// <see cref="SafetyZoneMeters"/> until touch / knuckle (band-aid until MPC).
/// </summary>
public static class PrepCreepPolicy
{
    /// <summary>
    /// Engineer reverse creep (km/h). Distinct from
    /// <see cref="PidSpeedHold.DepartureCrawlKmh"/> (2 km/h gear-wait).
    /// </summary>
    public const float CreepRequestKmh = 3f;

    /// <summary>
    /// Cab 22.46: PID trim from rem=30 left HUD ~19 at rem=1. Kiss at 25 km/h
    /// (mass d_stop + slack) then creep only in this leftover band until MPC.
    /// </summary>
    public const float SafetyZoneMeters = 10f;

    public static bool InSafetyZone(float? remMeters) =>
        remMeters is float r
        && !float.IsNaN(r)
        && r >= 0f
        && r <= SafetyZoneMeters;

    /// <summary>
    /// After 25-kiss dump, leftover rem in the safety zone at crawl → Arm GO at 3.
    /// Green window stays Stop GO. Cab 2.16.11 shoved an SL-52 cut because
    /// creep kept powering inside that window.
    /// </summary>
    public static bool ShouldArmCreepInSafetyZone(
        float? remMeters,
        float speedKmh,
        bool tipCoupled = false,
        bool holdAfterCouple = false)
    {
        if (tipCoupled || holdAfterCouple)
        {
            return false;
        }

        if (!InSafetyZone(remMeters) || remMeters is not float rem)
        {
            return false;
        }

        if (PrepCoupleExitGate.InTouchWindow(rem))
        {
            return false;
        }

        var speed = speedKmh < 0f || float.IsNaN(speedKmh) ? 0f : speedKmh;
        return speed <= CreepRequestKmh + 3f;
    }

    /// <summary>
    /// Stopped short of the knuckle, outside the green window: drop the hold
    /// so the 3 km/h creep can finish. Inside the window, or after a refused
    /// partner, stay stopped so we do not shove the cut.
    /// </summary>
    public static bool ShouldDropHoldForShortStop(
        bool holdAfterCouple,
        bool tipCoupled,
        float speedKmh,
        float? clearanceMeters,
        bool partnerRefused = false)
    {
        if (partnerRefused || !holdAfterCouple || tipCoupled || !PidGoStop.IsFullyStopped(speedKmh))
        {
            return false;
        }

        if (clearanceMeters is not float gap
            || float.IsNaN(gap)
            || float.IsInfinity(gap))
        {
            return false;
        }

        return gap > AutoCoupleAssist.MaxCoupleClearanceMeters
            && gap <= SafetyZoneMeters;
    }

    /// <summary>
    /// Cab 22.45: indy 27% left HUD at 24 until go-stop. Train 0.50 is the
    /// catch-down, not a Stop GO dump.
    /// </summary>
    public const float CatchDownTrain = 0.50f;

    public static bool IsCreepRequest(float requestKmh) =>
        requestKmh > 0f
        && !float.IsNaN(requestKmh)
        && requestKmh <= CreepRequestKmh + 0.05f;

    /// <summary>
    /// Above the 40 t floor. Cab 2.16.21: 38 t only twitched after the
    /// knuckle; 86 t ran to 9 km/h while the train brake was still empty.
    /// </summary>
    public static bool IsHeavyApproach(float massTonnes) =>
        !float.IsNaN(massTonnes)
        && !float.IsInfinity(massTonnes)
        && massTonnes > YardStopKinematics.ReferenceMassTonnes;

    /// <summary>
    /// Joined mass appears at the knuckle. Latch on the walk, outside that frame.
    /// </summary>
    public const float HeavyKnuckleLatchRemMeters = 2f;

    public static bool ShouldLatchHeavyKnuckle(
        bool coupleHold,
        bool tipCoupled,
        float? remMeters,
        float requestKmh,
        float massTonnes)
    {
        if (coupleHold || tipCoupled || !IsCreepRequest(requestKmh) || !IsHeavyApproach(massTonnes))
        {
            return false;
        }

        return remMeters is float rem
            && !float.IsNaN(rem)
            && !float.IsInfinity(rem)
            && rem > HeavyKnuckleLatchRemMeters;
    }

    public static bool ShouldSnapTrainOnHeavyKnuckle(bool coupleHold, bool heavyApproachLatched) =>
        coupleHold && heavyApproachLatched;

    public static bool WantsCatchDown(float speedKmh, float requestKmh)
    {
        if (!IsCreepRequest(requestKmh))
        {
            return false;
        }

        var speed = speedKmh < 0f || float.IsNaN(speedKmh) ? 0f : speedKmh;
        return speed > requestKmh + PidSpeedHold.OverspeedBandKmh;
    }

    public static bool WantsCreepCap(SwitchListStep? step) =>
        step != null && step.Kind == SwitchListStepKind.Prep;

    public static bool IsGreenClearance(float? clearanceMeters, bool partnerInCoupleRange) =>
        partnerInCoupleRange && AutoCoupleAssist.ClearanceAllowsCouple(clearanceMeters);

    public static bool ShouldStopGoForCouple(
        SwitchListRunMode mode,
        SwitchListStep? step,
        float? clearanceMeters,
        float speedKmh,
        bool mechanicallyCoupled,
        bool spurPickupComplete = false)
    {
        _ = speedKmh;
        return PrepCoupleExitGate.ShouldStopGoOnPrepApproach(
            mode,
            step,
            mechanicallyCoupled,
            spurPickupComplete,
            PrepSpurPickupSession.UnattachedOnPrepSpur,
            clearanceMeters);
    }
}
