namespace YardMasterSuite
{
    /// <summary>Hide YMS HUD + AR only on pause/save/modal — desk stays a driving guide.</summary>
    internal static class YmsOnScreenVisibility
    {
        internal static bool ShouldDraw(bool playerTransformPresent)
        {
            if (!playerTransformPresent)
            {
                return false;
            }

            if (!ScreenOverlayGate.WorldReady())
            {
                return false;
            }

            return !ScreenOverlayGate.IsBlocking();
        }
    }
}
