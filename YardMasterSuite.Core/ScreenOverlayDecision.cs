namespace YardMasterSuite.Core;

/// <summary>
/// Pause / save / modal / career notification cover the world — hide YMS HUD + AR
/// (6.16 Vehicle Restoration smoke). Maps desk uses a separate Unity gate.
/// </summary>
public static class ScreenOverlayDecision
{
    public static bool IsBlocking(
        bool pauseMenuOpen,
        bool modalPopupOpen,
        bool notificationOpen) =>
        pauseMenuOpen || modalPopupOpen || notificationOpen;

    /// <summary>UI tool chords stay live under career notifications / modals — pause only.</summary>
    public static bool BlocksToolHotkeys(bool pauseMenuOpen) => pauseMenuOpen;
}
