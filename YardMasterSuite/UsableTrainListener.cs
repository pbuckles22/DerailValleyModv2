using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Publishes <see cref="UsableTrainState"/> when the **4.3** gate or consist
    /// anchor changes (~10 Hz).
    /// </summary>
    public sealed class UsableTrainListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private bool _last;
        private int _lastAnchorId;
        private bool _seeded;
        private float _nextAt;

        private void OnEnable()
        {
            _seeded = false;
            _lastAnchorId = 0;
            _nextAt = 0f;
            Publish(force: true);
        }

        private void OnDisable()
        {
            _seeded = false;
            _lastAnchorId = 0;
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

            _nextAt = Time.unscaledTime + 0.1f;
            Publish(force: false);
        }

        private void Publish(bool force)
        {
            var loco = UsableTrainProbe.TryGetUsableLoco();
            var usable = loco != null;
            var anchorId = 0;
            if (loco != null)
            {
                try
                {
                    anchorId = loco.GetInstanceID();
                }
                catch
                {
                    usable = false;
                    anchorId = 0;
                }
            }

            if (!force && !UsableTrainGate.ShouldPublish(_seeded, _last, _lastAnchorId, usable, anchorId))
            {
                return;
            }

            var usableChanged = !_seeded || usable != _last;
            _seeded = true;
            _last = usable;
            _lastAnchorId = anchorId;
            YmsEventBus.RaiseUsableTrainChanged(new UsableTrainState(usable, anchorId));
            if (usableChanged)
            {
                EmitLog?.Invoke(usable ? "T2 usable-train on" : "T2 usable-train off");
            }
        }
    }
}
