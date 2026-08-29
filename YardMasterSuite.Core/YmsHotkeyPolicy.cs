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
    public const string DeskToggleLegend = "Ctrl+Insert";
    public const string LocoBringConfirmLegend = "Ctrl+Enter";
    public const string AlignLegend = "Ctrl+PageUp";
    public const string NextLegend = "Ctrl+PageDown";

    public static bool ControlHeld(bool leftControl, bool rightControl) =>
        leftControl || rightControl;

    /// <summary>Home / End / F8 / Enter family — require either Control key.</summary>
    public static bool ShouldAcceptToolChord(bool controlHeld, bool primaryKeyDown) =>
        controlHeld && primaryKeyDown;

    /// <summary>
    /// Reverser cycle: Numpad <c>+</c> (player key) or Numpad Enter. Same
    /// predicate for GetKeyDown and GetKeyUp.
    /// </summary>
    public static bool IsReverserCycleKey(bool keypadEnter, bool keypadPlus) =>
        keypadEnter || keypadPlus;
}
