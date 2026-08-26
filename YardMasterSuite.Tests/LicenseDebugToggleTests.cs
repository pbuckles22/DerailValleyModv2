using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LicenseDebugToggleTests
{
    [Fact]
    public void Next_toggles_real_and_all()
    {
        Assert.Equal(LicenseDebugMode.AllGranted, LicenseDebugToggle.Next(LicenseDebugMode.Real));
        Assert.Equal(LicenseDebugMode.Real, LicenseDebugToggle.Next(LicenseDebugMode.AllGranted));
    }

    [Fact]
    public void Smoke_license_debug_hotkey_is_not_f11()
    {
        Assert.Equal("F8", LicenseDebugToggle.HotkeyName);
        Assert.NotEqual("F11", LicenseDebugToggle.HotkeyName);
        Assert.True(LicenseDebugToggle.RequiresControlChord);
        Assert.Equal("Ctrl+F8", LicenseDebugToggle.ChordLegend);
    }

    [Fact]
    public void Smoke_license_debug_status_names_real_vs_all()
    {
        Assert.Equal("all licenses", LicenseDebugToggle.StatusFragment(LicenseDebugMode.AllGranted));
        Assert.Equal("real licenses", LicenseDebugToggle.StatusFragment(LicenseDebugMode.Real));
        Assert.Equal(
            "T2 licenses debug: all licenses",
            LicenseDebugToggle.FormatLog(LicenseDebugMode.AllGranted));
        Assert.Equal(
            "T2 licenses debug: real licenses",
            LicenseDebugToggle.FormatLog(LicenseDebugMode.Real));
    }
}
