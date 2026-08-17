using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Look-at / standing local car bar (**6.2**). Publishes formatted line on change (~10 Hz).
    /// </summary>
    public sealed class LocalCarTelemetryListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private string _lastLine = string.Empty;
        private float _nextAt;

        private void OnEnable()
        {
            _lastLine = string.Empty;
            _nextAt = 0f;
            Publish(force: true);
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
            var line = BuildLine() ?? string.Empty;
            if (!force && line == _lastLine)
            {
                return;
            }

            _lastLine = line;
            var visible = !string.IsNullOrWhiteSpace(line);
            YmsEventBus.RaiseLookAtBarChanged(new HudBarSnapshot(line, visible));
            if (visible)
            {
                EmitLog?.Invoke("T2 look-at bar");
            }
        }

        private static string? BuildLine()
        {
            var car = UsableTrainProbe.TryGetTargetCar();
            if (car == null)
            {
                return null;
            }

            try
            {
                var pipe = BrakePipeDisplay.FormatBar(TryGetBrakePipeBar(car));
                var handbrake = HandbrakeDisplay.FormatCount(TryGetHandbrakeCount(car));
                var couplers = CouplingDisplay.FormatHud(
                    CouplerProbe.TryGetLinkStatus(car.frontCoupler),
                    CouplerProbe.TryGetLinkStatus(car.rearCoupler));
                var carNumber = CarNumberDisplay.Format(isLoco: car.IsLoco, freightNumberFromLoco: null);
                var job = JobDisplay.Format(TryGetJobId(car));
                var track = TrackDisplay.Format(TryGetTrackId(car));
                var cargo = CargoDisplay.Format(car.IsLoco, TryGetCargoTypeName(car));
                var locoType = LocoTypeDisplay.Format(TryGetLocoType(car));
                var carKg = car.massController != null ? car.massController.TotalMass : (float?)null;
                ConsistTopologyListener.ReadConsist(car, out _, out var consistKg);
                var mass = TonnageDisplay.FormatCarAndConsistFromKilograms(carKg, consistKg);

                return LocalCarHudLine.Format(
                    pipe,
                    handbrake,
                    couplers,
                    carNumber,
                    job,
                    track,
                    cargo,
                    locoType,
                    mass);
            }
            catch
            {
                return null;
            }
        }

        private static float? TryGetBrakePipeBar(TrainCar car)
        {
            try
            {
                return car.brakeSystem?.brakePipePressure;
            }
            catch
            {
                return null;
            }
        }

        private static int? TryGetHandbrakeCount(TrainCar car)
        {
            try
            {
                return HandbrakeDisplay.IsApplied(car.brakeSystem?.handbrakePosition ?? 0f) ? 1 : 0;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetJobId(TrainCar car) => null;

        private static string? TryGetTrackId(TrainCar car)
        {
            try
            {
                return car.logicCar?.CurrentTrack?.ID.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetCargoTypeName(TrainCar car) => null;

        private static string? TryGetLocoType(TrainCar car)
        {
            try
            {
                return car.carType.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
