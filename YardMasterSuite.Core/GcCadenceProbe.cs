using System;
using UnityEngine;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Silent frametime / GC hitch monitor. Samples unscaled time each Update
    /// and logs a throttled T2 hitch-spike line plus a windowed T2 hitch-summary.
    /// Type B mailbox drain lives on <c>YmsMailboxDrain</c> (story 4.1).
    /// </summary>
    public sealed class GcCadenceProbe : MonoBehaviour
    {
        /// <summary>UMM logger sink; Main sets this on activate and clears on deactivate.</summary>
        internal static Action<string>? EmitLog;

        /// <summary>World session (player transform present). Main sets this with the HUD gate.</summary>
        internal static Func<bool>? IsWorldSession;

        private static GcCadenceProbe? _live;

        private GcCadenceState _state = GcCadenceState.Initial();
        private GcHitchHistogram _hist;
        private bool _wasInWorld;

        /// <summary>
        /// Emit the current window if any. Call from Main before clearing
        /// <see cref="EmitLog"/> — Unity <c>Destroy</c> defers OnDisable.
        /// </summary>
        internal static void FlushPending()
        {
            _live?.FlushSummary(Time.unscaledTime, force: true);
        }

        private void OnEnable()
        {
            _state = GcCadenceState.Initial();
            _state.LastGc0 = GC.CollectionCount(0);
            _hist = default;
            _wasInWorld = false;
            _live = this;
        }

        private void OnDisable()
        {
            FlushSummary(Time.unscaledTime, force: true);
            if (_live == this)
            {
                _live = null;
            }
        }

        private void Update()
        {
            var inWorld = IsWorldSession?.Invoke() ?? false;
            var now = Time.unscaledTime;
            var gc0 = GC.CollectionCount(0);
            var lastFrameAt = _state.LastFrameAt;
            var gcDelta = gc0 - _state.LastGc0;
            var spike = GcCadence.Observe(now, gc0, ref _state, inWorld);
            if (inWorld && lastFrameAt >= 0f)
            {
                GcCadence.Record(now - lastFrameAt, gcDelta, now, ref _hist);
            }

            var leftWorld = _wasInWorld && !inWorld;
            _wasInWorld = inWorld;
            if (spike != null)
            {
                EmitLog?.Invoke(spike);
            }

            FlushSummary(now, leftWorld);
        }

        private void FlushSummary(float now, bool force)
        {
            var summary = GcCadence.MaybeSummary(now, force, ref _hist);
            if (summary != null)
            {
                EmitLog?.Invoke(summary);
            }
        }
    }
}
