namespace YardMasterSuite.Core;

/// <summary>
/// Prep is done when tagged cars for this dest are on the consist:
/// job tag, then spur, then count, then each car verifies to the tag.
/// </summary>
public static class PrepSpurPickup
{
    /// <summary>
    /// Occupancy fallback when paperwork start-track quota is unknown (0).
    /// </summary>
    public static bool IsComplete(int attachedThisJobCars, int remainingThisJobOnThisPrep) =>
        IsComplete(
            expectedThisSpurJobCars: 0,
            attachedThisSpurJobCars: attachedThisJobCars,
            remainingThisJobOnThisPrep: remainingThisJobOnThisPrep);

    public static bool IsComplete(
        int expectedThisSpurJobCars,
        int attachedThisSpurJobCars,
        int remainingThisJobOnThisPrep)
    {
        if (expectedThisSpurJobCars > 0)
        {
            return attachedThisSpurJobCars >= expectedThisSpurJobCars
                && remainingThisJobOnThisPrep <= 0;
        }

        return attachedThisSpurJobCars > 0 && remainingThisJobOnThisPrep <= 0;
    }

    public static bool CountsAsRemainingThisPrep(
        bool isThisJobsCar,
        bool standingOnThisPrepSpur,
        bool alreadyAttached,
        bool assignedToThisSpur = true,
        bool tagVerified = true) =>
        tagVerified
        && isThisJobsCar
        && assignedToThisSpur
        && standingOnThisPrepSpur
        && !alreadyAttached;

    public static bool TagMatches(string? carJobTag, string? listJobId) =>
        RemoteTakeGate.ListJobMatches(carJobTag, listJobId);

    /// <summary>Painted job number on the car must equal the switch-list job.</summary>
    public static bool CarVerifiesToTag(string? carJobTag, string? listJobId) =>
        TagMatches(carJobTag, listJobId);

    public static bool CountsAsThisSpurNeed(
        bool tagVerified,
        bool standingOnThisPrepSpur,
        bool attached,
        bool taskStartsOnThisPrep) =>
        tagVerified && (standingOnThisPrepSpur || (attached && taskStartsOnThisPrep));

    public static bool CountsAsThisSpurHave(
        bool tagVerified,
        bool attached,
        bool standingOnThisPrepSpur,
        bool taskStartsOnThisPrep) =>
        tagVerified && attached && (standingOnThisPrepSpur || taskStartsOnThisPrep);

    public static bool TrackIsPrepSpur(string? carTrack, string? prepDest)
    {
        var a = JobCarMarkerDisplay.ShortSpurLabel(carTrack);
        var b = JobCarMarkerDisplay.ShortSpurLabel(prepDest);
        return !string.IsNullOrEmpty(a)
            && !string.IsNullOrEmpty(b)
            && string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
    }
}

public static class PrepSpurPickupSession
{
    public static int ExpectedThisSpurJobCars { get; private set; }

    public static int AttachedJobCars { get; private set; }

    public static int UnattachedOnPrepSpur { get; private set; }

    public static bool IsComplete =>
        PrepSpurPickup.IsComplete(
            ExpectedThisSpurJobCars,
            AttachedJobCars,
            UnattachedOnPrepSpur);

    public static void Observe(int attachedJobCars, int unattachedOnPrepSpur) =>
        Observe(
            expectedThisSpurJobCars: 0,
            attachedThisSpurJobCars: attachedJobCars,
            unattachedOnPrepSpur: unattachedOnPrepSpur);

    public static void Observe(
        int expectedThisSpurJobCars,
        int attachedThisSpurJobCars,
        int unattachedOnPrepSpur)
    {
        ExpectedThisSpurJobCars = expectedThisSpurJobCars < 0 ? 0 : expectedThisSpurJobCars;
        AttachedJobCars = attachedThisSpurJobCars < 0 ? 0 : attachedThisSpurJobCars;
        UnattachedOnPrepSpur = unattachedOnPrepSpur < 0 ? 0 : unattachedOnPrepSpur;
    }

    public static void Clear()
    {
        ExpectedThisSpurJobCars = 0;
        AttachedJobCars = 0;
        UnattachedOnPrepSpur = 0;
    }
}
