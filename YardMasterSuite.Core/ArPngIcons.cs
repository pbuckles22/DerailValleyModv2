namespace YardMasterSuite.Core
{
    /// <summary>
    /// v1 4.9 drop path: <c>Mods/YardMasterSuite/Icons/*.png</c>.
    /// </summary>
    public static class ArPngIcons
    {
        public const string FolderName = "Icons";

        public static string FileName(ArWaypointKind kind)
        {
            switch (kind)
            {
                case ArWaypointKind.Loco:
                    return "loco.png";
                case ArWaypointKind.Station:
                    return "station.png";
                case ArWaypointKind.Pin:
                    return "pin.png";
                case ArWaypointKind.OtherLoco:
                    return "radar.png"; // v1 4.10: same art as loco.png, amber tint
                case ArWaypointKind.JobCar:
                    return string.Empty; // purple quad this story; PNG may wait
                default:
                    return "loco.png";
            }
        }
    }

    /// <summary>Dark plate behind PNG + caption (v1 4.9 chrome).</summary>
    public static class ArMarkerPlate
    {
        public const float ContentPadX = 8f;
        public const float ExpandX = 4f;
        public const float ExpandY = 2f;
        public const float LabelGapY = 2f;
        public const float FillAlpha = 0.55f;

        public const float HorizontalChromePixels = ContentPadX + (ExpandX * 2f);

        public static float OuterHeightPixels(float iconPixels, float captionHeight)
        {
            var icon = iconPixels < 0f ? 0f : iconPixels;
            var cap = captionHeight < 0f ? 0f : captionHeight;
            var content = icon + (cap > 0f ? cap + LabelGapY : 0f);
            return content + (ExpandY * 2f);
        }
    }
}
