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
    /// Pin step uses the <b>latch</b> reverse bit before CLEARED (live
    /// pin-behind flips at the frog). After CLEARED, same dest facing as the
    /// desk (<see cref="RouteFacingPhasePolicy"/>) so GO does not thrash
    /// Forward while the list says Set Reverse.
    /// </summary>
    public static bool LegNeedsReverse(
        bool pinStepActive,
        bool pinStepReverse,
        bool destBehind) =>
        LegNeedsReverse(pinStepActive, pinStepReverse, destBehind, RouteClearancePhase.Idle);

    public static bool LegNeedsReverse(
        bool pinStepActive,
        bool pinStepReverse,
        bool destBehind,
        RouteClearancePhase clearancePhase)
    {
        if (pinStepActive)
        {
            return RouteFacingPhasePolicy.FacingNeedsReverse(
                clearancePhase,
                pinArmedForClearance: true,
                pinLatched: true,
                pinTravelReverse: pinStepReverse,
                pinBehindLive: pinStepReverse,
                destBehindLive: destBehind);
        }

        return destBehind;
    }

    public static bool PinStepActive(bool pinDisplayShown, bool sawtoothArmed, bool pinDismissed) =>
        pinDisplayShown || (sawtoothArmed && !pinDismissed);
}
