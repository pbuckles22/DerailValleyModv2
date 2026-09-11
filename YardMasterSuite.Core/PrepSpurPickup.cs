namespace YardMasterSuite.Core;

/// <summary>
/// Prep is done when this spur's job cars are on the consist — not when the
/// new consist tip is still an open knuckle (cab 2.13.2.5.22.2: GO kept
/// reversing after B1S hoppers were already hooked).
/// </summary>
public static class PrepSpurPickup
{
    public static bool IsComplete(int attachedJobCars, int unattachedOnPrepSpur) =>
        attachedJobCars > 0 && unattachedOnPrepSpur <= 0;

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
    public static int AttachedJobCars { get; private set; }

    public static int UnattachedOnPrepSpur { get; private set; }

    public static bool IsComplete =>
        PrepSpurPickup.IsComplete(AttachedJobCars, UnattachedOnPrepSpur);

    public static void Observe(int attachedJobCars, int unattachedOnPrepSpur)
    {
        AttachedJobCars = attachedJobCars < 0 ? 0 : attachedJobCars;
        UnattachedOnPrepSpur = unattachedOnPrepSpur < 0 ? 0 : unattachedOnPrepSpur;
    }

    public static void Clear()
    {
        AttachedJobCars = 0;
        UnattachedOnPrepSpur = 0;
    }
}
