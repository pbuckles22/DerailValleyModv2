namespace YardMasterSuite.Core;

/// <summary>
/// Radar only marks locos the player is licensed to drive (6.16 smoke).
/// No extra license (DE2) → show. Missing livery / LicenseManager → unknown,
/// hide this scan and retry once — do not fail-closed forever.
/// </summary>
public enum LocoRadarLicenseVerdict
{
    Include = 0,
    Exclude = 1,
    Unknown = 2,
}

public static class LocoRadarLicenseGate
{
    /// <summary>
    /// Parked: v1 4.10 marks every loco, and filtering left the radar empty at low career
    /// stages (DE2-only save saw 0 of 9 locos). Flip to true to re-arm — see PM_PLAN deferred.
    /// </summary>
    public const bool FilterEnabled = false;

    public static LocoRadarLicenseVerdict Evaluate(
        bool hasLivery,
        bool requiresLicense,
        bool licenseQueryReady,
        bool playerHoldsRequiredLicense)
    {
        if (!hasLivery)
        {
            return LocoRadarLicenseVerdict.Unknown;
        }

        if (!requiresLicense)
        {
            return LocoRadarLicenseVerdict.Include;
        }

        if (!licenseQueryReady)
        {
            return LocoRadarLicenseVerdict.Unknown;
        }

        return playerHoldsRequiredLicense
            ? LocoRadarLicenseVerdict.Include
            : LocoRadarLicenseVerdict.Exclude;
    }

    public static bool ShouldShow(LocoRadarLicenseVerdict verdict) =>
        !FilterEnabled || verdict == LocoRadarLicenseVerdict.Include;
}
