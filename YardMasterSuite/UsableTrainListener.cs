using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Publishes <see cref="UsableTrainState"/> when the **4.3** gate changes (~10 Hz).
    /// </summary>
    public sealed class UsableTrainListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private bool _last;
        private bool _seeded;
        private float _nextAt;

        private void OnEnable()
        {
            _seeded = false;
            _nextAt = 0f;
            Publish(force: true);
        }

        private void OnDisable()
        {
            _seeded = false;
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
            var usable = UsableTrainProbe.HasUsableLocoTrain();
            if (!force && _seeded && usable == _last)
            {
                return;
            }

            _seeded = true;
            _last = usable;
            YmsEventBus.RaiseUsableTrainChanged(new UsableTrainState(usable));
            EmitLog?.Invoke(usable ? "T2 usable-train on" : "T2 usable-train off");
        }
    }
}
