using System;

namespace YardMasterSuite.Core
{
    /// <summary>Coupled consist size. Type A payload.</summary>
    public readonly struct ConsistSnapshot
    {
        public readonly int CarCount;
        public readonly int MassTonnes;

        public ConsistSnapshot(int carCount, int massTonnes)
        {
            CarCount = carCount;
            MassTonnes = massTonnes;
        }
    }

    public struct ConsistCache
    {
        public int CarCount;
        public int MassTonnes;
        public bool Seeded;
    }

    /// <summary>
    /// How the Unity listener should treat a loco presence change.
    /// </summary>
    public enum ConsistBindAction
    {
        /// <summary>Unboard or same loco: keep coupler binds and cache.</summary>
        KeepListening = 0,
        /// <summary>First board or a different loco: unbind, reset, bind the new car.</summary>
        BindNewLoco = 1,
    }

    /// <summary>
    /// Unity-free consist gate. Emits on first sample (board) and when cars or
    /// rounded tonnes change (couple / uncouple).
    /// </summary>
    public static class ConsistTopology
    {
        public static void Reset(ref ConsistCache cache)
        {
            cache = default;
        }

        /// <summary>
        /// Yard uncouple is on foot. Do not reset or drop binds on unboard.
        /// Reset only when the boarded loco instance changes.
        /// </summary>
        public static ConsistBindAction PrepareForLoco(
            int boardedLocoId,
            ref ConsistCache cache,
            ref int boundLocoId)
        {
            if (boardedLocoId == 0)
            {
                return ConsistBindAction.KeepListening;
            }

            if (boundLocoId == boardedLocoId)
            {
                return ConsistBindAction.KeepListening;
            }

            Reset(ref cache);
            boundLocoId = boardedLocoId;
            return ConsistBindAction.BindNewLoco;
        }

        public static string? Observe(int carCount, float massKg, ref ConsistCache cache)
        {
            if (carCount < 0)
            {
                carCount = 0;
            }

            var tonnes = (int)Math.Round(massKg / 1000.0);
            if (tonnes < 0)
            {
                tonnes = 0;
            }

            if (cache.Seeded && carCount == cache.CarCount && tonnes == cache.MassTonnes)
            {
                return null;
            }

            cache.Seeded = true;
            cache.CarCount = carCount;
            cache.MassTonnes = tonnes;
            return "T2 consist: cars=" + carCount + " t=" + tonnes;
        }
    }
}
