using System;

namespace YardMasterSuite.Core
{
    /// <summary>Display-shell waypoint kinds. Job-car / route-leg are later stories.</summary>
    public enum ArWaypointKind
    {
        Loco = 0,
        Station = 1,
        Pin = 2,
    }

    /// <summary>
    /// Pure screen helpers for AR markers (Unity supplies WorldToScreenPoint).
    /// Screen coords use Unity’s bottom-left origin.
    /// </summary>
    public static class ArMarkerProjection
    {
        public const float DefaultEdgeMarginPixels = 28f;

        public static bool IsBehindCamera(float viewForward, float epsilon = 0.05f) =>
            viewForward <= epsilon;

        public static bool IsBehindCameraHysteresis(
            float viewForward,
            bool wasBehind,
            float enterBehind = 0.05f,
            float exitAhead = 0.35f)
        {
            if (wasBehind)
            {
                return viewForward <= exitAhead;
            }

            return viewForward <= enterBehind;
        }

        public static bool IsOnScreen(
            float screenX,
            float screenY,
            float screenWidth,
            float screenHeight,
            float inflatePixels = 0f) =>
            screenX >= -inflatePixels
            && screenX <= screenWidth + inflatePixels
            && screenY >= -inflatePixels
            && screenY <= screenHeight + inflatePixels;

        public static bool ShouldPlaceOnObject(
            bool behindCamera,
            float screenZ,
            float screenX,
            float screenY,
            float screenWidth,
            float screenHeight,
            float inflatePixels = 0f) =>
            !behindCamera
            && screenZ > 0.05f
            && IsOnScreen(screenX, screenY, screenWidth, screenHeight, inflatePixels);

        public static void ApplyBehindCameraEdge(
            bool behindCamera,
            float viewRight,
            float viewUp,
            float screenWidth,
            float screenHeight,
            float edgeMargin,
            ref float screenX,
            ref float screenY)
        {
            if (!behindCamera)
            {
                return;
            }

            ProjectViewDirectionToEdge(
                viewRight,
                viewUp,
                screenWidth,
                screenHeight,
                edgeMargin,
                out screenX,
                out screenY);
        }

        public static void ApplyBehindCameraHorizontalEdge(
            bool behindCamera,
            ArHorizontalEdge side,
            float screenWidth,
            float screenHeight,
            float edgeMargin,
            ref float screenX,
            ref float screenY)
        {
            if (!behindCamera)
            {
                return;
            }

            var minX = edgeMargin;
            var maxX = Math.Max(edgeMargin, screenWidth - edgeMargin);
            screenX = side == ArHorizontalEdge.Right ? maxX : minX;
            screenY = screenHeight * 0.5f;
        }

        public static void ProjectViewDirectionToEdge(
            float viewRight,
            float viewUp,
            float screenWidth,
            float screenHeight,
            float edgeMargin,
            out float screenX,
            out float screenY)
        {
            var minX = edgeMargin;
            var maxX = Math.Max(edgeMargin, screenWidth - edgeMargin);
            var minY = edgeMargin;
            var maxY = Math.Max(edgeMargin, screenHeight - edgeMargin);
            var cx = (minX + maxX) * 0.5f;
            var cy = (minY + maxY) * 0.5f;

            if (Math.Abs(viewRight) < 1e-6f && Math.Abs(viewUp) < 1e-6f)
            {
                screenX = cx;
                screenY = minY;
                return;
            }

            var angle = Math.Atan2(viewRight, viewUp);
            var dx = (float)Math.Sin(angle);
            var dy = (float)Math.Cos(angle);

            var t = float.PositiveInfinity;
            if (dx > 1e-6f)
            {
                t = Math.Min(t, (maxX - cx) / dx);
            }
            else if (dx < -1e-6f)
            {
                t = Math.Min(t, (minX - cx) / dx);
            }

            if (dy > 1e-6f)
            {
                t = Math.Min(t, (maxY - cy) / dy);
            }
            else if (dy < -1e-6f)
            {
                t = Math.Min(t, (minY - cy) / dy);
            }

            if (float.IsInfinity(t) || t < 0f)
            {
                screenX = cx;
                screenY = minY;
                return;
            }

            screenX = cx + dx * t;
            screenY = cy + dy * t;
        }

        public static bool ClampToScreen(
            float x,
            float y,
            float screenWidth,
            float screenHeight,
            float edgeMargin,
            out float clampedX,
            out float clampedY)
        {
            var minX = edgeMargin;
            var maxX = Math.Max(edgeMargin, screenWidth - edgeMargin);
            var minY = edgeMargin;
            var maxY = Math.Max(edgeMargin, screenHeight - edgeMargin);

            clampedX = Math.Min(maxX, Math.Max(minX, x));
            clampedY = Math.Min(maxY, Math.Max(minY, y));
            return Math.Abs(clampedX - x) > 0.5f || Math.Abs(clampedY - y) > 0.5f;
        }

        public static float ToGuiY(float screenYBottomOrigin, float screenHeight) =>
            screenHeight - screenYBottomOrigin;
    }

    /// <summary>
    /// IMGUI-font-safe ASCII labels (Unity's default skin has no ⌂ / ▲ / ●).
    /// Shape is the colored quad in the overlay; letters distinguish kinds.
    /// </summary>
    public static class ArMarkerDisplay
    {
        public static string Glyph(ArWaypointKind kind)
        {
            switch (kind)
            {
                case ArWaypointKind.Loco:
                    return "LOCO";
                case ArWaypointKind.Station:
                    return "STN";
                case ArWaypointKind.Pin:
                    return "PIN";
                default:
                    return "?";
            }
        }

        public static bool IsImguiFontSafe(string glyph)
        {
            if (string.IsNullOrEmpty(glyph))
            {
                return false;
            }

            for (var i = 0; i < glyph.Length; i++)
            {
                var c = glyph[i];
                if (c < 32 || c > 126)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
