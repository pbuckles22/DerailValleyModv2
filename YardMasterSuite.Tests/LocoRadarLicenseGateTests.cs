using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16): v1 4.10 parity — the radar marks every loco. The licence rule is
/// still computed but parked, because a DE2-only save saw 0 of 9 nearby locos.
/// </summary>
public class LocoRadarLicenseGateTests
{
    [Fact]
    public void Smoke_de2_only_save_shows_unlicensed_locos_without_f11()
    {
        Assert.False(LocoRadarLicenseGate.FilterEnabled);

        var verdict = LocoRadarLicenseGate.Evaluate(
            hasLivery: true,
            requiresLicense: true,
            licenseQueryReady: true,
            playerHoldsRequiredLicense: false);

        Assert.Equal(LocoRadarLicenseVerdict.Exclude, verdict);
        Assert.True(LocoRadarLicenseGate.ShouldShow(verdict));
    }

    [Fact]
    public void Smoke_licensed_de2_shows_before_f11()
    {
        var verdict = LocoRadarLicenseGate.Evaluate(
            hasLivery: true,
            requiresLicense: false,
            licenseQueryReady: true,
            playerHoldsRequiredLicense: false);

        Assert.Equal(LocoRadarLicenseVerdict.Include, verdict);
        Assert.True(LocoRadarLicenseGate.ShouldShow(verdict));
    }

    /// <summary>Parked rule still evaluates, so re-arming the filter needs no new logic.</summary>
    [Fact]
    public void Unlicensed_loco_still_evaluates_as_exclude()
    {
        var verdict = LocoRadarLicenseGate.Evaluate(
            hasLivery: true,
            requiresLicense: true,
            licenseQueryReady: true,
            playerHoldsRequiredLicense: false);

        Assert.Equal(LocoRadarLicenseVerdict.Exclude, verdict);
    }

    [Fact]
    public void Smoke_unknown_livery_does_not_permanently_hide()
    {
        var verdict = LocoRadarLicenseGate.Evaluate(
            hasLivery: false,
            requiresLicense: false,
            licenseQueryReady: false,
            playerHoldsRequiredLicense: false);

        Assert.Equal(LocoRadarLicenseVerdict.Unknown, verdict);
        Assert.True(LocoRadarScanPolicy.ShouldForceScanOnLicenseUnknownRetry(
            hadUnknown: true,
            alreadyRetried: false));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnLicenseUnknownRetry(
            hadUnknown: true,
            alreadyRetried: true));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnLicenseUnknownRetry(
            hadUnknown: false,
            alreadyRetried: false));
        Assert.True(LocoRadarScanPolicy.ShouldForceScanOnKnowledgeRetry(
            hadUnknownLicense: false,
            sawZeroLocoCars: true,
            alreadyRetried: false));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnKnowledgeRetry(
            hadUnknownLicense: false,
            sawZeroLocoCars: true,
            alreadyRetried: true));
    }

    [Fact]
    public void Smoke_empty_radar_rescans_when_player_loco_known()
    {
        Assert.True(LocoRadarScanPolicy.ShouldForceScanOnPlayerLocoKnown(
            cacheCount: 0,
            playerHasKnownLoco: true,
            alreadyUsed: false));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnPlayerLocoKnown(
            cacheCount: 1,
            playerHasKnownLoco: true,
            alreadyUsed: false));
        Assert.False(LocoRadarScanPolicy.ShouldForceScanOnPlayerLocoKnown(
            cacheCount: 0,
            playerHasKnownLoco: true,
            alreadyUsed: true));
    }
}
