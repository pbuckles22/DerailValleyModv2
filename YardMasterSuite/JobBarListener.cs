using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Active job bar (**6.13**). Hidden when no job on look-at car.
    /// </summary>
    public sealed class JobBarListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private string _lastLine = string.Empty;
        private float _nextAt;

        private void OnEnable()
        {
            _lastLine = string.Empty;
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

            _nextAt = Time.unscaledTime + 0.25f;
            Publish(force: false);
        }

        private void Publish(bool force)
        {
            var line = BuildLine() ?? string.Empty;
            if (!force && line == _lastLine)
            {
                return;
            }

            _lastLine = line;
            YmsEventBus.RaiseJobBarChanged(new HudBarSnapshot(line, visible: !string.IsNullOrWhiteSpace(line)));
            if (!string.IsNullOrWhiteSpace(line))
            {
                EmitLog?.Invoke("T2 job bar");
            }
        }

        private static string? BuildLine() => null;
    }
}
