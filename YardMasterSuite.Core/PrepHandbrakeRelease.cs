namespace YardMasterSuite.Core;

/// <summary>
/// Prep couple: drop parking brakes on the joined cut so the pull-out
/// is not dragging a set with a handbrake on (cab 2.13.2.5.16).
/// Delivery drop (15.2) does not use this path.
/// Cab 2.13.2.5.22.16: physical couple after Next off Prep still releases
/// (list was already Transit; AutoCouple Done never fired).
/// </summary>
public static class PrepHandbrakeRelease
{
    public const float ReleasedPosition = 0f;

    /// <summary>Ignore cargo wobble; a car join is tens of tonnes.</summary>
    public const int MinTonnesJoin = 10;

    public static bool ShouldReleaseOnCoupleSuccess(
        SwitchListStepKind? kind,
        bool coupleSuccess)
    {
        if (!coupleSuccess || !kind.HasValue)
        {
            return false;
        }

        return kind.Value != SwitchListStepKind.Delivery;
    }

    public static bool ShouldReleaseOnTonnesJoin(
        int previousTonnes,
        int currentTonnes,
        SwitchListStepKind? kind) =>
        ShouldReleaseOnCoupleSuccess(
            kind,
            previousTonnes >= 1 && currentTonnes >= previousTonnes + MinTonnesJoin);

    public static string? FormatLog(int releasedCount) =>
        releasedCount <= 0 ? null : SwitchListRunnerTelemetry.PrepHandbrakeRelease + releasedCount;
}
