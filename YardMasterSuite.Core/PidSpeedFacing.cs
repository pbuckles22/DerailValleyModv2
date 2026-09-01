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
    /// Pin step uses the <b>latch</b> reverse bit, not live pin-behind (that
    /// flips at the frog and PID wrote F at 25). After the pin step, live dest
    /// behind — not 8.7 bind-time pin-reverse ⇒ dest ahead.
    /// </summary>
    public static bool LegNeedsReverse(
        bool pinStepActive,
        bool pinStepReverse,
        bool destBehind)
    {
        if (pinStepActive)
        {
            return pinStepReverse;
        }

        return destBehind;
    }

    public static bool PinStepActive(bool pinDisplayShown, bool sawtoothArmed, bool pinDismissed) =>
        pinDisplayShown || (sawtoothArmed && !pinDismissed);
}
