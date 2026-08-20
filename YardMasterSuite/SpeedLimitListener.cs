using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Posted sticky Limit for the HUD. No geometry. Next stays 6.10.
    /// </summary>
    public sealed class SpeedLimitListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private SpeedLimitCache _cache;
        private bool _hasUsableLoco;
        private float? _postedKmh;

        private void OnEnable()
        {
            _cache = default;
            _hasUsableLoco = false;
            _postedKmh = null;
            YmsEventBus.OnUsableTrainChanged += OnUsable;
            YmsEventBus.OnPostedLimitChanged += OnPosted;
            _hasUsableLoco = UsableTrainProbe.HasUsableLocoTrain();
            Publish();
        }

        private void OnDisable()
        {
            YmsEventBus.OnUsableTrainChanged -= OnUsable;
            YmsEventBus.OnPostedLimitChanged -= OnPosted;
            _cache = default;
            _hasUsableLoco = false;
            _postedKmh = null;
        }

        private void OnUsable(UsableTrainState state)
        {
            _hasUsableLoco = state.HasUsableLocoTrain;
            Publish();
        }

        private void OnPosted(PostedLimitSnapshot snapshot)
        {
            _postedKmh = snapshot.Kmh;
            Publish();
        }

        private void Publish()
        {
            var snapshot = SpeedLimitState.Resolve(_hasUsableLoco, _postedKmh);
            var wasSeeded = _cache.Seeded;
            if (!SpeedLimitTelemetry.Observe(snapshot, ref _cache, out var published))
            {
                return;
            }

            EmitLog?.Invoke(SpeedLimitTelemetry.Format(published, wasSeeded));
            YmsEventBus.RaiseSpeedLimitChanged(published);
        }
    }
}
