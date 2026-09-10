namespace YardMasterSuite.Core;

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
}
