namespace YardMasterSuite.Core;

/// <summary>
/// Next / GO still advance the Switch List row. Align, reverser, and TT wait
/// until the consist is stopped so a facing flip cannot blow the motors.
/// </summary>
public static class SwitchListAutoPrepHold
{
    public static bool Pending { get; private set; }

    public static string Reason { get; private set; } = "";

    public static bool ShouldHold(float absSpeedKmh) =>
        !SwitchListStepPrereq.AllowsAutoPrep(absSpeedKmh);

    /// <summary>
    /// Rolling → arm a pending prep and skip writes. Stopped → claim the apply
    /// (clears pending) so the caller can Align / facing-prep now.
    /// </summary>
    public static bool TryClaimApply(string reason, float absSpeedKmh)
    {
        if (ShouldHold(absSpeedKmh))
        {
            Pending = true;
            Reason = string.IsNullOrEmpty(reason) ? "prep" : reason;
            return false;
        }

        Pending = false;
        Reason = "";
        return true;
    }

    public static void Clear()
    {
        Pending = false;
        Reason = "";
    }
}
