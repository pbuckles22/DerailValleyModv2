namespace YardMasterSuite.Core;

/// <summary>
/// Hotkey policy: UI/tools use Ctrl+key; Numpad gameplay is single-tap
/// (vanilla DV leaves the numpad free). Unity KeyCode only — never Rewired.
/// </summary>
public static class YmsHotkeyPolicy
{
    public const string MarkSetLegend = "Ctrl+Home";
    public const string MarkClearLegend = "Ctrl+Shift+Home";
    public const string PathSetLegend = "Ctrl+End";
    public const string PathClearLegend = "Ctrl+Shift+End";
    public const string LicenseDebugLegend = "Ctrl+F8";

    public static bool ControlHeld(bool leftControl, bool rightControl) =>
        leftControl || rightControl;

    /// <summary>Home / End / F8 family — require either Control key.</summary>
    public static bool ShouldAcceptToolChord(bool controlHeld, bool primaryKeyDown) =>
        controlHeld && primaryKeyDown;
}
