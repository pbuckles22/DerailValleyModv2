namespace YardMasterSuite.Core
{
    /// <summary>
    /// Shared Monitor HUD stack geometry (GUI / top-left origin). v1 4.7 IA layout.
    /// Stack order: loco → look-at → job → always-on (heading).
    /// </summary>
    public static class MonitorHudStackLayout
    {
        public const float Pad = 12f;
        public const float BarHeight = 28f;
        public const float Gap = 4f;
        public const float StickyRowGap = 8f;

        /// <summary>GUI Y just below the last optional bar plus always-on heading.</summary>
        public static float StackBottomGuiY(bool hasTrainBar, bool hasLocalBar, bool hasJobBar)
        {
            var y = Pad;
            if (hasTrainBar)
            {
                y += BarHeight + Gap;
            }

            if (hasLocalBar)
            {
                y += BarHeight + Gap;
            }

            if (hasJobBar)
            {
                y += BarHeight + Gap;
            }

            return y + BarHeight;
        }
    }

    /// <summary>Horizontally center a HUD bar from measured content width.</summary>
    public static class HudCenterLayout
    {
        public static float CenteredBarX(float contentWidth, float screenWidth, float pad)
        {
            if (contentWidth <= 0f || screenWidth <= 0f)
            {
                return pad;
            }

            var x = (screenWidth - contentWidth) * 0.5f;
            return x < pad ? pad : x;
        }
    }

    /// <summary>Sticky-row placement for AR markers under the HUD stack.</summary>
    public static class ArStickyRowPlacement
    {
        public static float StickyRowTopGuiY(float stackBottomGuiY, float gapBelowHud = MonitorHudStackLayout.StickyRowGap) =>
            stackBottomGuiY + gapBelowHud;

        /// <summary>
        /// Slot GUI Y (icon bottom / label top). Edge pins under the HUD stack;
        /// OnObject keeps the world projection.
        /// </summary>
        public static float ResolveSlotGuiY(
            ArMarkerPlace place,
            float projectedGuiY,
            float stackBottomGuiY,
            float iconPixels)
        {
            if (place != ArMarkerPlace.Edge || stackBottomGuiY <= 1f || iconPixels <= 0f)
            {
                return projectedGuiY;
            }

            return StickyRowTopGuiY(stackBottomGuiY) + iconPixels;
        }

        public static void PinScreenYToStickyRow(
            float stickyRowCenterGuiY,
            float screenHeight,
            ref float screenY) =>
            screenY = screenHeight - stickyRowCenterGuiY;

        public static float MarkerTopGuiY(float stickyRowTopGuiY, float markerHeight) =>
            stickyRowTopGuiY;
    }
}
