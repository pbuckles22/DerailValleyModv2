using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Samples look heading in LateUpdate (camera has already moved). Publishes
    /// Type A only when the 16-point bucket changes — not per frame.
    /// Source: <see cref="PlayerManager.ActiveCamera"/>, else player transform.
    /// Not loco facing — this is a personal compass (on foot and in cab).
    /// </summary>
    public sealed class HeadingListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private HeadingCache _cache;
        private float _lastChangeLogAt = -999f;

        private void OnEnable()
        {
            _cache = default;
            _lastChangeLogAt = -999f;
            PublishIfChanged();
        }

        private void OnDisable()
        {
            _cache = default;
            _lastChangeLogAt = -999f;
        }

        private void LateUpdate()
        {
            PublishIfChanged();
        }

        private void PublishIfChanged()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            var index = ResolveHeadingIndex();
            var wasSeeded = _cache.Seeded;
            if (!HeadingTelemetry.Observe(index, ref _cache))
            {
                return;
            }

            YmsEventBus.RaiseHeadingChanged(new CompassHeading(index));
            var kind = wasSeeded ? HeadingLogKind.Change : HeadingLogKind.Init;
            var msg = HeadingTelemetry.NextLog(index, kind, Time.unscaledTime, ref _lastChangeLogAt);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        internal static int ResolveHeadingIndex()
        {
            var cam = PlayerManager.ActiveCamera;
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam != null)
            {
                var f = cam.transform.forward;
                return HeadingDisplay.ToPointIndex(HeadingDisplay.FromForward(f.x, f.z));
            }

            var player = PlayerManager.PlayerTransform;
            if (player != null)
            {
                var f = player.forward;
                return HeadingDisplay.ToPointIndex(HeadingDisplay.FromForward(f.x, f.z));
            }

            return HeadingDisplay.UnknownIndex;
        }
    }
}
