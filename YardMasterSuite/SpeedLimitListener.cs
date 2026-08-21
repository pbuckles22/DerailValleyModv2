using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Posted sticky Limit + Next for the HUD. No geometry.
    /// </summary>
    public sealed class SpeedLimitListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private SpeedLimitCache _cache;
        private bool _hasUsableLoco;
        private float? _postedKmh;
        private float? _nextKmh;
        private float? _nextAlongMeters;
        private float _massTonnes = 40f;

        private void OnEnable()
        {
            _cache = default;
            _hasUsableLoco = false;
            _postedKmh = null;
            _nextKmh = null;
            _nextAlongMeters = null;
            _massTonnes = 40f;
            YmsEventBus.OnUsableTrainChanged += OnUsable;
            YmsEventBus.OnPostedLimitChanged += OnPosted;
            YmsEventBus.OnConsistChanged += OnConsist;
            _hasUsableLoco = UsableTrainProbe.HasUsableLocoTrain();
            Publish();
        }

        private void OnDisable()
        {
            YmsEventBus.OnUsableTrainChanged -= OnUsable;
            YmsEventBus.OnPostedLimitChanged -= OnPosted;
            YmsEventBus.OnConsistChanged -= OnConsist;
            _cache = default;
            _hasUsableLoco = false;
            _postedKmh = null;
            _nextKmh = null;
            _nextAlongMeters = null;
        }

        private void OnUsable(UsableTrainState state)
        {
            _hasUsableLoco = state.HasUsableLocoTrain;
            Publish();
        }

        private void OnPosted(PostedLimitSnapshot snapshot)
        {
            _postedKmh = snapshot.Kmh;
            _nextKmh = snapshot.NextKmh;
            _nextAlongMeters = snapshot.NextAlongMeters;
            Publish();
        }

        private void OnConsist(ConsistSnapshot snapshot)
        {
            _massTonnes = snapshot.MassTonnes > 0 ? snapshot.MassTonnes : 40f;
            Publish();
        }

        private void Publish()
        {
            var snapshot = SpeedLimitState.Resolve(
                _hasUsableLoco,
                _postedKmh,
                _nextKmh,
                _nextAlongMeters);
            var wasSeeded = _cache.Seeded;
            if (!SpeedLimitTelemetry.Observe(snapshot, ref _cache, out var published, _massTonnes))
            {
                return;
            }

            if (_cache.EmitLog)
            {
                EmitLog?.Invoke(SpeedLimitTelemetry.Format(published, wasSeeded, _massTonnes));
            }

            YmsEventBus.RaiseSpeedLimitChanged(published);
        }
    }
}
