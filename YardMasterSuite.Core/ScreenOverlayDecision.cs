namespace YardMasterSuite.Core;

/// <summary>
/// Pause / save / modal / career notification cover the world — hide AR
/// (6.16 Vehicle Restoration smoke). HUD bars may stay.
/// </summary>
public static class ScreenOverlayDecision
{
    public static bool IsBlocking(
        bool pauseMenuOpen,
        bool modalPopupOpen,
        bool notificationOpen) =>
        pauseMenuOpen || modalPopupOpen || notificationOpen;
}
