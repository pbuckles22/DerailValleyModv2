namespace YardMasterSuite.Core;

/// <summary>
/// Keyboard-style hold repeat: fire on press, pause, then fire on an interval while held.
/// </summary>
public static class HoldRepeat
{
    public const float DefaultInitialDelaySeconds = 0.35f;
    public const float DefaultIntervalSeconds = 0.08f;

    public static bool ShouldFire(
        bool pressedThisFrame,
        bool isHeld,
        float timeHeld,
        ref float nextFireAt,
        float initialDelaySeconds = DefaultInitialDelaySeconds,
        float intervalSeconds = DefaultIntervalSeconds)
    {
        if (!isHeld)
        {
            nextFireAt = 0f;
            return false;
        }

        var delay = initialDelaySeconds > 0f ? initialDelaySeconds : DefaultInitialDelaySeconds;
        var interval = intervalSeconds > 0f ? intervalSeconds : DefaultIntervalSeconds;

        if (pressedThisFrame)
        {
            nextFireAt = delay;
            return true;
        }

        if (timeHeld + 0.0001f < nextFireAt)
        {
            return false;
        }

        nextFireAt = timeHeld + interval;
        return true;
    }
}
