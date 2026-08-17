using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Always-on extras: clock (**6.1**). Marked / station / path ship in **6.11–6.13**.
    /// </summary>
    public sealed class AlwaysOnHudListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private string _lastExtras = string.Empty;
        private float _nextAt;

        private void OnEnable()
        {
            _lastExtras = string.Empty;
            _nextAt = 0f;
            Publish(force: true);
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextAt)
            {
                return;
            }

            _nextAt = Time.unscaledTime + 1f;
            Publish(force: false);
        }

        private void Publish(bool force)
        {
            var extras = BuildExtras();
            if (!force && extras == _lastExtras)
            {
                return;
            }

            _lastExtras = extras;
            YmsEventBus.RaiseAlwaysOnExtrasChanged(new HudBarSnapshot(extras, visible: !string.IsNullOrWhiteSpace(extras)));
        }

        private static string BuildExtras()
        {
            try
            {
                var clock = ClockDisplay.Format(TryGetGameTime());
                return clock.StartsWith("—", StringComparison.Ordinal) ? string.Empty : clock;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static DateTime? TryGetGameTime() => null;
    }
}
