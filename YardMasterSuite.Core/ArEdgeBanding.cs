namespace YardMasterSuite.Core
{
    /// <summary>
    /// Vertical band for an AR chip in GUI space (top-left origin).
    /// Smoke C: ClampToScreen parked LOCO in <see cref="Top"/> (HUD stack).
    /// </summary>
    public enum ArEdgeBand
    {
        Hidden = 0,
        Object = 1,
        Mid = 2,
        Top = 3,
        Bottom = 4,
    }

    /// <summary>
    /// HUD occupies two 26 px bars from y=8. Marker icon sits above GuiY.
    /// 96 px keeps the square below both bars.
    /// </summary>
    public static class ArEdgeBanding
    {
        public const float HudClearanceGuiY = 96f;

        public static ArEdgeBand ClassifyGuiY(float guiY, float screenHeight)
        {
            if (guiY < HudClearanceGuiY)
            {
                return ArEdgeBand.Top;
            }

            if (screenHeight > 0f && guiY > screenHeight - HudClearanceGuiY)
            {
                return ArEdgeBand.Bottom;
            }

            return ArEdgeBand.Mid;
        }

        public static ArEdgeBand Classify(in ArMarkerSlot slot, float screenHeight)
        {
            if (!slot.Occupied || slot.Place == ArMarkerPlace.Hidden)
            {
                return ArEdgeBand.Hidden;
            }

            if (slot.Place == ArMarkerPlace.OnObject)
            {
                return ArEdgeBand.Object;
            }

            return ClassifyGuiY(slot.GuiY, screenHeight);
        }
    }
}
