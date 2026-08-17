using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Maps geometry scan results to HUD limit snapshots (geometry authority first).
    /// </summary>
    public sealed class SpeedLimitListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private SpeedLimitCache _cache;

        private void OnEnable()
        {
            _cache = default;
            YmsEventBus.OnGeometryScan += OnGeometryScan;
        }

        private void OnDisable()
        {
            YmsEventBus.OnGeometryScan -= OnGeometryScan;
            _cache = default;
        }

        private void OnGeometryScan(GeometryScanResult scan)
        {
            var snapshot = SpeedLimitState.FromGeometry(in scan);
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
