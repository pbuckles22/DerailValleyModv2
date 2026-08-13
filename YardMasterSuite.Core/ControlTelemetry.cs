using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Cab lever positions 0–1 (stack struct, zero alloc on the bus).
    /// Engine brake is optional per loco (<see cref="HasEngineBrake"/>).
    /// </summary>
    public readonly struct CabControlsState
    {
        public readonly float Throttle;
        public readonly float IndyBrake;
        public readonly float TrainBrake;
        public readonly float EngineBrake;
        public readonly float Reverser;
        public readonly bool HasEngineBrake;

        public CabControlsState(
            float throttle,
            float indyBrake,
            float trainBrake,
            float engineBrake,
            bool hasEngineBrake,
            float reverser)
        {
            Throttle = throttle;
            IndyBrake = indyBrake;
            TrainBrake = trainBrake;
            EngineBrake = engineBrake;
            HasEngineBrake = hasEngineBrake;
            Reverser = reverser;
        }
    }

    /// <summary>Last published lever percents. Seeded = first sample already taken.</summary>
    public struct ControlLeversCache
    {
        public int ThrottlePct;
        public int IndyPct;
        public int TrainPct;
        public int EnginePct;
        public int ReverserPct;
        public bool EnginePresent;
        public bool Seeded;
    }

    /// <summary>
    /// Unity-free lever gate. Publishes only when a rounded percent changes.
    /// T2 names each brake so smoke can tell train / indy / engine apart.
    /// </summary>
    public static class ControlTelemetry
    {
        public static void Reset(ref ControlLeversCache cache)
        {
            cache = default;
        }

        /// <summary>
        /// First call seeds and returns null. Later calls return a T2 line when
        /// any lever percent changes. Allocates only on a real move.
        /// </summary>
        public static string? Observe(
            float throttle,
            float indy,
            float train,
            float engine,
            bool enginePresent,
            float reverser,
            ref ControlLeversCache cache)
        {
            var t = ToPct(throttle);
            var i = ToPct(indy);
            var tr = ToPct(train);
            var e = enginePresent ? ToPct(engine) : 0;
            var r = ToPct(reverser);
            if (!cache.Seeded)
            {
                cache.ThrottlePct = t;
                cache.IndyPct = i;
                cache.TrainPct = tr;
                cache.EnginePct = e;
                cache.ReverserPct = r;
                cache.EnginePresent = enginePresent;
                cache.Seeded = true;
                return null;
            }

            if (t == cache.ThrottlePct
                && i == cache.IndyPct
                && tr == cache.TrainPct
                && r == cache.ReverserPct
                && enginePresent == cache.EnginePresent
                && (!enginePresent || e == cache.EnginePct))
            {
                return null;
            }

            cache.ThrottlePct = t;
            cache.IndyPct = i;
            cache.TrainPct = tr;
            cache.EnginePct = e;
            cache.ReverserPct = r;
            cache.EnginePresent = enginePresent;
            var engPct = enginePresent ? e.ToString() : "na";
            var engRaw = enginePresent ? FormatRaw(engine) : "-";
            return "T2 controls: thr=" + t
                + " indy=" + i
                + " train=" + tr
                + " eng=" + engPct
                + " rev=" + r
                + " raw=" + FormatRaw(throttle)
                + "," + FormatRaw(indy)
                + "," + FormatRaw(train)
                + "," + engRaw
                + "," + FormatRaw(reverser);
        }

        internal static int ToPct(float normalized)
        {
            var pct = (int)Math.Round(normalized * 100.0);
            if (pct < 0)
            {
                return 0;
            }

            if (pct > 100)
            {
                return 100;
            }

            return pct;
        }

        internal static string FormatRaw(float normalized)
        {
            var n = ToPct(normalized);
            var whole = n / 100;
            var frac = n % 100;
            if (frac < 10)
            {
                return whole + ".0" + frac;
            }

            return whole + "." + frac;
        }
    }
}
