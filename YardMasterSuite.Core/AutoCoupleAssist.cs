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

    /// <summary>Refuse TryCouple at yard-slam speeds (daisy-chain after a snap).</summary>
    public const float MaxCoupleSpeedKmh = 8f;

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
