namespace YardMasterSuite.Core;

/// <summary>
/// Cap overlay FindObjectOfType. Missing notification root used to retry every 2 s
/// and hitch cab drive (~120–150 ms, ~15 feature frames / 30 s).
/// </summary>
public static class ScreenOverlayHandlePolicy
{
    public const int MaxLookupsPerWorld = 2;

    public const float RetrySeconds = 2f;

    public static bool ShouldLookup(
        bool havePopup,
        bool haveNotification,
        int attempts,
        float now,
        float nextRetryAt)
    {
        if (havePopup && haveNotification)
        {
            return false;
        }

        if (attempts >= MaxLookupsPerWorld)
        {
            return false;
        }

        if (attempts <= 0)
        {
            return true;
        }

        return now >= nextRetryAt;
    }
}
