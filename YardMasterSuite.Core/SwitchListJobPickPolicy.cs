namespace YardMasterSuite.Core;

/// <summary>
/// Desk Per-job picker. Auto-hold on world enter is
/// <see cref="SmokeJobHoldGate"/> (off). Available board jobs still belong
/// in the dropdown so Refresh is not empty.
/// </summary>
public static class SwitchListJobPickPolicy
{
    public const bool DeskListsAvailableJobs = true;

    public static bool ShouldAddAvailableBoardJobs(bool autoHoldEnabled)
    {
        _ = autoHoldEnabled;
        return DeskListsAvailableJobs;
    }
}
