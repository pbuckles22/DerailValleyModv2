using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Posted Limit sticky: a restriction is released only by passing a new
    /// board, never by a looser board becoming nearest-behind (v1 0.5.50).
    /// </summary>
    public static class PostedStickyLimit
    {
        public const float StandstillMaxSpeedKmh = 0.5f;
        public const float ReverseDot = -0.2f;

        public static float? Resolve(float? sticky, float? takenKmh, float? seedKmh) =>
            Resolve(sticky, takenKmh, seedKmh, speedKmh: 0f);

        /// <summary>
        /// Seed (nearest behind) is cold-start only — standstill. Rolling trains
        /// wait for a take so a board they did not pass cannot move Limit.
        /// </summary>
        public static float? Resolve(float? sticky, float? takenKmh, float? seedKmh, float speedKmh)
        {
            if (takenKmh is float taken)
            {
                return taken;
            }

            if (sticky is float held)
            {
                return held;
            }

            if (speedKmh > StandstillMaxSpeedKmh)
            {
                return null;
            }

            return seedKmh;
        }

        public static bool ShouldClearForReverse(
            float speedKmh,
            float stickyTravelX,
            float stickyTravelZ,
            float travelX,
            float travelZ)
        {
            if (speedKmh <= StandstillMaxSpeedKmh)
            {
                return false;
            }

            if (!TryNormalize(stickyTravelX, stickyTravelZ, out var sx, out var sz))
            {
                return false;
            }

            if (!TryNormalize(travelX, travelZ, out var tx, out var tz))
            {
                return false;
            }

            return (sx * tx) + (sz * tz) < ReverseDot;
        }

        private static bool TryNormalize(float x, float z, out float nx, out float nz)
        {
            var len = (float)System.Math.Sqrt((x * x) + (z * z));
            if (len < 1e-4f)
            {
                nx = nz = 0f;
                return false;
            }

            nx = x / len;
            nz = z / len;
            return true;
        }
    }

    /// <summary>
    /// Detects the ahead → behind transition that means "we just passed this board".
    /// </summary>
    public sealed class BoardTakeDetector
    {
        private readonly Dictionary<int, bool> _wasAhead = new Dictionary<int, bool>();

        public float? Observe(int boardId, float kmh, float alongMeters)
        {
            var ahead = alongMeters > 0f;
            var seenAhead = _wasAhead.TryGetValue(boardId, out var wasAhead) && wasAhead;
            _wasAhead[boardId] = ahead;
            return !ahead && seenAhead ? kmh : (float?)null;
        }

        public void Reset() => _wasAhead.Clear();
    }
}
