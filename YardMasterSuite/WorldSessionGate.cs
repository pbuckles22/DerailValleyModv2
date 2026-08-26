using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Player transform plus loading/stream gate for HUD and background listeners.
    /// </summary>
    internal static class WorldSessionGate
    {
        public static bool IsActive() =>
            HudWorldSession.IsActive(
                PlayerManager.PlayerTransform != null,
                ScreenOverlayGate.WorldReady());
    }
}
