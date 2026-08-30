namespace YardMasterSuite.Core;

/// <summary>
/// Live cab→pin/dest gear for <b>9.1</b>. Do not default Forward on dest-set
/// before the pin latch / plan exists (smoke: dest → thr=100 F, then reverse=1).
/// </summary>
public static class PidSpeedFacing
{
    public static bool FacingReady(
        bool switchListActive,
        bool pinLatched,
        bool hasPlan) =>
        switchListActive || pinLatched || hasPlan;

    /// <summary>
    /// Pin step (display still up, or sawtooth not yet Next) uses live pin-behind.
    /// Dest step uses <see cref="RouteDestFacingPolicy"/> so pin-reverse ⇒ dest ahead.
    /// </summary>
    public static bool LegNeedsReverse(
        bool pinStepActive,
        bool pinBehind,
        bool destBehind)
    {
        if (pinStepActive)
        {
            return pinBehind;
        }

        return RouteDestFacingPolicy.DestNeedsReverse(pinBehind, destBehind);
    }

    public static bool PinStepActive(bool pinDisplayShown, bool sawtoothArmed, bool pinDismissed) =>
        pinDisplayShown || (sawtoothArmed && !pinDismissed);
}
