namespace YardMasterSuite.Core;

/// <summary>
/// One Numpad Enter is one cycle. Ignore Windows/Unity key-repeat until
/// KeyUp. Hold the written notch so Rewired / a same-press echo cannot
/// snap Reverse → Forward → Neutral (6.16 smoke).
/// </summary>
public static class ReverserCyclePressGate
{
    public const float DefaultCooldownSeconds = 0.35f;
    public const float DefaultHoldSeconds = 0.25f;

    public static bool ShouldAcceptPress(
        float now,
        float lastAcceptedAt,
        float cooldownSeconds = DefaultCooldownSeconds,
        bool sawKeyUpSinceLastAccept = true)
    {
        if (!sawKeyUpSinceLastAccept)
        {
            return false;
        }

        if (float.IsNaN(now) || now < 0f)
        {
            return false;
        }

        if (lastAcceptedAt < 0f || float.IsNaN(lastAcceptedAt))
        {
            return true;
        }

        var cooldown = cooldownSeconds > 0f ? cooldownSeconds : DefaultCooldownSeconds;
        return now - lastAcceptedAt >= cooldown;
    }

    public static bool ShouldHoldWrittenValue(
        float now,
        float writtenAt,
        float holdSeconds = DefaultHoldSeconds)
    {
        if (writtenAt < 0f || float.IsNaN(writtenAt) || float.IsNaN(now))
        {
            return false;
        }

        var hold = holdSeconds > 0f ? holdSeconds : DefaultHoldSeconds;
        return now - writtenAt < hold;
    }

    public static bool ShouldPassThroughNeutral(float current, float next)
    {
        return ProximityTravelDirectionGate.FromReverser(current)
                == ProximityTravelDirection.Reverse
            && ProximityTravelDirectionGate.FromReverser(next)
                == ProximityTravelDirection.Forward;
    }
}
