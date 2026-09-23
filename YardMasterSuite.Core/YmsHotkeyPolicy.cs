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
    public const string DeskToggleLegend = "Ctrl+Insert / Ctrl+Right / Ctrl+Left";
    public const string LocoBringConfirmLegend = "Ctrl+Enter";
    public const string AlignLegend = "Ctrl+PageUp";
    public const string NextLegend = "Ctrl+PageDown";

    public static bool ControlHeld(bool leftControl, bool rightControl) =>
        leftControl || rightControl;

    /// <summary>Home / End / F8 / Enter family — require either Control key.</summary>
    public static bool ShouldAcceptToolChord(bool controlHeld, bool primaryKeyDown) =>
        controlHeld && primaryKeyDown;

    /// <summary>
    /// Maps desk open/close. Insert is the original chord; RightArrow is the
    /// laptop alias when Insert is missing or Fn-layered. LeftArrow is the
    /// same alias (players hit Left as often as Right).
    /// </summary>
    public static bool ShouldAcceptDeskToggle(
        bool controlHeld,
        bool insertDown,
        bool rightArrowDown,
        bool leftArrowDown = false) =>
        ShouldAcceptToolChord(controlHeld, insertDown || rightArrowDown || leftArrowDown);

    /// <summary>
    /// Cab 2.16.27: Ctrl+Right and Ctrl+Insert never reached
    /// <c>Input.GetKeyDown</c> or the GUI key event (no <c>T2 desk-key</c>).
    /// The keys are still held. Accept the rising edge of that hold so a
    /// stuck key does not toggle every frame.
    /// </summary>
    public static bool ShouldAcceptDeskChord(
        bool controlHeld,
        bool insertHeld,
        bool rightHeld,
        bool leftHeld,
        bool heldPrevious) =>
        !heldPrevious && ShouldAcceptDeskToggle(controlHeld, insertHeld, rightHeld, leftHeld);

    /// <summary>
    /// Ctrl+arrow often never reaches <c>Input.GetKeyDown</c>. The GUI key
    /// event still has Control. Ignore repaint and layout passes.
    /// </summary>
    public static bool ShouldAcceptDeskToggleFromEvent(
        bool isKeyDown,
        bool controlHeld,
        bool insertDown,
        bool rightArrowDown,
        bool leftArrowDown = false) =>
        isKeyDown && ShouldAcceptDeskToggle(controlHeld, insertDown, rightArrowDown, leftArrowDown);

    /// <summary>
    /// Reverser cycle: Numpad <c>+</c> (player key) or Numpad Enter. Same
    /// predicate for GetKeyDown and GetKeyUp.
    /// </summary>
    public static bool IsReverserCycleKey(bool keypadEnter, bool keypadPlus) =>
        keypadEnter || keypadPlus;
}
