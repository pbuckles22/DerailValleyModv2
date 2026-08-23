namespace YardMasterSuite.Core;

/// <summary>Why a loco-radar FindObjectsOfType scan may run (6.16 hitch diet).</summary>
public enum LocoRadarScanReason
{
    /// <summary>Keep prior cache — no FoT.</summary>
    None = 0,

    /// <summary>Player entered / changed city (yard id).</summary>
    CityEntered = 1,

    /// <summary>Player left a loco (or switched to another). Mark the departed id.</summary>
    LeftLoco = 2,

    /// <summary>World enter / invalidate — one refresh.</summary>
    Forced = 3,
}

/// <summary>
/// Event-gated loco radar scans: one FoT per city, one on leave-loco, never on a timer.
/// Parked locos do not justify rescans; a moving loco the player occupies is already known.
/// </summary>
public static class LocoRadarScanPolicy
{
    /// <summary>
    /// Decide whether to FoT-scan. Updates nothing — caller advances trackers after the call.
    /// When <see cref="LocoRadarScanReason.LeftLoco"/>, <paramref name="leftLocoId"/> is the departed loco.
    /// </summary>
    public static LocoRadarScanReason Decide(
        bool featureEnabled,
        bool forceScan,
        string? lastScannedCityId,
        string? currentCityId,
        int? lastOccupiedLocoId,
        int? currentOccupiedLocoId,
        out int? leftLocoId)
    {
        leftLocoId = null;
        if (!featureEnabled)
        {
            return LocoRadarScanReason.None;
        }

        if (forceScan)
        {
            return LocoRadarScanReason.Forced;
        }

        if (lastOccupiedLocoId.HasValue
            && (!currentOccupiedLocoId.HasValue
                || currentOccupiedLocoId.Value != lastOccupiedLocoId.Value))
        {
            leftLocoId = lastOccupiedLocoId;
            return LocoRadarScanReason.LeftLoco;
        }

        if (!string.IsNullOrWhiteSpace(currentCityId)
            && !CityEquals(lastScannedCityId, currentCityId))
        {
            return LocoRadarScanReason.CityEntered;
        }

        return LocoRadarScanReason.None;
    }

    /// <summary>
    /// Second save / world unload: drop cached TrainCar refs (6.16 smoke).
    /// </summary>
    public static bool ShouldInvalidateCache(bool wasInWorld, bool inWorld) =>
        wasInWorld && !inWorld;

    /// <summary>Rising world edge after a load — FoT even if city id is unchanged.</summary>
    public static bool ShouldForceScanOnWorldEnter(bool wasInWorld, bool inWorld) =>
        inWorld && !wasInWorld;

    /// <summary>
    /// Cached ids survived a reload but the cars did not (Unity destroyed).
    /// </summary>
    public static bool ShouldForceScanWhenCacheDead(int cachedCount, int liveCount) =>
        cachedCount > 0 && liveCount <= 0;

    /// <summary>F8 license debug / career buy — license set changed, re-rank radar.</summary>
    public static bool ShouldForceScanOnLicenseChange(int lastLicenseKey, int currentLicenseKey) =>
        lastLicenseKey != currentLicenseKey;

    public const float LicenseUnknownRetrySeconds = 0.75f;

    /// <summary>
    /// First FoT can run before liveries exist (6.16 smoke). One retry, not a timer loop.
    /// </summary>
    public static bool ShouldForceScanOnLicenseUnknownRetry(bool hadUnknown, bool alreadyRetried) =>
        ShouldForceScanOnKnowledgeRetry(
            hadUnknownLicense: hadUnknown,
            sawZeroLocoCars: false,
            alreadyRetried);

    /// <summary>
    /// First FoT can run before cars or liveries exist. One retry, not a timer loop.
    /// </summary>
    public static bool ShouldForceScanOnKnowledgeRetry(
        bool hadUnknownLicense,
        bool sawZeroLocoCars,
        bool alreadyRetried) =>
        !alreadyRetried && (hadUnknownLicense || sawZeroLocoCars);

    /// <summary>
    /// Empty radar after an early FoT — rescan once when LastLoco / occupied loco exists.
    /// </summary>
    public static bool ShouldForceScanOnPlayerLocoKnown(
        int cacheCount,
        bool playerHasKnownLoco,
        bool alreadyUsed) =>
        cacheCount <= 0 && playerHasKnownLoco && !alreadyUsed;

    public static bool CityEquals(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        return string.Equals(a!.Trim(), b!.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
