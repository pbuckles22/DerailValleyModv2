using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>6.20 held-overview missing-license chip (v1 0.4.53).</summary>
public class LicenseWarnDisplayTests
{
    [Fact]
    public void Format_null_when_empty()
    {
        Assert.Null(LicenseWarnDisplay.Format(null));
        Assert.Null(LicenseWarnDisplay.Format(Array.Empty<string>()));
        Assert.Null(LicenseWarnDisplay.Format(new[] { "  ", "" }));
    }

    [Fact]
    public void Smoke_no_license_fh_formats_red()
    {
        Assert.Equal("No license: FH", LicenseWarnDisplay.Format(new[] { "FH" }));
        Assert.Equal("No license: FH, HZ1", LicenseWarnDisplay.Format(new[] { "FH", "HZ1" }));
        Assert.Contains(LicenseWarnDisplay.WarnColor, LicenseWarnDisplay.Format(new[] { "FH" }, richText: true)!);
    }

    [Fact]
    public void Abbreviate_ticket_style_codes()
    {
        Assert.Equal("FH", LicenseWarnDisplay.Abbreviate("FreightHaul"));
        Assert.Equal("SH", LicenseWarnDisplay.Abbreviate("Shunting"));
        Assert.Equal("LH", LicenseWarnDisplay.Abbreviate("LogisticalHaul"));
        Assert.Equal("HZ1", LicenseWarnDisplay.Abbreviate("Hazmat1"));
        Assert.Equal("TL2", LicenseWarnDisplay.Abbreviate("TrainLength2"));
        Assert.Equal("FH", LicenseWarnDisplay.Abbreviate("FH"));
        Assert.Equal(string.Empty, LicenseWarnDisplay.Abbreviate(null));
    }

    [Fact]
    public void NormalizeCodes_dedupes_and_abbreviates()
    {
        var codes = LicenseWarnDisplay.NormalizeCodes(new[] { "FreightHaul", "FH", "Hazmat1", "  " });
        Assert.Equal(new[] { "FH", "HZ1" }, codes);
    }
}
