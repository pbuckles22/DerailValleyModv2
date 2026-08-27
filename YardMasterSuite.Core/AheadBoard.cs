using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>One governing board ahead of the loco (km/h + along-track meters).</summary>
    public readonly struct AheadBoard
    {
        public AheadBoard(float kmh, float alongMeters, string alongSrc = "path")
        {
            Kmh = kmh;
            AlongMeters = alongMeters;
            AlongSrc = alongSrc;
        }

        public float Kmh { get; }
        public float AlongMeters { get; }
        public string AlongSrc { get; }
    }

    /// <summary>Look-ahead helpers for Posted Limit + Next (6.10).</summary>
    public static class AheadBoards
    {
        public const int DiagnosticCap = 4;

        /// <summary>Nearest board ahead whose number differs from <paramref name="fromKmh"/>.</summary>
        public static AheadBoard? NextDifferent(float? fromKmh, IReadOnlyList<AheadBoard>? aheadBoards)
        {
            if (fromKmh is not float from || aheadBoards == null)
            {
                return null;
            }

            var fromWhole = (int)Math.Round(from, MidpointRounding.AwayFromZero);
            AheadBoard? best = null;
            for (var i = 0; i < aheadBoards.Count; i++)
            {
                var board = aheadBoards[i];
                if (board.AlongMeters <= 0f)
                {
                    continue;
                }

                var whole = (int)Math.Round(board.Kmh, MidpointRounding.AwayFromZero);
                if (whole == fromWhole)
                {
                    continue;
                }

                if (best is null || board.AlongMeters < best.Value.AlongMeters)
                {
                    best = board;
                }
            }

            return best;
        }

        /// <summary>Nearest-first along the route (not FILO). Writes up to dest.Length, cap 4.</summary>
        public static int CopyNearest(IReadOnlyList<AheadBoard>? src, AheadBoard[] dest)
        {
            if (src == null || dest == null || dest.Length == 0)
            {
                return 0;
            }

            var cap = dest.Length > DiagnosticCap ? DiagnosticCap : dest.Length;
            var filled = 0;
            for (var i = 0; i < src.Count; i++)
            {
                var board = src[i];
                if (board.AlongMeters <= 0f)
                {
                    continue;
                }

                if (filled < cap)
                {
                    dest[filled] = board;
                    BubbleLeft(dest, filled);
                    filled++;
                    continue;
                }

                if (board.AlongMeters >= dest[filled - 1].AlongMeters)
                {
                    continue;
                }

                dest[filled - 1] = board;
                BubbleLeft(dest, filled - 1);
            }

            return filled;
        }

        private static void BubbleLeft(AheadBoard[] dest, int index)
        {
            var i = index;
            while (i > 0 && dest[i].AlongMeters < dest[i - 1].AlongMeters)
            {
                var tmp = dest[i];
                dest[i] = dest[i - 1];
                dest[i - 1] = tmp;
                i--;
            }
        }
    }
}
