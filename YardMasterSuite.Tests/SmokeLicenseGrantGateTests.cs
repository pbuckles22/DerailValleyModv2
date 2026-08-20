using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: 6.7 older-save license grant. Flip
/// <see cref="SmokeLicenseGrantGate.Enabled"/> to disable.
/// </summary>
public class SmokeLicenseGrantGateTests
{
    [Fact]
    public void Ship_default_does_not_grant_licenses()
    {
        Assert.False(SmokeLicenseGrantGate.Enabled);
    }

    [Fact]
    public void Smoke_older_save_skips_grant_when_flag_off()
    {
        Assert.Equal("T2 licenses skip: flag off", SmokeLicenseGrantGate.FormatDisabled());
    }

    [Fact]
    public void Smoke_older_save_emits_T2_licenses_granted_n()
    {
        Assert.Equal("T2 licenses granted: n=12", SmokeLicenseGrantGate.FormatGranted(12));
    }

    [Fact]
    public void Smoke_older_save_emits_T2_licenses_fail_without_manager()
    {
        Assert.Equal("T2 licenses fail: no LicenseManager", SmokeLicenseGrantGate.FormatFail("no LicenseManager"));
    }
}
