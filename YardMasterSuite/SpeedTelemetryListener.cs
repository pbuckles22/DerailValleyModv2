using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Samples boarded loco speed on FixedUpdate; publishes when rounded km/h changes.
    /// </summary>
    public sealed class SpeedTelemetryListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private SpeedCache _cache;
        private bool _boarded;

        private void OnEnable()
        {
            SpeedTelemetry.Reset(ref _cache);
            _boarded = false;
            YmsEventBus.OnPlayerBoardedTrain += OnLocoPresence;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPlayerBoardedTrain -= OnLocoPresence;
            _boarded = false;
            SpeedTelemetry.Reset(ref _cache);
        }

        private void OnLocoPresence(LocoPresence presence)
        {
            _boarded = presence.IsBoarded;
            SpeedTelemetry.Reset(ref _cache);
        }

        private void FixedUpdate()
        {
            if (!_boarded)
            {
                return;
            }

            var car = PlayerManager.Car;
            if (car == null || !car.IsLoco)
            {
                return;
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
