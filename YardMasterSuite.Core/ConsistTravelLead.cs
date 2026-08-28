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
}
