namespace YardMasterSuite.Core;

/// <summary>
/// Cab drive gold is <c>feature=0</c>. Look-at SphereCast from the hood
/// hits yard cars and rebuilds the HUD bar (smoke 2.8.7.25 reverse → CLEARED).
/// On-foot / car-roof still SphereCast. Boarded loco also hides the look-at bar
/// so standing-on-loco does not rebuild it at 10 Hz.
/// </summary>
public static class CabLookAtPolicy
{
    public static bool SkipLookAtCast(bool boardedLoco) => boardedLoco;

    public static bool HideLookAtBar(bool boardedLoco) => boardedLoco;
}
