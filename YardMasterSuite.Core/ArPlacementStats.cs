namespace YardMasterSuite.Core
{
    /// <summary>
    /// Windowed AR placement counts. Hot path is int increments; one T2 line per window.
    /// </summary>
    public struct ArPlacementHistogram
    {
        public int Object;
        public int EdgeMid;
        public int EdgeTop;
        public int Hidden;
        public float WindowStartAt;

        public int Samples => Object + EdgeMid + EdgeTop + Hidden;
    }

    public static class ArPlacementStats
    {
        public static void Record(
            ArMarkerSlot[] slots,
            float screenHeight,
            float now,
            ref ArPlacementHistogram hist)
        {
            if (slots == null)
            {
                return;
            }

            if (hist.Samples == 0)
            {
                hist.WindowStartAt = now;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                switch (ArEdgeBanding.Classify(in slots[i], screenHeight))
                {
                    case ArEdgeBand.Object:
                        hist.Object++;
                        break;
                    case ArEdgeBand.Top:
                        hist.EdgeTop++;
                        break;
                    case ArEdgeBand.Mid:
                    case ArEdgeBand.Bottom:
                        hist.EdgeMid++;
                        break;
                    default:
                        hist.Hidden++;
                        break;
                }
            }
        }

        public static string? MaybeSummary(float now, bool force, ref ArPlacementHistogram hist)
        {
            if (hist.Samples == 0)
            {
                return null;
            }

            if (!force && now - hist.WindowStartAt < GcCadence.SummaryIntervalSeconds)
            {
                return null;
            }

            var line = FormatSummary(in hist);
            hist = default;
            return line;
        }

        public static string FormatSummary(in ArPlacementHistogram hist)
        {
            var sb = StringBuilderPool.Shared.Rent();
            sb.Append("T2 ar-summary: n=");
            sb.Append(hist.Samples);
            sb.Append(" object=");
            sb.Append(hist.Object);
            sb.Append(" edgeMid=");
            sb.Append(hist.EdgeMid);
            sb.Append(" edgeTop=");
            sb.Append(hist.EdgeTop);
            sb.Append(" hidden=");
            sb.Append(hist.Hidden);
            var text = sb.ToString();
            StringBuilderPool.Shared.Return(sb);
            return text;
        }
    }
}
