namespace YardMasterSuite.Core;

/// <summary>What a coupler is for the travel-direction approach.</summary>
public enum ApproachCouplerRole
{
    Ignore = 0,
    FreeTip = 1,
    ClosedKnuckle = 2,
}

/// <summary>
/// Which trainset car is the travel-leading end. Reverse: the rear
/// (max <c>indexInTrainset</c>) is the virtual nose — the butt is "front".
/// </summary>
public static class ConsistTravelLead
{
    public static int LeadingIndex(int minIndex, int maxIndex, bool travelReverse)
    {
        if (maxIndex < minIndex)
        {
            return minIndex;
        }

        return travelReverse ? maxIndex : minIndex;
    }

    /// <summary>
    /// Trainset index of the approach knuckle. Reverse / Rear = last car
    /// (consist butt), not the loco.
    /// </summary>
    public static int ApproachTipIndex(int carCount, bool useFront)
    {
        if (carCount <= 0)
        {
            return 0;
        }

        return LeadingIndex(0, carCount - 1, travelReverse: !useFront);
    }

    /// <summary>
    /// Travel-tip coupler. A free knuckle that faces the intent is the gap.
    /// A coupler that faces the other way is the already-coupled end (cab
    /// B1S reverse, <c>tenths=-1</c>). A couple to our own car is not the
    /// standing car. A couple to a foreign car on this axis is a shut knuckle.
    /// </summary>
    public static ApproachCouplerRole ClassifyApproachCoupler(
        float alignment,
        bool coupled,
        bool coupledToConsistMate,
        bool oppositeCoupled,
        bool alone)
    {
        if (alignment <= 0f || float.IsNaN(alignment))
        {
            return ApproachCouplerRole.Ignore;
        }

        if (coupled)
        {
            return coupledToConsistMate
                ? ApproachCouplerRole.Ignore
                : ApproachCouplerRole.ClosedKnuckle;
        }

        if (!alone && !oppositeCoupled)
        {
            return ApproachCouplerRole.Ignore;
        }

        return ApproachCouplerRole.FreeTip;
    }
}

/// <summary>
/// Best facing coupler this sample. A free tip wins over a shut knuckle.
/// Stack-only — the proximity poll must not allocate.
/// </summary>
public struct ApproachTipScore
{
    public float FreeAlign;
    public float ClosedAlign;
    private byte _flags;

    public bool HasFree => (_flags & 1) != 0;

    public bool KnuckleClosed => !HasFree && (_flags & 2) != 0;

    /// <summary>
    /// True when this coupler becomes the sample's tip.
    /// <paramref name="isFree"/> is false for a shut approach knuckle.
    /// </summary>
    public bool Consider(
        float alignment,
        bool coupled,
        bool coupledToConsistMate,
        bool oppositeCoupled,
        bool alone,
        out bool isFree)
    {
        isFree = false;
        var role = ConsistTravelLead.ClassifyApproachCoupler(
            alignment,
            coupled,
            coupledToConsistMate,
            oppositeCoupled,
            alone);
        if (role == ApproachCouplerRole.FreeTip)
        {
            if (HasFree && alignment <= FreeAlign)
            {
                return false;
            }

            FreeAlign = alignment;
            _flags |= 1;
            isFree = true;
            return true;
        }

        if (role != ApproachCouplerRole.ClosedKnuckle || HasFree)
        {
            return false;
        }

        if ((_flags & 2) != 0 && alignment <= ClosedAlign)
        {
            return false;
        }

        ClosedAlign = alignment;
        _flags |= 2;
        return true;
    }
}
