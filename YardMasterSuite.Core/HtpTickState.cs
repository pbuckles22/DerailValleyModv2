namespace YardMasterSuite.Core;

/// <summary>
/// One Player.log sample for HTP replay. Built from
/// <c>T2 speed change</c> / <c>T2 controls</c> lines — not a physics tick.
/// </summary>
public readonly struct HtpTickState
{
    public HtpTickState(int speedKmh, float throttle, float independent)
    {
        SpeedKmh = speedKmh;
        Throttle = throttle;
        Independent = independent;
    }

    public int SpeedKmh { get; }

    public float Throttle { get; }

    public float Independent { get; }
}
