namespace YardMasterSuite.Core;

/// <summary>
/// Yard GO with the prime mover off sits at full throttle and does not roll.
/// Crank while the reader says off. A missing reader is not "off".
/// </summary>
public static class EngineStartPolicy
{
    public const float CrankHold = 1f;

    public const float Released = 0f;

    public static bool ShouldCrank(bool driveActive, bool engineReaderPresent, bool engineOn) =>
        driveActive && engineReaderPresent && !engineOn;

    public static float StarterHold(bool crank) => crank ? CrankHold : Released;
}
