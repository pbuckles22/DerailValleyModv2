namespace YardMasterSuite.Core
{
    /// <summary>
    /// Whether the Display Shell may draw. Launcher and menus have no player transform.
    /// World must finish streaming before listeners wake (player can spawn at ~86%).
    /// Hides quit-time consist peel from the HUD.
    /// </summary>
    public static class HudWorldSession
    {
        public static bool IsActive(bool playerTransformPresent, bool worldReady = true) =>
            playerTransformPresent && worldReady;
    }
}
