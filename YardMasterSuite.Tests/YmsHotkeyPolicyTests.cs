using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class YmsHotkeyPolicyTests
{
    [Fact]
    public void Smoke_tool_keys_require_control_chord()
    {
        Assert.False(YmsHotkeyPolicy.ShouldAcceptToolChord(controlHeld: false, primaryKeyDown: true));
        Assert.False(YmsHotkeyPolicy.ShouldAcceptToolChord(controlHeld: true, primaryKeyDown: false));
        Assert.True(YmsHotkeyPolicy.ShouldAcceptToolChord(controlHeld: true, primaryKeyDown: true));
    }

    [Fact]
    public void Smoke_either_control_key_counts()
    {
        Assert.True(YmsHotkeyPolicy.ControlHeld(leftControl: true, rightControl: false));
        Assert.True(YmsHotkeyPolicy.ControlHeld(leftControl: false, rightControl: true));
        Assert.False(YmsHotkeyPolicy.ControlHeld(leftControl: false, rightControl: false));
    }

    [Fact]
    public void Smoke_tool_legends_document_ctrl_chords()
    {
        Assert.Equal("Ctrl+Home", YmsHotkeyPolicy.MarkSetLegend);
        Assert.Equal("Ctrl+Shift+Home", YmsHotkeyPolicy.MarkClearLegend);
        Assert.Equal("Ctrl+End", YmsHotkeyPolicy.PathSetLegend);
        Assert.Equal("Ctrl+Shift+End", YmsHotkeyPolicy.PathClearLegend);
        Assert.Equal("Ctrl+F8", YmsHotkeyPolicy.LicenseDebugLegend);
    }
}
