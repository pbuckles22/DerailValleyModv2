using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Reads cab levers on the physics tick and publishes only when a rounded
    /// percent changes. Named T2 fields: thr / indy / train / eng / rev.
    /// </summary>
    public sealed class ControlTelemetryListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private ControlLeversCache _cache;
        private bool _boarded;

        private void OnEnable()
        {
            ControlTelemetry.Reset(ref _cache);
            _boarded = false;
            YmsEventBus.OnPlayerBoardedTrain += OnLocoPresence;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPlayerBoardedTrain -= OnLocoPresence;
            _boarded = false;
            ControlTelemetry.Reset(ref _cache);
        }

        private void OnLocoPresence(LocoPresence presence)
        {
            _boarded = presence.IsBoarded;
            ControlTelemetry.Reset(ref _cache);
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

            if (!TryReadLevers(car, out var throttle, out var indy, out var train, out var engine, out var enginePresent, out var reverser))
            {
                return;
            }

            var wasSeeded = _cache.Seeded;
            var msg = ControlTelemetry.Observe(
                throttle, indy, train, engine, enginePresent, reverser, ref _cache);
            if (msg == null && wasSeeded)
            {
                return;
            }

            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }

            YmsEventBus.RaiseCabControlsChanged(new CabControlsState(
                throttle, indy, train, engine, enginePresent, reverser));
        }

        internal static bool TryReadLevers(
            TrainCar car,
            out float throttle,
            out float indy,
            out float train,
            out float engine,
            out bool enginePresent,
            out float reverser)
        {
            throttle = 0f;
            indy = 0f;
            train = 0f;
            engine = 0f;
            enginePresent = false;
            reverser = 0.5f;
            var controls = car.SimController?.controlsOverrider;
            if (controls == null)
            {
                return false;
            }

            if (controls.Throttle != null)
            {
                throttle = controls.Throttle.Value;
            }

            if (controls.IndependentBrake != null)
            {
                indy = controls.IndependentBrake.Value;
            }

            if (controls.Brake != null)
            {
                train = controls.Brake.Value;
            }

            if (controls.DynamicBrake != null)
            {
                engine = controls.DynamicBrake.Value;
                enginePresent = true;
            }

            if (controls.Reverser != null)
            {
                reverser = controls.Reverser.Value;
            }

            return true;
        }
    }
}
