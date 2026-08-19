using System;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Always-on extras: clock (**6.1**). Marked / station / path ship in **6.11–6.12**.
    /// </summary>
    public sealed class AlwaysOnHudListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private ClockCache _cache;

        private void OnEnable()
        {
            _cache = default;
            PublishIfChanged();
        }

        private void OnDisable()
        {
            _cache = default;
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            PublishIfChanged();
        }

        private void PublishIfChanged()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            var known = TryGetGameTime(out var hour, out var minute);
            var wasSeeded = _cache.Seeded;
            var wasKnown = _cache.Known;
            if (!ClockTelemetry.Observe(known, hour, minute, ref _cache))
            {
                return;
            }

            var extras = known ? ClockDisplay.Format(hour, minute) : string.Empty;
            YmsEventBus.RaiseAlwaysOnExtrasChanged(
                new HudBarSnapshot(extras, visible: known));

            var kind = ResolveLogKind(known, wasSeeded, wasKnown);
            var msg = ClockTelemetry.NextLog(hour, minute, kind);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        private static ClockLogKind ResolveLogKind(bool known, bool wasSeeded, bool wasKnown)
        {
            if (!known)
            {
                return ClockLogKind.Hide;
            }

            return !wasSeeded || !wasKnown ? ClockLogKind.Init : ClockLogKind.Change;
        }

        private static bool TryGetGameTime(out int hour, out int minute)
        {
            hour = 0;
            minute = 0;
            try
            {
                var wrapper = DateTimeWrapper.Instance;
                if (wrapper == null)
                {
                    return false;
                }

                var t = wrapper.DateTime;
                hour = t.Hour;
                minute = t.Minute;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
