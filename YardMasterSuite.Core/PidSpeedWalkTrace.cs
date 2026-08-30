namespace YardMasterSuite.Core;

/// <summary>
/// Change-only HTP walk sample. No strings. Tests format for
/// <c>ITestOutputHelper</c> (xUnit prints on fail). Per-tick lines do not
/// scale to hundreds of walks; this fires only when try/applied/speed/mode
/// change.
/// </summary>
public struct PidSpeedWalkTraceCache
{
    public int SpeedKmh;
    public int TryThrPct;
    public int TryIndyPct;
    public int AppliedThrPct;
    public int AppliedIndyPct;
    public PidSpeedMode Mode;
    public bool Seeded;
}

public static class PidSpeedWalkTrace
{
    public static bool Observe(
        int speedKmh,
        int tryThrPct,
        int tryIndyPct,
        int appliedThrPct,
        int appliedIndyPct,
        PidSpeedMode mode,
        ref PidSpeedWalkTraceCache cache)
    {
        if (cache.Seeded
            && cache.SpeedKmh == speedKmh
            && cache.TryThrPct == tryThrPct
            && cache.TryIndyPct == tryIndyPct
            && cache.AppliedThrPct == appliedThrPct
            && cache.AppliedIndyPct == appliedIndyPct
            && cache.Mode == mode)
        {
            return false;
        }

        cache.Seeded = true;
        cache.SpeedKmh = speedKmh;
        cache.TryThrPct = tryThrPct;
        cache.TryIndyPct = tryIndyPct;
        cache.AppliedThrPct = appliedThrPct;
        cache.AppliedIndyPct = appliedIndyPct;
        cache.Mode = mode;
        return true;
    }
}
