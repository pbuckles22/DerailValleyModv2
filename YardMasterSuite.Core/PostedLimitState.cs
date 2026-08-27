namespace YardMasterSuite.Core
{
    /// <summary>Posted Limit snapshot (sticky km/h, Next, last FoT roster size).</summary>
    public readonly struct PostedLimitSnapshot
    {
        public readonly float? Kmh;
        public readonly int RosterCount;
        public readonly float? NextKmh;
        public readonly float? NextAlongMeters;

        public PostedLimitSnapshot(
            float? kmh,
            int rosterCount,
            float? nextKmh = null,
            float? nextAlongMeters = null)
        {
            Kmh = kmh;
            RosterCount = rosterCount;
            NextKmh = nextKmh;
            NextAlongMeters = nextAlongMeters;
        }

        public static PostedLimitSnapshot None => default;
    }

    public struct PostedLimitCache
    {
        public int KmhRounded;
        public int RosterCount;
        public int NextRounded;
        public int NextBucket;
        public bool Seeded;
    }

    public static class PostedLimitTelemetry
    {
        public static bool Observe(
            in PostedLimitSnapshot snapshot,
            ref PostedLimitCache cache,
            out PostedLimitSnapshot published,
            float massTonnes = 40f)
        {
            published = snapshot;
            var kmh = snapshot.Kmh is float value
                ? Round(value)
                : -1;
            var next = snapshot.NextKmh is float n
                ? Round(n)
                : -1;
            var from = snapshot.Kmh ?? SpeedLimitState.UnrestrictedKmh;
            var wasShowing = cache.Seeded && cache.NextRounded == next && cache.NextBucket >= 0;
            var bucket = snapshot.NextKmh is float nk && snapshot.NextAlongMeters is float along
                ? NextLimitReveal.PublishBucket(along, from, nk, massTonnes, wasShowing)
                : -1;
            // RosterCount changes on silent RefillFrom — do not bust publish/HUD/log.
            if (cache.Seeded
                && cache.KmhRounded == kmh
                && cache.NextRounded == next
                && cache.NextBucket == bucket)
            {
                return false;
            }

            cache.Seeded = true;
            cache.KmhRounded = kmh;
            cache.RosterCount = snapshot.RosterCount;
            cache.NextRounded = next;
            cache.NextBucket = bucket;
            return true;
        }

        public static void Reset(ref PostedLimitCache cache) => cache = default;

        private static int Round(float value) =>
            (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    public static class PostedBoardTelemetry
    {
        public static string FormatFot(int rawCount, int parsedCount) =>
            "T2 boards fot: raw=" + rawCount.ToString() + " parsed=" + parsedCount.ToString();

        public static string FormatFiloWarm(
            string reason,
            int plusCount,
            int minusCount,
            int rawCount,
            int parsedCount,
            long fotMs) =>
            "T2 limit filo: warm · "
            + reason
            + " · plus="
            + plusCount.ToString()
            + " minus="
            + minusCount.ToString()
            + " raw="
            + rawCount.ToString()
            + " parsed="
            + parsedCount.ToString()
            + " fotMs="
            + fotMs.ToString();

        public static string FormatFiloReverse(int plusCount, int minusCount) =>
            "T2 limit filo: reverse swap · plus="
            + plusCount.ToString()
            + " minus="
            + minusCount.ToString();

        public static string FormatFiloLock(int activeCount) =>
            "T2 limit filo: direction lock · n=" + activeCount.ToString();

        public static string FormatFiloTake(float kmh, float alongMeters, string alongSrc) =>
            "T2 limit filo: take "
            + Round(kmh).ToString()
            + "@"
            + Round(alongMeters).ToString()
            + " src="
            + (string.IsNullOrEmpty(alongSrc) ? "—" : alongSrc);

        public static string FormatFiloPathRebuild(string reason, int hopCount, long fotMs) =>
            "T2 limit filo: path rebuild · "
            + reason
            + " · hops="
            + hopCount.ToString()
            + " fotMs="
            + fotMs.ToString();

        public static string FormatFiloAlongJump(float fromMeters, float toMeters) =>
            "T2 limit filo: along jump "
            + Round(fromMeters).ToString()
            + "→"
            + Round(toMeters).ToString();

        /// <summary>
        /// Hitch: only emit T2 limit-ahead when sticky/next km/h change — not every metre.
        /// </summary>
        public static bool ShouldLogAhead(
            float stickyKmh,
            float? nextKmh,
            float lastLoggedSticky,
            float lastLoggedNext)
        {
            var sticky = Round(stickyKmh);
            var next = nextKmh is float n ? Round(n) : -1;
            return sticky != Round(lastLoggedSticky) || next != Round(lastLoggedNext);
        }

        public static string FormatFiloHead(
            float? plusKmh,
            float plusAlongMeters,
            float? minusKmh,
            float minusAlongMeters)
        {
            var plus = plusKmh is float pk
                ? "+" + Round(pk).ToString() + "@" + Round(plusAlongMeters).ToString()
                : "+—";
            var minus = minusKmh is float mk
                ? "-" + Round(mk).ToString() + "@" + Round(minusAlongMeters).ToString()
                : "-—";
            return "T2 limit filo: head " + plus + " " + minus;
        }

        public static string FormatAhead(
            float stickyKmh,
            float speedKmh,
            float? nextKmh,
            float? nextAlongMeters,
            AheadBoard[] nearest,
            int nearestCount,
            float? skipKmh = null,
            float skipAlongMeters = 0f,
            string? skipReason = null,
            string? alongSrc = null)
        {
            var sb = StringBuilderPool.Shared.Rent();
            sb.Append("T2 limit-ahead: sticky=");
            sb.Append(Round(stickyKmh));
            sb.Append(" speed=");
            sb.Append(Round(speedKmh));
            if (nextKmh is float nk && nextAlongMeters is float along && along > 0f)
            {
                sb.Append(" next=");
                sb.Append(Round(nk));
                sb.Append(" ");
                sb.Append(Round(along));
                sb.Append("m");
            }
            else
            {
                sb.Append(" next=—");
            }

            sb.Append(" src=");
            sb.Append(string.IsNullOrEmpty(alongSrc) ? "—" : alongSrc);
            sb.Append(" n=");
            sb.Append(nearestCount);
            if (nearest != null)
            {
                var boards = nearest;
                var cap = boards.Length;
                if (cap > nearestCount)
                {
                    cap = nearestCount;
                }

                for (var i = 0; i < cap; i++)
                {
                    sb.Append(" ");
                    sb.Append(Round(boards[i].Kmh));
                    sb.Append("@");
                    sb.Append(Round(boards[i].AlongMeters));
                }
            }

            if (skipKmh is float sk && !string.IsNullOrEmpty(skipReason))
            {
                sb.Append(" skip=");
                sb.Append(Round(sk));
                sb.Append("@");
                sb.Append(Round(skipAlongMeters));
                sb.Append(" ");
                sb.Append(skipReason);
            }

            var text = sb.ToString();
            StringBuilderPool.Shared.Return(sb);
            return text;
        }

        public static int AheadFingerprint(
            float stickyKmh,
            float? nextKmh,
            float? nextAlongMeters,
            AheadBoard[] nearest,
            int nearestCount,
            float? skipKmh,
            float skipAlongMeters,
            string? skipReason,
            string? alongSrc = null)
        {
            unchecked
            {
                var h = Round(stickyKmh);
                h = (h * 397) ^ (nextKmh is float nk ? Round(nk) : -1);
                h = (h * 397) ^ (nextAlongMeters is float a ? Round(a) / 10 : -1);
                h = (h * 397) ^ nearestCount;
                if (nearest != null)
                {
                    var boards = nearest;
                    var cap = boards.Length;
                    if (cap > nearestCount)
                    {
                        cap = nearestCount;
                    }

                    for (var i = 0; i < cap; i++)
                    {
                        h = (h * 397) ^ Round(boards[i].Kmh);
                        h = (h * 397) ^ (Round(boards[i].AlongMeters) / 10);
                    }
                }

                h = (h * 397) ^ (skipKmh is float sk ? Round(sk) : -1);
                h = (h * 397) ^ (Round(skipAlongMeters) / 10);
                h = (h * 397) ^ (skipReason == null ? 0 : skipReason.GetHashCode());
                h = (h * 397) ^ (alongSrc == null ? 0 : alongSrc.GetHashCode());
                return h;
            }
        }

        public static string SkipReason(in SpeedLimitBoardFacing.Eval eval)
        {
            if (eval.Governs)
            {
                return string.Empty;
            }

            if (eval.ForwardDot > -SpeedLimitBoardFacing.MinForwardAlign)
            {
                return "away";
            }

            if (eval.TrackKnown && !eval.OnOurTrack)
            {
                return "track";
            }

            if (!eval.OnRight && !eval.TrackKnown)
            {
                return "left";
            }

            return "wide";
        }

        private static int Round(float value) =>
            (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }
}
