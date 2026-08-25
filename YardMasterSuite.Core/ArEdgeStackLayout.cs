using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Fan sticky markers that share a left/right edge so they do not sit on one pixel.
    /// Outermost = furthest from camera center; then step inward. Y is the sticky row
    /// under the HUD (6.4); this is not a top-of-screen slide.
    /// </summary>
    public static class ArEdgeStackLayout
    {
        /// <summary>Clears 64 px IMGUI labels (v1 used 40 px for smaller chips).</summary>
        public const float DefaultSeparationPixels = 72f;

        /// <summary>Gap between occupancy boxes (end + pad + start).</summary>
        public const float CaptionPadPixels = 8f;

        public const float EdgeDetectTolerancePixels = 2.5f;

        public const float EstimatePixelsPerChar = 9f;

        /// <summary>Wider of icon and caption, no plate chrome.</summary>
        public static float InnerOccupancyWidthPixels(float iconPixels, float captionWidthPixels)
        {
            var icon = iconPixels < 0f ? 0f : iconPixels;
            var cap = captionWidthPixels < 0f ? 0f : captionWidthPixels;
            return icon > cap ? icon : cap;
        }

        /// <summary>Plate width: inner occupancy plus dark-plate chrome.</summary>
        public static float OccupancyWidthPixels(float iconPixels, float captionWidthPixels)
        {
            var inner = InnerOccupancyWidthPixels(iconPixels, captionWidthPixels);
            return inner <= 0f ? 0f : inner + ArMarkerPlate.HorizontalChromePixels;
        }

        /// <summary>Icon-center distance so occupancy A ends, then pad, then B starts.</summary>
        public static float CenterSeparationPixels(
            float occupancyA,
            float occupancyB,
            float padPixels = CaptionPadPixels)
        {
            var pad = padPixels < 0f ? 0f : padPixels;
            return (occupancyA * 0.5f) + (occupancyB * 0.5f) + pad;
        }

        public static float OutermostCenterX(
            ArHorizontalEdge side,
            float edgeMargin,
            float screenWidth,
            float occupancy)
        {
            var half = occupancy < 0f ? 0f : occupancy * 0.5f;
            if (side == ArHorizontalEdge.Right)
            {
                var rightX = Math.Max(edgeMargin, screenWidth - edgeMargin);
                var inset = screenWidth - half;
                return inset < rightX ? inset : rightX;
            }

            return half > edgeMargin ? half : edgeMargin;
        }

        public static float EstimateCaptionWidthPixels(
            string? text,
            float pixelsPerChar = EstimatePixelsPerChar)
        {
            if (string.IsNullOrEmpty(text) || pixelsPerChar <= 0f)
            {
                return 0f;
            }

            var max = 0;
            var run = 0;
            for (var i = 0; i < text!.Length; i++)
            {
                if (text[i] == '\n')
                {
                    if (run > max)
                    {
                        max = run;
                    }

                    run = 0;
                    continue;
                }

                run++;
            }

            if (run > max)
            {
                max = run;
            }

            return max * pixelsPerChar;
        }

        public static float CaptionSeparationPixels(ArWaypointKind a, ArWaypointKind b)
        {
            var icon = ArMarkerDisplay.IconPixels;
            return CenterSeparationPixels(
                OccupancyWidthPixels(icon, ArMarkerDisplay.LabelWidthPixels(a)),
                OccupancyWidthPixels(icon, ArMarkerDisplay.LabelWidthPixels(b)));
        }

        public static bool CaptionsOverlap(float guiX0, float width0, float guiX1, float width1)
        {
            var left0 = guiX0 - (width0 * 0.5f);
            var right0 = guiX0 + (width0 * 0.5f);
            var left1 = guiX1 - (width1 * 0.5f);
            var right1 = guiX1 + (width1 * 0.5f);
            return left0 < right1 && left1 < right0;
        }

        public static ArHorizontalEdge DetectEdge(
            float screenX,
            float screenWidth,
            float edgeMargin,
            float tolerancePixels = EdgeDetectTolerancePixels)
        {
            if (Math.Abs(screenX - edgeMargin) <= tolerancePixels)
            {
                return ArHorizontalEdge.Left;
            }

            var rightX = Math.Max(edgeMargin, screenWidth - edgeMargin);
            if (Math.Abs(screenX - rightX) <= tolerancePixels)
            {
                return ArHorizontalEdge.Right;
            }

            return ArHorizontalEdge.None;
        }

        /// <summary>
        /// Higher key = more extreme outward on this edge (left: more negative bearing;
        /// right: more positive).
        /// </summary>
        public static float OutwardSortKey(ArHorizontalEdge edge, float behindBearingRadians)
        {
            switch (edge)
            {
                case ArHorizontalEdge.Left:
                    return -behindBearingRadians;
                case ArHorizontalEdge.Right:
                    return behindBearingRadians;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Write stacked X positions. Highest sort key → outermost; then step inward.
        /// Stable tie-break: lower index wins the outward slot. n ≤ 31, no heap.
        /// </summary>
        public static void AssignStackedXs(
            ArHorizontalEdge edge,
            float outermostX,
            float separationPixels,
            float[] sortKeys,
            float[] outXs)
        {
            if (sortKeys == null || outXs == null)
            {
                throw new ArgumentNullException(sortKeys == null ? nameof(sortKeys) : nameof(outXs));
            }

            var n = sortKeys.Length;
            if (outXs.Length < n)
            {
                throw new ArgumentException("outXs shorter than sortKeys.", nameof(outXs));
            }

            if (edge == ArHorizontalEdge.None || n == 0)
            {
                for (var i = 0; i < n; i++)
                {
                    outXs[i] = outermostX;
                }

                return;
            }

            var inward = edge == ArHorizontalEdge.Left ? 1f : -1f;
            var used = 0;
            for (var slot = 0; slot < n; slot++)
            {
                var best = -1;
                for (var i = 0; i < n; i++)
                {
                    if ((used & (1 << i)) != 0)
                    {
                        continue;
                    }

                    if (best < 0 || IsMoreOuter(i, best, sortKeys))
                    {
                        best = i;
                    }
                }

                used |= 1 << best;
                outXs[best] = outermostX + slot * separationPixels * inward;
            }
        }

        /// <summary>
        /// After all slots are projected: fan any two+ Edge markers that share a side.
        /// Mutates GuiX only. n ≤ 16; no heap.
        /// </summary>
        public static void Apply(
            ArMarkerSlot[] slots,
            float screenWidth,
            float screenHeight = 0f,
            float edgeMargin = ArMarkerProjection.DefaultEdgeMarginPixels,
            float separationPixels = DefaultSeparationPixels,
            float hudBottomGuiY = 0f,
            float iconPixels = ArMarkerDisplay.IconPixels,
            float[]? captionWidths = null)
        {
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            ApplySide(
                slots,
                ArHorizontalEdge.Left,
                screenWidth,
                screenHeight,
                edgeMargin,
                separationPixels,
                hudBottomGuiY,
                iconPixels,
                captionWidths);
            ApplySide(
                slots,
                ArHorizontalEdge.Right,
                screenWidth,
                screenHeight,
                edgeMargin,
                separationPixels,
                hudBottomGuiY,
                iconPixels,
                captionWidths);
        }

        private static bool IsMoreOuter(int a, int b, float[] sortKeys)
        {
            var cmp = sortKeys[a].CompareTo(sortKeys[b]);
            if (cmp != 0)
            {
                return cmp > 0;
            }

            return a < b;
        }

        private static bool IsMoreOuter(float keyA, int indexA, float keyB, int indexB)
        {
            var cmp = keyA.CompareTo(keyB);
            if (cmp != 0)
            {
                return cmp > 0;
            }

            return indexA < indexB;
        }

        private static float CaptionWidthAt(
            ArMarkerSlot[] slots,
            int index,
            float[]? captionWidths)
        {
            if (captionWidths != null && index >= 0 && index < captionWidths.Length)
            {
                var w = captionWidths[index];
                if (w > 0f)
                {
                    return w;
                }
            }

            return ArMarkerDisplay.LabelWidthPixels(slots[index].Kind);
        }

        private static void ApplySide(
            ArMarkerSlot[] slots,
            ArHorizontalEdge side,
            float screenWidth,
            float screenHeight,
            float edgeMargin,
            float separationPixels,
            float hudBottomGuiY,
            float iconPixels,
            float[]? captionWidths)
        {
            _ = separationPixels;
            var fan = default(EdgeFan);
            for (var i = 0; i < slots.Length; i++)
            {
                ref var slot = ref slots[i];
                if (!slot.Occupied || slot.Place != ArMarkerPlace.Edge)
                {
                    continue;
                }

                if (screenHeight > 0f)
                {
                    var band = ArEdgeBanding.ClassifyGuiY(slot.GuiY, screenHeight, hudBottomGuiY);
                    if (band != ArEdgeBand.Mid)
                    {
                        continue;
                    }
                }

                var edge = slot.Edge;
                if (edge == ArHorizontalEdge.None)
                {
                    edge = DetectEdge(slot.GuiX, screenWidth, edgeMargin);
                }

                if (edge != side)
                {
                    continue;
                }

                if (!fan.TryAdd(i, slot.EdgeSortKey))
                {
                    break;
                }
            }

            if (fan.Count < 1)
            {
                return;
            }

            var inward = side == ArHorizontalEdge.Left ? 1f : -1f;
            var used = 0;
            var havePrev = false;
            var prevX = 0f;
            var prevOcc = 0f;
            for (var slot = 0; slot < fan.Count; slot++)
            {
                var best = -1;
                for (var j = 0; j < fan.Count; j++)
                {
                    if ((used & (1 << j)) != 0)
                    {
                        continue;
                    }

                    if (best < 0
                        || IsMoreOuter(fan.KeyAt(j), fan.IndexAt(j), fan.KeyAt(best), fan.IndexAt(best)))
                    {
                        best = j;
                    }
                }

                used |= 1 << best;
                var index = fan.IndexAt(best);
                var occ = OccupancyWidthPixels(
                    iconPixels,
                    CaptionWidthAt(slots, index, captionWidths));
                if (!havePrev)
                {
                    slots[index].GuiX = OutermostCenterX(side, edgeMargin, screenWidth, occ);
                    havePrev = true;
                }
                else
                {
                    var step = CenterSeparationPixels(prevOcc, occ);
                    slots[index].GuiX = prevX + (step * inward);
                }

                prevX = slots[index].GuiX;
                prevOcc = occ;
            }
        }

        /// <summary>Up to 16 mid-edge markers on one side (STN/LOCO/PIN + radar + job cars).</summary>
        private struct EdgeFan
        {
            public const int Capacity = 16;

            public int Count;
            private int _i0;
            private int _i1;
            private int _i2;
            private int _i3;
            private int _i4;
            private int _i5;
            private int _i6;
            private int _i7;
            private int _i8;
            private int _i9;
            private int _i10;
            private int _i11;
            private int _i12;
            private int _i13;
            private int _i14;
            private int _i15;
            private float _k0;
            private float _k1;
            private float _k2;
            private float _k3;
            private float _k4;
            private float _k5;
            private float _k6;
            private float _k7;
            private float _k8;
            private float _k9;
            private float _k10;
            private float _k11;
            private float _k12;
            private float _k13;
            private float _k14;
            private float _k15;

            public bool TryAdd(int index, float key)
            {
                if (Count >= Capacity)
                {
                    return false;
                }

                Set(Count, index, key);
                Count++;
                return true;
            }

            public int IndexAt(int n)
            {
                switch (n)
                {
                    case 0: return _i0;
                    case 1: return _i1;
                    case 2: return _i2;
                    case 3: return _i3;
                    case 4: return _i4;
                    case 5: return _i5;
                    case 6: return _i6;
                    case 7: return _i7;
                    case 8: return _i8;
                    case 9: return _i9;
                    case 10: return _i10;
                    case 11: return _i11;
                    case 12: return _i12;
                    case 13: return _i13;
                    case 14: return _i14;
                    default: return _i15;
                }
            }

            public float KeyAt(int n)
            {
                switch (n)
                {
                    case 0: return _k0;
                    case 1: return _k1;
                    case 2: return _k2;
                    case 3: return _k3;
                    case 4: return _k4;
                    case 5: return _k5;
                    case 6: return _k6;
                    case 7: return _k7;
                    case 8: return _k8;
                    case 9: return _k9;
                    case 10: return _k10;
                    case 11: return _k11;
                    case 12: return _k12;
                    case 13: return _k13;
                    case 14: return _k14;
                    default: return _k15;
                }
            }

            private void Set(int n, int index, float key)
            {
                switch (n)
                {
                    case 0: _i0 = index; _k0 = key; return;
                    case 1: _i1 = index; _k1 = key; return;
                    case 2: _i2 = index; _k2 = key; return;
                    case 3: _i3 = index; _k3 = key; return;
                    case 4: _i4 = index; _k4 = key; return;
                    case 5: _i5 = index; _k5 = key; return;
                    case 6: _i6 = index; _k6 = key; return;
                    case 7: _i7 = index; _k7 = key; return;
                    case 8: _i8 = index; _k8 = key; return;
                    case 9: _i9 = index; _k9 = key; return;
                    case 10: _i10 = index; _k10 = key; return;
                    case 11: _i11 = index; _k11 = key; return;
                    case 12: _i12 = index; _k12 = key; return;
                    case 13: _i13 = index; _k13 = key; return;
                    case 14: _i14 = index; _k14 = key; return;
                    default: _i15 = index; _k15 = key; return;
                }
            }
        }
    }
}
