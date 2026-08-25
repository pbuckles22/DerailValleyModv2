namespace YardMasterSuite.Core;

/// <summary>
/// Vanilla <c>NotchedPortIncrementalInput</c> notches on
/// <c>GetAnyDirButtonDown</c>. Analog/look chatter can fire Down every
/// frame and walk throttle, indy, and train brake (2.6.21.3 cab smoke).
/// One notch on the rising edge only.
/// </summary>
public static class IncrementalChatterGate
{
    public static bool ShouldApplyNotch(bool buttonDown, bool wasHeld) =>
        buttonDown && !wasHeld;
}
