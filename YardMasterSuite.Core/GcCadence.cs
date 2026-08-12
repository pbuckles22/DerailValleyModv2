using System;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Mutable hitch-probe clocks. Keep on the stack / in the MonoBehaviour;
    /// do not box.
    /// </summary>
    public struct GcCadenceState
    {
        public float LastFrameAt;
        public float LastLogAt;
        public int LastGc0;

        public static GcCadenceState Initial()
        {
            return new GcCadenceState
            {
                LastFrameAt = -1f,
                LastLogAt = -999f,
                LastGc0 = 0
            };
        }
    }

    /// <summary>
    /// Unity-free hitch gate. <see cref="GcCadenceProbe"/> samples clocks in
    /// Update(); this type decides whether to emit a T2 line.
    /// </summary>
    public static class GcCadence
    {
        /// <summary>Log when frame gap exceeds this (seconds).</summary>
        public const float SpikeSeconds = 0.04f;

        /// <summary>At most one spike log per this many seconds (logging itself hitch-taxes).</summary>
        public const float MinLogIntervalSeconds = 1f;

        /// <summary>
        /// Returns a T2 hitch line or null. Allocates only when logging.
        /// </summary>
        public static string? Observe(float now, int gc0, ref GcCadenceState state)
        {
            var lastFrameAt = state.LastFrameAt;
            var gcDelta = gc0 - state.LastGc0;
            state.LastFrameAt = now;
            state.LastGc0 = gc0;

            if (lastFrameAt < 0f)
            {
                return null;
            }

            var dt = now - lastFrameAt;
            if (dt < SpikeSeconds)
            {
                return null;
            }

            if (now - state.LastLogAt < MinLogIntervalSeconds)
            {
                return null;
            }

            state.LastLogAt = now;
            var dtMs = (int)Math.Round(dt * 1000f);
            if (gcDelta > 0)
            {
                return "T2 hitch-spike: dt=" + dtMs + "ms gc0=+" + gcDelta;
            }

            return "T2 hitch-spike: dt=" + dtMs + "ms";
        }
    }
}
