namespace YardMasterSuite.Core
{
    /// <summary>Occupancy + place for the three Display Shell kinds. Type A / T2 fields.</summary>
    public readonly struct ArOverlaySnapshot
    {
        public readonly ArMarkerPlace Loco;
        public readonly ArMarkerPlace Station;
        public readonly ArMarkerPlace Pin;

        public ArOverlaySnapshot(ArMarkerPlace loco, ArMarkerPlace station, ArMarkerPlace pin)
        {
            Loco = loco;
            Station = station;
            Pin = pin;
        }

        public bool Equals(in ArOverlaySnapshot other) =>
            Loco == other.Loco && Station == other.Station && Pin == other.Pin;
    }

    /// <summary>
    /// Discrete Player.log lines for AR. Quiet on camera tick — logs init and
    /// occupancy/place changes only. Look-around T2 change lines are throttled
    /// (same 2 s gate as heading) so UMM logging cannot hitch-tax a sweep.
    /// </summary>
    public static class ArTelemetry
    {
        public const float MinChangeLogSeconds = 2f;

        public static string? NextLog(ArOverlaySnapshot? previous, in ArOverlaySnapshot current)
        {
            var last = -999f;
            return NextLog(previous, in current, nowSeconds: 0f, ref last);
        }

        public static string? NextLog(
            ArOverlaySnapshot? previous,
            in ArOverlaySnapshot current,
            float nowSeconds,
            ref float lastChangeLogAt)
        {
            if (previous is null)
            {
                lastChangeLogAt = nowSeconds;
                return "T2 ar init: " + Format(in current);
            }

            var prior = previous.Value;
            if (prior.Equals(in current))
            {
                return null;
            }

            if (nowSeconds - lastChangeLogAt < MinChangeLogSeconds)
            {
                return null;
            }

            lastChangeLogAt = nowSeconds;
            return "T2 ar change: " + Format(in current);
        }

        public static string Format(in ArOverlaySnapshot snap)
        {
            var sb = StringBuilderPool.Shared.Rent();
            sb.Append("loco=");
            AppendPlace(sb, snap.Loco);
            sb.Append(" office=");
            AppendPlace(sb, snap.Station);
            sb.Append(" pin=");
            AppendPlace(sb, snap.Pin);
            var text = sb.ToString();
            StringBuilderPool.Shared.Return(sb);
            return text;
        }

        private static void AppendPlace(System.Text.StringBuilder sb, ArMarkerPlace place)
        {
            switch (place)
            {
                case ArMarkerPlace.OnObject:
                    sb.Append("object");
                    break;
                case ArMarkerPlace.Edge:
                    sb.Append("edge");
                    break;
                default:
                    sb.Append("—");
                    break;
            }
        }
    }
}
