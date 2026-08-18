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

        public const float EdgeDetectTolerancePixels = 2.5f;

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
        /// Mutates GuiX only. Capacity 3; no heap.
        /// </summary>
        public static void Apply(
            ArMarkerSlot[] slots,
            float screenWidth,
            float screenHeight = 0f,
            float edgeMargin = ArMarkerProjection.DefaultEdgeMarginPixels,
            float separationPixels = DefaultSeparationPixels,
            float hudBottomGuiY = 0f)
        {
            if (slots == null || slots.Length == 0)
            {
                return;
            }

            ApplySide(slots, ArHorizontalEdge.Left, screenWidth, screenHeight, edgeMargin, separationPixels, hudBottomGuiY);
            ApplySide(slots, ArHorizontalEdge.Right, screenWidth, screenHeight, edgeMargin, separationPixels, hudBottomGuiY);
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

        private static void ApplySide(
            ArMarkerSlot[] slots,
            ArHorizontalEdge side,
            float screenWidth,
            float screenHeight,
            float edgeMargin,
            float separationPixels,
            float hudBottomGuiY)
        {
            var i0 = -1;
            var i1 = -1;
            var i2 = -1;
            var k0 = 0f;
            var k1 = 0f;
            var k2 = 0f;
            var n = 0;
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

                if (n == 0)
                {
                    i0 = i;
                    k0 = slot.EdgeSortKey;
                }
                else if (n == 1)
                {
                    i1 = i;
                    k1 = slot.EdgeSortKey;
                }
                else if (n == 2)
                {
                    i2 = i;
                    k2 = slot.EdgeSortKey;
                }

                n++;
                if (n == 3)
                {
                    break;
                }
            }

            if (n < 2)
            {
                return;
            }

            var outermostX = side == ArHorizontalEdge.Left
                ? edgeMargin
                : Math.Max(edgeMargin, screenWidth - edgeMargin);
            var inward = side == ArHorizontalEdge.Left ? 1f : -1f;

            if (n == 2)
            {
                var first = IsMoreOuter(k0, i0, k1, i1) ? i0 : i1;
                var second = first == i0 ? i1 : i0;
                slots[first].GuiX = outermostX;
                slots[second].GuiX = outermostX + separationPixels * inward;
                return;
            }

            var first3 = i0;
            var kFirst = k0;
            if (IsMoreOuter(k1, i1, kFirst, first3))
            {
                first3 = i1;
                kFirst = k1;
            }

            if (IsMoreOuter(k2, i2, kFirst, first3))
            {
                first3 = i2;
            }

            int r0;
            int r1;
            float kr0;
            float kr1;
            if (first3 == i0)
            {
                r0 = i1;
                kr0 = k1;
                r1 = i2;
                kr1 = k2;
            }
            else if (first3 == i1)
            {
                r0 = i0;
                kr0 = k0;
                r1 = i2;
                kr1 = k2;
            }
            else
            {
                r0 = i0;
                kr0 = k0;
                r1 = i1;
                kr1 = k1;
            }

            var second3 = IsMoreOuter(kr0, r0, kr1, r1) ? r0 : r1;
            var third3 = second3 == r0 ? r1 : r0;
            slots[first3].GuiX = outermostX;
            slots[second3].GuiX = outermostX + separationPixels * inward;
            slots[third3].GuiX = outermostX + 2f * separationPixels * inward;
        }
    }
}
