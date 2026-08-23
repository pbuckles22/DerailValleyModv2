namespace YardMasterSuite.Core
{
    public enum ArMarkerPlace
    {
        Hidden = 0,
        OnObject = 1,
        Edge = 2,
    }

    public struct ArMarkerSlot
    {
        public bool Occupied;
        public ArWaypointKind Kind;
        public float GuiX;
        public float GuiY;
        public ArMarkerPlace Place;
        public int DistanceMeters;
        public ArHorizontalEdge Edge;
        public float EdgeSortKey;
    }

    /// <summary>
    /// Fixed 3-slot overlay (loco / office / pin). Hide = move off-screen.
    /// Never grow, Instantiate, or Destroy per marker.
    /// </summary>
    public static class ArMarkerBuffer
    {
        public const int Capacity = 3;
        public const float OffScreenX = -4096f;
        public const float OffScreenY = -4096f;

        public static int SlotOf(ArWaypointKind kind) => (int)kind;

        public static ArMarkerSlot[] Create()
        {
            var slots = new ArMarkerSlot[Capacity];
            for (var i = 0; i < Capacity; i++)
            {
                Hide(ref slots[i]);
            }

            return slots;
        }

        public static void Hide(ref ArMarkerSlot slot)
        {
            slot.Occupied = false;
            slot.Place = ArMarkerPlace.Hidden;
            slot.GuiX = OffScreenX;
            slot.GuiY = OffScreenY;
            slot.DistanceMeters = 0;
            slot.Edge = ArHorizontalEdge.None;
            slot.EdgeSortKey = 0f;
        }

        public static void Show(
            ref ArMarkerSlot slot,
            ArWaypointKind kind,
            float guiX,
            float guiY,
            ArMarkerPlace place,
            int distanceMeters,
            ArHorizontalEdge edge = ArHorizontalEdge.None,
            float edgeSortKey = 0f)
        {
            slot.Occupied = true;
            slot.Kind = kind;
            slot.GuiX = guiX;
            slot.GuiY = guiY;
            slot.Place = place;
            slot.DistanceMeters = distanceMeters;
            slot.Edge = place == ArMarkerPlace.Edge ? edge : ArHorizontalEdge.None;
            slot.EdgeSortKey = place == ArMarkerPlace.Edge ? edgeSortKey : 0f;
        }

        public static bool ShouldDrawSlot(in ArMarkerSlot slot) =>
            slot.Occupied && slot.Place != ArMarkerPlace.Hidden;

        public static ArOverlaySnapshot Snapshot(ArMarkerSlot[] slots)
        {
            return new ArOverlaySnapshot(
                PlaceOf(slots, ArWaypointKind.Loco),
                PlaceOf(slots, ArWaypointKind.Station),
                PlaceOf(slots, ArWaypointKind.Pin));
        }

        private static ArMarkerPlace PlaceOf(ArMarkerSlot[] slots, ArWaypointKind kind)
        {
            var i = SlotOf(kind);
            if (i < 0 || i >= slots.Length)
            {
                return ArMarkerPlace.Hidden;
            }

            var slot = slots[i];
            return slot.Occupied ? slot.Place : ArMarkerPlace.Hidden;
        }
    }

    /// <summary>AR draws only in a world session (same gate as the HUD shell).</summary>
    public static class ArOverlay
    {
        public static bool ShouldDraw(bool playerTransformPresent) =>
            ShouldDraw(playerTransformPresent, worldReady: true, screenOverlayOpen: false);

        /// <summary>
        /// Hide AR on the launcher, while the world is still loading, and on any
        /// pause/save/menu overlay (HUD bars may stay).
        /// </summary>
        public static bool ShouldDraw(
            bool playerTransformPresent,
            bool worldReady,
            bool screenOverlayOpen) =>
            HudWorldSession.IsActive(playerTransformPresent)
            && worldReady
            && !screenOverlayOpen;
    }

        /// <summary>
        /// Unity-free placement after WorldToScreenPoint. Behind or off-FOV →
        /// horizontal mid-edge. Object vs edge uses pixel hysteresis so look-around
        /// does not chatter (v1 hitch mode: log + flip every frame).
        /// </summary>
        public static class ArMarkerPlacement
        {
            public const float PlaceHysteresisPixels = 48f;

            public static void Resolve(
                float viewForward,
                float viewRight,
                float screenX,
                float screenY,
                float screenZ,
                float screenWidth,
                float screenHeight,
                bool wasBehind,
                ArHorizontalEdge previousEdge,
                ArMarkerPlace previousPlace,
                out bool behind,
                out ArMarkerPlace place,
                out float guiX,
                out float guiY,
                out ArHorizontalEdge edge)
            {
                behind = ArMarkerProjection.IsBehindCameraHysteresis(viewForward, wasBehind);
                var margin = ArMarkerProjection.DefaultEdgeMarginPixels;
                var sx = screenX;
                var sy = screenY;

                if (behind)
                {
                    edge = ArEdgeHysteresis.Resolve(viewRight, viewForward, previousEdge);
                    ArMarkerProjection.ApplyBehindCameraHorizontalEdge(
                        true, edge, screenWidth, screenHeight, margin, ref sx, ref sy);
                    place = ArMarkerPlace.Edge;
                    guiX = sx;
                    guiY = ArMarkerProjection.ToGuiY(sy, screenHeight);
                    return;
                }

                var inflate = 0f;
                if (previousPlace == ArMarkerPlace.OnObject)
                {
                    inflate = PlaceHysteresisPixels;
                }
                else if (previousPlace == ArMarkerPlace.Edge)
                {
                    inflate = -PlaceHysteresisPixels;
                }

                if (ArMarkerProjection.ShouldPlaceOnObject(
                        false, screenZ, screenX, screenY, screenWidth, screenHeight, inflate))
                {
                    edge = ArHorizontalEdge.None;
                    place = ArMarkerPlace.OnObject;
                    guiX = screenX;
                    guiY = ArMarkerProjection.ToGuiY(screenY, screenHeight);
                    return;
                }

                edge = ArEdgeHysteresis.Resolve(viewRight, viewForward, previousEdge);
                ArMarkerProjection.ApplyBehindCameraHorizontalEdge(
                    true, edge, screenWidth, screenHeight, margin, ref sx, ref sy);
                place = ArMarkerPlace.Edge;
                guiX = sx;
                guiY = ArMarkerProjection.ToGuiY(sy, screenHeight);
            }
        }
}
