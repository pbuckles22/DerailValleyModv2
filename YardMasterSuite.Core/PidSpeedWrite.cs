namespace YardMasterSuite.Core;

/// <summary>
/// Three-Gate write intent for <see cref="PidSpeedHold"/>. Gear-pending must
/// still raise independent on overspeed (2.9.1.5: <c>thr-off</c> then
/// <c>pid: gear</c> at 32 with indy stuck at 0). Snap / Floor before
/// <see cref="PidSpeedNotch.ApplyExpander"/> so DE2 <c>Set</c> is on-grid.
/// </summary>
public static class PidSpeedWrite
{
    public static float Quantize(float desired, float current)
    {
        if (desired + PidSpeedNotch.ExactEpsilon < current)
        {
            return PidSpeedNotch.Hud(PidSpeedNotch.Floor(desired));
        }

        return PidSpeedNotch.Hud(PidSpeedNotch.Snap(desired));
    }

    public static bool Independent(
        float current,
        float desired,
        bool gearPending,
        bool brakePending)
    {
        if (LimitThrottleCap.ShouldRaise(current, desired))
        {
            return true;
        }

        if (!LimitThrottleCap.ShouldLower(current, desired))
        {
            return false;
        }

        return brakePending || !gearPending;
    }

    public static bool Throttle(
        float current,
        float desired,
        bool gearPending,
        bool brakePending,
        bool wantThrottle)
    {
        if (brakePending)
        {
            return LimitThrottleCap.ShouldLower(current, desired);
        }

        if (LimitThrottleCap.ShouldLower(current, desired))
        {
            return true;
        }

        if (gearPending)
        {
            return false;
        }

        return LimitThrottleCap.ShouldRaise(current, desired) || wantThrottle;
    }
}
