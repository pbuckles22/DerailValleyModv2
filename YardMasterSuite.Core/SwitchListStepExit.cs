namespace YardMasterSuite.Core;

/// <summary>
/// Switch List auto-next waits for this row's exit. Manual Next (desk /
/// Ctrl+Left) still bypasses. Exit of row N is entrance of row N+1.
/// <list type="bullet">
/// <item>Past-switch / pin Transit: CLEARED (after At switch).</item>
/// <item>Prep: this spur's job cars knuckled (not leftover cars from a
/// prior Prep). Handbrake drop runs on that couple, then auto-next.</item>
/// </list>
/// </summary>
public static class SwitchListStepExit
{
    /// <summary>
    /// Prep auto-next only when <paramref name="attachedOnThisSpur"/> cars from
    /// <em>this</em> dest are on the hook and none remain unattached on that
    /// spur. All-job attached count must not be used (B1S leftover ≠ C4S).
    /// </summary>
    public static bool PrepKnuckleComplete(int attachedOnThisSpur, int unattachedOnThisSpur) =>
        PrepSpurPickup.IsComplete(attachedOnThisSpur, unattachedOnThisSpur);

    public static bool AllowsAutoNextPrep(
        SwitchListStepKind? kind,
        bool hasNextStep,
        bool pickupComplete) =>
        kind == SwitchListStepKind.Prep && hasNextStep && pickupComplete;
}
