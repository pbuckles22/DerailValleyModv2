namespace YardMasterSuite.Core;

/// <summary>**13.2.2** at-spur latch for the current Prep row (not Switch List Next).</summary>
public static class PrepTrackArrivalSession
{
    public static bool AtSpur { get; private set; }

    /// <summary>Rising edge AtTrack → true once. Ambiguous never latches. OffTrack drops.</summary>
    public static bool TryArrive(PrepTrackArrival arrival)
    {
        if (arrival == PrepTrackArrival.OffTrack)
        {
            AtSpur = false;
            return false;
        }

        if (!PrepTrackArrivalGate.ShouldAdvanceToAtSpur(arrival))
        {
            return false;
        }

        if (AtSpur)
        {
            return false;
        }

        AtSpur = true;
        return true;
    }

    public static void Clear() => AtSpur = false;
}
