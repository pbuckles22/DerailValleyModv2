namespace YardMasterSuite.Core;

/// <summary>Discrete couple-assist write for <b>7.4</b>.</summary>
public enum AutoCoupleAction
{
    None = 0,
    Couple = 1,
    Finish = 2,
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
