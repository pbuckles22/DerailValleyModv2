namespace YardMasterSuite.Core;

/// <summary>Discrete couple-assist write for <b>7.4</b>.</summary>
public enum AutoCoupleAction
{
    None = 0,
    Couple = 1,
    Finish = 2,
    Uncouple = 3,
}

/// <summary>
/// Fail-closed couple assist (no RCL remote). On-consist + travel aim + green
/// crawl window → mechanical couple; already coupled but incomplete → finish
/// hose / cocks / chain. Never CoupleTo across a gap (joint snap). MU is
/// best-effort in the same write, not a hold.
/// </summary>
public static class AutoCoupleAssist
{
    /// <summary>Match HUD green couple window — not vanilla 1.5 m scan.</summary>
    public const float MaxCoupleClearanceMeters = BackupProximityDisplay.GreenMaxDisplayMeters;

    /// <summary>
    /// Cab 2.16.10: HUD "0.5 m" rounds up to about 0.54, which missed the
    /// green window, and the knuckles were still open. Write the couple
    /// anywhere inside 1 m while the speed gate still blocks a yard slam.
    /// </summary>
    public const float SlideCoupleMeters = 1f;

    /// <summary>
    /// Cab 2.16.19: first Prep closed the knuckle at 4 km/h under a full dump.
    /// The write waits for the 3 km/h creep. Above that the brakes are still on.
    /// </summary>
    public const float MaxCoupleSpeedKmh = 3f;

    public static bool ActorOnConsist(bool playerOnCar, bool standingInSameTrainset) =>
        playerOnCar && standingInSameTrainset;

    public static bool HasTravelAim(ProximityTravelDirection direction) =>
        ProximityTravelDirectionGate.ShouldShowChip(direction);

    /// <summary>Tow link ready: mechanical + tight + hose + both cocks. MU is not required.</summary>
    public static bool LinkComplete(
        bool mechanicallyCoupled,
        bool tightened,
        bool airHoseConnected,
        bool cocksOpenBothSides) =>
        CouplingLink.IsUsableLink(
            mechanicallyCoupled,
            tightened,
            airHoseConnected,
            cocksOpenBothSides);

    public static bool ClearanceAllowsCouple(float? clearanceMeters)
    {
        if (clearanceMeters is null || float.IsNaN(clearanceMeters.Value))
        {
            return false;
        }

        var m = clearanceMeters.Value;
        return m >= 0f && m <= MaxCoupleClearanceMeters;
    }

    public static bool ClearanceAllowsSlideCouple(float? clearanceMeters)
    {
        if (clearanceMeters is null || float.IsNaN(clearanceMeters.Value))
        {
            return false;
        }

        var m = clearanceMeters.Value;
        return m >= 0f && m <= SlideCoupleMeters;
    }

    public static bool SpeedAllowsCouple(float speedKmh)
    {
        if (float.IsNaN(speedKmh) || speedKmh < 0f)
        {
            return false;
        }

        return speedKmh <= MaxCoupleSpeedKmh;
    }

    /// <summary>
    /// 7.4 during a Switch List is Prep only. Transit / Past-switch must not
    /// grab a foreign cut (cab cars=8).
    /// </summary>
    public static bool StepAllowsCoupleAssist(bool switchListActive, SwitchListStepKind? kind) =>
        !switchListActive || kind == SwitchListStepKind.Prep;

    /// <summary>
    /// Cab 22.58: bumper-to-bumper foreign cuts (no job tag / other job)
    /// must not stay on the consist. Locos are not job cars.
    /// </summary>
    public static bool PartnerIsTakenJobCar(string? takenJobId, string? partnerJobId)
    {
        var taken = takenJobId?.Trim();
        var partner = partnerJobId?.Trim();
        return !string.IsNullOrEmpty(taken)
            && !string.IsNullOrEmpty(partner)
            && string.Equals(taken, partner, System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool PartnerAllowsCouple(
        bool switchListActive,
        string? takenJobId,
        string? partnerJobId,
        bool partnerIsLoco)
    {
        if (!switchListActive || partnerIsLoco)
        {
            return true;
        }

        var taken = takenJobId?.Trim();
        if (string.IsNullOrEmpty(taken))
        {
            return true;
        }

        return PartnerIsTakenJobCar(taken, partnerJobId);
    }

    /// <summary>
    /// Cab 2.16.11: 3 km/h creep shoved an SL-52 cut after the knuckle
    /// window opened on a car this job must not take.
    /// </summary>
    public static bool ShouldHoldCreepForRefusedPartner(
        bool switchListActive,
        SwitchListStepKind? kind,
        bool mechanicallyCoupled,
        bool partnerInRange,
        bool partnerAllowsCouple,
        float? clearanceMeters) =>
        switchListActive
        && kind == SwitchListStepKind.Prep
        && !mechanicallyCoupled
        && partnerInRange
        && !partnerAllowsCouple
        && ClearanceAllowsSlideCouple(clearanceMeters);

    public static bool ShouldUncoupleForeignPartner(
        bool switchListActive,
        string? takenJobId,
        string? partnerJobId,
        bool mechanicallyCoupled,
        bool partnerIsLoco) =>
        switchListActive
        && mechanicallyCoupled
        && !partnerIsLoco
        && !string.IsNullOrEmpty(takenJobId?.Trim())
        && !PartnerIsTakenJobCar(takenJobId, partnerJobId);

    public static bool CarIsKeep(bool isLoco, string? takenJobId, string? carJobId) =>
        isLoco || PartnerIsTakenJobCar(takenJobId, carJobId);

    public static bool ShouldUncoupleKnuckle(bool keepA, bool keepB) => keepA != keepB;

    public static int CountForeignFreight(int totalCars, int locoCount, int jobCarCount)
    {
        if (totalCars < 0)
        {
            totalCars = 0;
        }

        var keep = (locoCount < 0 ? 0 : locoCount) + (jobCarCount < 0 ? 0 : jobCarCount);
        var foreign = totalCars - keep;
        return foreign < 0 ? 0 : foreign;
    }

    /// <summary>
    /// Foreign bumper cuts stay cuttable on Transit after a slam (cab C4S cars=8).
    /// Couple/Finish stay Prep-only.
    /// </summary>
    public static bool StepAllowsUncoupleAssist(bool switchListActive) => switchListActive;

    public static AutoCoupleAction Decide(
        bool hasTravelAim,
        bool hasTip,
        bool partnerInRange,
        bool mechanicallyCoupled,
        bool linkComplete,
        bool closeEnough,
        bool speedOk)
    {
        if (!hasTravelAim || !hasTip || linkComplete)
        {
            return AutoCoupleAction.None;
        }

        if (!mechanicallyCoupled)
        {
            return partnerInRange && closeEnough && speedOk
                ? AutoCoupleAction.Couple
                : AutoCoupleAction.None;
        }

        return AutoCoupleAction.Finish;
    }

    public static bool IsSafeToWrite(
        bool worldActive,
        bool actorOnConsist,
        bool tipPresent,
        bool preventCouple,
        bool overlayClear,
        AutoCoupleAction action) =>
        worldActive
        && actorOnConsist
        && tipPresent
        && !preventCouple
        && overlayClear
        && action != AutoCoupleAction.None;
}
