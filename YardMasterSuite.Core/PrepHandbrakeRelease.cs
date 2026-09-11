namespace YardMasterSuite.Core;

/// <summary>
/// Prep couple: drop parking brakes on the joined cut so the pull-out
/// is not dragging a set with a handbrake on (cab 2.13.2.5.16).
/// Delivery drop (15.2) does not use this path.
/// </summary>
public static class PrepHandbrakeRelease
{
    public const float ReleasedPosition = 0f;

    public static bool ShouldReleaseOnCoupleSuccess(
        SwitchListStepKind? kind,
        bool coupleSuccess) =>
        coupleSuccess && kind == SwitchListStepKind.Prep;

    public static string? FormatLog(int releasedCount) =>
        releasedCount <= 0 ? null : SwitchListRunnerTelemetry.PrepHandbrakeRelease + releasedCount;
}
