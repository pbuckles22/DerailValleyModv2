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
    public void Abbreviate_covers_all_known_license_aliases()
    {
        Assert.Equal(string.Empty, LicenseWarnDisplay.Abbreviate("  "));
        Assert.Equal("FH", LicenseWarnDisplay.Abbreviate("FH"));
        Assert.Equal("SH", LicenseWarnDisplay.Abbreviate("SH"));
        Assert.Equal("LH", LicenseWarnDisplay.Abbreviate("LH"));
        Assert.Equal("HZ1", LicenseWarnDisplay.Abbreviate("HZ1"));
        Assert.Equal("HZ2", LicenseWarnDisplay.Abbreviate("Hazmat2"));
        Assert.Equal("HZ2", LicenseWarnDisplay.Abbreviate("HZ2"));
        Assert.Equal("HZ3", LicenseWarnDisplay.Abbreviate("Hazmat3"));
        Assert.Equal("HZ3", LicenseWarnDisplay.Abbreviate("HZ3"));
        Assert.Equal("M1", LicenseWarnDisplay.Abbreviate("Military1"));
        Assert.Equal("M1", LicenseWarnDisplay.Abbreviate("M1"));
        Assert.Equal("M2", LicenseWarnDisplay.Abbreviate("Military2"));
        Assert.Equal("M2", LicenseWarnDisplay.Abbreviate("M2"));
        Assert.Equal("M3", LicenseWarnDisplay.Abbreviate("Military3"));
        Assert.Equal("M3", LicenseWarnDisplay.Abbreviate("M3"));
        Assert.Equal("TL1", LicenseWarnDisplay.Abbreviate("TrainLength1"));
        Assert.Equal("TL1", LicenseWarnDisplay.Abbreviate("TL1"));
        Assert.Equal("TL2", LicenseWarnDisplay.Abbreviate("TL2"));
        Assert.Equal("FR", LicenseWarnDisplay.Abbreviate("Fragile"));
        Assert.Equal("FR", LicenseWarnDisplay.Abbreviate("FR"));
        Assert.Equal("DISP", LicenseWarnDisplay.Abbreviate("Dispatcher1"));
        Assert.Equal("DISP", LicenseWarnDisplay.Abbreviate("Dispatcher"));
        Assert.Equal("DISP", LicenseWarnDisplay.Abbreviate("DISP"));
        Assert.Equal("Basic", LicenseWarnDisplay.Abbreviate("Basic"));
        Assert.Equal("CustomX", LicenseWarnDisplay.Abbreviate("CustomX"));
    }

    [Fact]
    public void NormalizeCodes_null_is_empty()
    {
        Assert.Empty(LicenseWarnDisplay.NormalizeCodes(null));
    }

    [Fact]
    public void NormalizeCodes_dedupes_and_abbreviates()
    {
        var codes = LicenseWarnDisplay.NormalizeCodes(new[] { "FreightHaul", "FH", "Hazmat1", "  " });
        Assert.Equal(new[] { "FH", "HZ1" }, codes);
    }
}
