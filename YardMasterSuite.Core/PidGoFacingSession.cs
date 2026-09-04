namespace YardMasterSuite.Core;

/// <summary>
/// Latch drive facing for the active GO leg so CLEARED↔AtSwitch chatter
/// cannot flip the reverser mid-run (**13.4** TM blow).
/// </summary>
public static class PidGoFacingSession
{
    public static bool Active { get; private set; }

    public static bool NeedsReverse { get; private set; }

    public static void Latch(bool needsReverse)
    {
        Active = true;
        NeedsReverse = needsReverse;
    }

    public static void Clear()
    {
        Active = false;
        NeedsReverse = false;
    }

    public static bool Resolve(bool liveNeedsReverse)
    {
        if (!Active)
        {
            return liveNeedsReverse;
        }

        return NeedsReverse;
    }
}
