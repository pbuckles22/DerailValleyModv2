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
        /// <summary>Log when frame gap exceeds this (seconds). 100 ms: yard play often sits at 40–80 ms.</summary>
        public const float SpikeSeconds = 0.100f;

        /// <summary>Load / stream / scene switch. Distinct from an in-world feature hitch.</summary>
        public const float LoadScaleSeconds = 1f;

        /// <summary>At most one spike log per this many seconds (logging itself hitch-taxes).</summary>
        public const float MinLogIntervalSeconds = 1f;

        /// <summary>Frames faster than this count as <c>fine</c> in the hitch-summary (old 40 ms gate).</summary>
        public const float FineSeconds = 0.040f;

        /// <summary>Windowed <c>T2 hitch-summary</c> interval. Not per-frame.</summary>
        public const float SummaryIntervalSeconds = 30f;

        /// <summary>
        /// Returns a T2 hitch line or null. Allocates only when logging.
        /// Clocks always advance so a load hitch cannot fire on the first in-world frame.
        /// </summary>
        public static string? Observe(
            float now,
            int gc0,
            ref GcCadenceState state,
            bool worldSessionActive)
        {
            var lastFrameAt = state.LastFrameAt;
            var gcDelta = gc0 - state.LastGc0;
            state.LastFrameAt = now;
            state.LastGc0 = gc0;

            if (!HudWorldSession.IsActive(worldSessionActive))
            {
                return null;
            }

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

        /// <summary>
        /// Classify a frame gap for the performance log. Does not decide whether
        /// to emit T2 (that is <see cref="Observe"/> + world session).
        /// </summary>
        public static HitchBand Classify(float dtSeconds)
        {
            if (dtSeconds < SpikeSeconds)
            {
                return HitchBand.BelowGate;
            }

            if (dtSeconds < LoadScaleSeconds)
            {
                return HitchBand.Feature;
            }

            return HitchBand.LoadScale;
        }

        /// <summary>
        /// Count every in-world frame. Hot path is int increments only.
        /// </summary>
        public static void Record(float dtSeconds, int gcDelta, float now, ref GcHitchHistogram hist)
        {
            if (hist.Frames == 0)
            {
                hist.WindowStartAt = now;
            }

            hist.Frames++;
            if (dtSeconds < FineSeconds)
            {
                hist.Fine++;
                return;
            }

            var band = Classify(dtSeconds);
            if (band == HitchBand.BelowGate)
            {
                hist.Below++;
                var ms = (int)Math.Round(dtSeconds * 1000f);
                if (ms > hist.MaxBelowMs)
                {
                    hist.MaxBelowMs = ms;
                }

                if (gcDelta > 0)
                {
                    hist.BelowGc0++;
                }

                return;
            }

            if (band == HitchBand.Feature)
            {
                hist.Feature++;
                return;
            }

            hist.Load++;
        }

        /// <summary>
        /// One T2 hitch-summary per window, or immediately when <paramref name="force"/>
        /// (leave world / disable). Allocates only when emitting.
        /// </summary>
        public static string? MaybeSummary(float now, bool force, ref GcHitchHistogram hist)
        {
            if (hist.Frames == 0)
            {
                return null;
            }

            if (!force && now - hist.WindowStartAt < SummaryIntervalSeconds)
            {
                return null;
            }

            var line = FormatSummary(in hist);
            hist = default;
            return line;
        }

        public static string FormatSummary(in GcHitchHistogram hist)
        {
            var sb = StringBuilderPool.Shared.Rent();
            sb.Append("T2 hitch-summary: n=");
            sb.Append(hist.Frames);
            sb.Append(" fine=");
            sb.Append(hist.Fine);
            sb.Append(" below=");
            sb.Append(hist.Below);
            sb.Append(" max=");
            sb.Append(hist.MaxBelowMs);
            sb.Append("ms gc0=");
            sb.Append(hist.BelowGc0);
            sb.Append(" feature=");
            sb.Append(hist.Feature);
            sb.Append(" load=");
            sb.Append(hist.Load);
            var text = sb.ToString();
            StringBuilderPool.Shared.Return(sb);
            return text;
        }
    }

    /// <summary>
    /// Windowed frame-time buckets. <see cref="GcCadence.Record"/> is the only hot-path writer.
    /// </summary>
    public struct GcHitchHistogram
    {
        public int Frames;
        public int Fine;
        public int Below;
        public int Feature;
        public int Load;
        public int MaxBelowMs;
        public int BelowGc0;
        public float WindowStartAt;
    }

    /// <summary>dt bands from 3.1 hitch smoke. Keep in lockstep with PERFORMANCE_LOG.</summary>
    public enum HitchBand
    {
        /// <summary>Under 100 ms. Probe is silent. Still record in the performance log when counted.</summary>
        BelowGate = 0,
        /// <summary>100 ms through just under 1 s. Probe logs in a world session.</summary>
        Feature = 1,
        /// <summary>1 s and up. Load, stream, scene switch.</summary>
        LoadScale = 2,
    }
}
