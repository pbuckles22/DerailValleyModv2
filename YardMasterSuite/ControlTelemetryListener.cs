using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Reads lead-cab levers on the physics tick and publishes only when a rounded
    /// percent changes. Named T2 fields: thr / indy / train / eng / rev.
    /// Samples the usable loco (boarded or look-at) so the full lever row is live (**6.8**).
    /// After PID so <c>thr=</c> matches the commanded notch, not the pre-write 0.
    /// </summary>
    [DefaultExecutionOrder(32100)]
    public sealed class ControlTelemetryListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private ControlLeversCache _cache;
        private int _anchorId;

        private void OnEnable()
        {
            ControlTelemetry.Reset(ref _cache);
            _anchorId = 0;
            YmsEventBus.OnUsableTrainChanged += OnUsableTrain;
        }

        private void OnDisable()
        {
            YmsEventBus.OnUsableTrainChanged -= OnUsableTrain;
            _anchorId = 0;
            ControlTelemetry.Reset(ref _cache);
        }

        private void OnUsableTrain(UsableTrainState state)
        {
            if (!state.HasUsableLocoTrain)
            {
                _anchorId = 0;
                ControlTelemetry.Reset(ref _cache);
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
                ControlTelemetry.Reset(ref _cache);
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
