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
        Assert.Equal("Ctrl+Insert", YmsHotkeyPolicy.DeskToggleLegend);
        Assert.Equal("Ctrl+PageUp", YmsHotkeyPolicy.AlignLegend);
        Assert.Equal("Ctrl+PageDown", YmsHotkeyPolicy.NextLegend);
    }

    [Fact]
    public void Smoke_8_7_align_next_chords_are_tool_keys()
    {
        Assert.True(YmsHotkeyPolicy.ShouldAcceptToolChord(controlHeld: true, primaryKeyDown: true));
        Assert.False(YmsHotkeyPolicy.ShouldAcceptToolChord(controlHeld: false, primaryKeyDown: true));
    }

    [Fact]
    public void Smoke_numpad_plus_or_enter_cycles_reverser()
    {
        Assert.False(YmsHotkeyPolicy.IsReverserCycleKey(keypadEnter: false, keypadPlus: false));
        Assert.True(YmsHotkeyPolicy.IsReverserCycleKey(keypadEnter: true, keypadPlus: false));
        Assert.True(YmsHotkeyPolicy.IsReverserCycleKey(keypadEnter: false, keypadPlus: true));
    }
}
