using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Posted Limit FILO (v1): soft-loaded exit corridors (≤ <see cref="MaxDepth"/>
    /// boards each way). Cab never FoT; town / Y / reverse events rebuild.
    /// </summary>
    public static class PostedLimitFilo
    {
        /// <summary>Cap per exit corridor (v1: Current + Next cushion).</summary>
        public const int MaxDepth = 5;

        /// <summary>Lock travel polarity once moving faster than this (drop opposite exit).</summary>
        public const float DirectionLockMinSpeedKmh = 5f;

        /// <summary>
        /// Sit physics jitter: boards slightly behind (along ≤ 0) still count as Next
        /// until pop (which requires direction lock + crawl). Stops n=0 ↔ refill publish churn.
        /// </summary>
        public const float SnapshotAlongMinMeters = -2f;

        public static bool ShouldLockDirection(float speedKmh) =>
            speedKmh > DirectionLockMinSpeedKmh;

        /// <summary>
        /// Sit freeze: no SetTravel/Tick chord math while crawling — physics
        /// vibration must not re-sort the funnel.
        /// </summary>
        public static bool ShouldFreezeAtStandstill(float speedKmh) =>
            speedKmh <= PostedStickyLimit.StandstillMaxSpeedKmh;

        /// <summary>Board is still "ahead" for Next snapshot despite tiny sit jitter.</summary>
        public static bool IsVisibleAlong(float alongMeters) =>
            alongMeters > SnapshotAlongMinMeters;

        /// <summary>
        /// Pop only after direction lock (travel polarity known) and above crawl.
        /// Prevents 1 km/h crawl takes before lock and sit churn.
        /// </summary>
        public static bool ShouldPopOnTick(float speedKmh, bool directionLocked) =>
            directionLocked && speedKmh > PostedStickyLimit.StandstillMaxSpeedKmh;

        /// <summary>
        /// Refill roster scan only after a pop freed a slot — not every cab frame.
        /// </summary>
        public static bool ShouldRefillAfterPop(
            int countBefore,
            int countAfter,
            int activeCapacity,
            int rosterCount) =>
            rosterCount > 0
            && countAfter < countBefore
            && countAfter < activeCapacity;

        /// <summary>
        /// Empty FoT is hitch-class (~100ms). Never FoT just because the funnel
        /// drained — refill from the last warm roster instead.
        /// </summary>
        public static bool ShouldEmptyFot() => false;

        /// <summary>
        /// Town FoT only when the polled yard is a real id that changed.
        /// Ignore SW→— flicker (null/empty poll) — that was clearing sticky to 120.
        /// </summary>
        public static bool ShouldRewarmForYard(string? previousYard, string? polledYard)
        {
            if (string.IsNullOrEmpty(polledYard))
            {
                return false;
            }

            return !string.Equals(previousYard, polledYard, StringComparison.Ordinal);
        }

        /// <summary>
        /// Split parsed boards into +travel exit vs −travel exit, nearest-first, capped.
        /// </summary>
        public static void PartitionExits(
            ParsedPostedBoard[] boards,
            float originX,
            float originY,
            float originZ,
            float forwardX,
            float forwardY,
            float forwardZ,
            out ParsedPostedBoard[] exitPlus,
            out ParsedPostedBoard[] exitMinus)
        {
            exitPlus = Array.Empty<ParsedPostedBoard>();
            exitMinus = Array.Empty<ParsedPostedBoard>();
            if (boards == null || boards.Length == 0)
            {
                return;
            }

            var plus = new Ranked[boards.Length];
            var minus = new Ranked[boards.Length];
            var plusN = 0;
            var minusN = 0;

            for (var i = 0; i < boards.Length; i++)
            {
                var b = boards[i];
                var dx = b.X - originX;
                var dy = b.Y - originY;
                var dz = b.Z - originZ;
                var along = (dx * forwardX) + (dy * forwardY) + (dz * forwardZ);
                if (along >= 0f)
                {
                    plus[plusN++] = new Ranked(along, b);
                }
                else
                {
                    minus[minusN++] = new Ranked(-along, b);
                }
            }

            exitPlus = TakeNearest(plus, plusN);
            exitMinus = TakeNearest(minus, minusN);
        }

        /// <summary>Pick the exit that matches current travel vs warm-time forward.</summary>
        public static ParsedPostedBoard[] SelectActiveExit(
            ParsedPostedBoard[] exitPlus,
            ParsedPostedBoard[] exitMinus,
            float warmForwardX,
            float warmForwardZ,
            float travelX,
            float travelZ)
        {
            var warmLen = Math.Sqrt((warmForwardX * warmForwardX) + (warmForwardZ * warmForwardZ));
            var travelLen = Math.Sqrt((travelX * travelX) + (travelZ * travelZ));
            if (warmLen < 1e-4 || travelLen < 1e-4)
            {
                return exitPlus ?? Array.Empty<ParsedPostedBoard>();
            }

            var dot = ((warmForwardX * travelX) + (warmForwardZ * travelZ)) / (warmLen * travelLen);
            return dot >= 0.0
                ? exitPlus ?? Array.Empty<ParsedPostedBoard>()
                : exitMinus ?? Array.Empty<ParsedPostedBoard>();
        }

        /// <summary>
        /// Sit / Y / station: scan both exits. After direction lock, travel side only.
        /// </summary>
        public static ParsedPostedBoard[] SelectScanSet(
            ParsedPostedBoard[] exitPlus,
            ParsedPostedBoard[] exitMinus,
            bool directionLocked,
            float warmForwardX,
            float warmForwardZ,
            float travelX,
            float travelZ)
        {
            if (!directionLocked)
            {
                return UnionExits(exitPlus, exitMinus);
            }

            return SelectActiveExit(
                exitPlus,
                exitMinus,
                warmForwardX,
                warmForwardZ,
                travelX,
                travelZ);
        }

        public static ParsedPostedBoard[] UnionExits(
            ParsedPostedBoard[] exitPlus,
            ParsedPostedBoard[] exitMinus)
        {
            var plus = exitPlus ?? Array.Empty<ParsedPostedBoard>();
            var minus = exitMinus ?? Array.Empty<ParsedPostedBoard>();
            if (minus.Length == 0)
            {
                return plus;
            }

            if (plus.Length == 0)
            {
                return minus;
            }

            var all = new ParsedPostedBoard[plus.Length + minus.Length];
            Array.Copy(plus, 0, all, 0, plus.Length);
            Array.Copy(minus, 0, all, plus.Length, minus.Length);
            return all;
        }

        public static float AlongMeters(
            float originX,
            float originY,
            float originZ,
            float forwardX,
            float forwardY,
            float forwardZ,
            ParsedPostedBoard board)
        {
            var dx = board.X - originX;
            var dy = board.Y - originY;
            var dz = board.Z - originZ;
            return (dx * forwardX) + (dy * forwardY) + (dz * forwardZ);
        }

        private static ParsedPostedBoard[] TakeNearest(Ranked[] ranked, int count)
        {
            if (count <= 0)
            {
                return Array.Empty<ParsedPostedBoard>();
            }

            Array.Sort(ranked, 0, count, RankedComparer.Instance);
            var n = count < MaxDepth ? count : MaxDepth;
            var result = new ParsedPostedBoard[n];
            for (var i = 0; i < n; i++)
            {
                result[i] = ranked[i].Board;
            }

            return result;
        }

        private readonly struct Ranked
        {
            public Ranked(float distance, ParsedPostedBoard board)
            {
                Distance = distance;
                Board = board;
            }

            public float Distance { get; }
            public ParsedPostedBoard Board { get; }
        }

        private sealed class RankedComparer : System.Collections.Generic.IComparer<Ranked>
        {
            public static readonly RankedComparer Instance = new RankedComparer();

            public int Compare(Ranked x, Ranked y) => x.Distance.CompareTo(y.Distance);
        }
    }
}
