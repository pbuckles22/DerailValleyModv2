namespace YardMasterSuite.Core
{
    /// <summary>
    /// Whether the Display Shell may draw. Launcher and menus have no player transform.
    /// Hides quit-time consist peel from the HUD.
    /// </summary>
    public static class HudWorldSession
    {
        public static bool IsActive(bool playerTransformPresent) => playerTransformPresent;
    }
}
