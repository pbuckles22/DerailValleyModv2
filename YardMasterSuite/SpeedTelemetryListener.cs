using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Samples usable-loco speed on FixedUpdate; publishes when rounded km/h changes.
    /// Boarded cab and on-foot look-at share the same consist speed (**6.8**).
    /// </summary>
    public sealed class SpeedTelemetryListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private SpeedCache _cache;
        private int _anchorId;

        private void OnEnable()
        {
            SpeedTelemetry.Reset(ref _cache);
            _anchorId = 0;
            YmsEventBus.OnUsableTrainChanged += OnUsableTrain;
        }

        private void OnDisable()
        {
            YmsEventBus.OnUsableTrainChanged -= OnUsableTrain;
            _anchorId = 0;
            SpeedTelemetry.Reset(ref _cache);
        }

        private void OnUsableTrain(UsableTrainState state)
        {
            if (!state.HasUsableLocoTrain)
            {
                _anchorId = 0;
                SpeedTelemetry.Reset(ref _cache);
            }
        }

        private void FixedUpdate()
        {
            var car = UsableTrainProbe.TryGetUsableLoco();
            if (car == null || !car.IsLoco)
            {
                return;
            }

            var id = car.GetInstanceID();
            if (id != _anchorId)
            {
                _anchorId = id;
                SpeedTelemetry.Reset(ref _cache);
            }

            float speedMps;
            try
            {
                speedMps = car.GetAbsSpeed();
            }
            catch
            {
                return;
            }

            var wasSeeded = _cache.Seeded;
            if (!SpeedTelemetry.Observe(speedMps, ref _cache, out var snapshot))
            {
                return;
            }

            var msg = SpeedTelemetry.FormatLog(snapshot.Kmh, wasSeeded);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }

            YmsEventBus.RaiseSpeedChanged(snapshot);
        }
    }
}
