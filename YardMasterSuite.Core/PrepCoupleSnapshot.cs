namespace YardMasterSuite.Core;

/// <summary>
/// Type A payload: Prep reverse couple sensors. Raised on change only
/// (AutoCouplerListener). "Knuckle" = <see cref="MechanicallyCoupled"/>.
/// </summary>
public readonly struct PrepCoupleSnapshot
{
    public readonly bool MechanicallyCoupled;
    public readonly bool InTouchWindow;
    public readonly bool WantsCoupleStop;
    public readonly int RemDecimeters;

    public PrepCoupleSnapshot(
        bool mechanicallyCoupled,
        bool inTouchWindow,
        bool wantsCoupleStop,
        int remDecimeters)
    {
        MechanicallyCoupled = mechanicallyCoupled;
        InTouchWindow = inTouchWindow;
        WantsCoupleStop = wantsCoupleStop;
        RemDecimeters = remDecimeters;
    }

    public static int RemDecimetersFrom(float? clearanceMeters)
    {
        if (clearanceMeters is not float rem
            || float.IsNaN(rem)
            || float.IsInfinity(rem)
            || rem < 0f)
        {
            return int.MinValue;
        }

        return (int)System.Math.Round(rem * 10f);
    }
}
