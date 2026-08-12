using System;
using UnityEngine;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Silent frametime / GC hitch monitor. Samples unscaled time each Update
    /// and logs a throttled T2 hitch-spike line. Type B mailbox drain is Epic 4.
    /// </summary>
    public sealed class GcCadenceProbe : MonoBehaviour
    {
        /// <summary>UMM logger sink; Main sets this on activate and clears on deactivate.</summary>
        internal static Action<string>? EmitLog;

        private GcCadenceState _state = GcCadenceState.Initial();

        private void OnEnable()
        {
            _state = GcCadenceState.Initial();
            _state.LastGc0 = GC.CollectionCount(0);
        }

        private void Update()
        {
            var msg = GcCadence.Observe(Time.unscaledTime, GC.CollectionCount(0), ref _state);
            if (msg == null)
            {
                return;
            }

            EmitLog?.Invoke(msg);
        }
    }
}
