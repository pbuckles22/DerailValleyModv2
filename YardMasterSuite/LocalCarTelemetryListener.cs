using System;
using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Look-at / standing local car bar (**6.2**). HUD refreshes on line change
    /// (~10 Hz). T2 logs identity (car / cargo / track / job) only.
    /// </summary>
    public sealed class LocalCarTelemetryListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private readonly List<bool> _isLocoFlags = new List<bool>(32);
        private string _lastLine = string.Empty;
        private LookAtBarCache _logCache;
        private float _nextAt;

        private void OnEnable()
        {
            _lastLine = string.Empty;
            _nextAt = 0f;
            LookAtBarTelemetry.Reset(ref _logCache);
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
            var car = UsableTrainProbe.TryGetTargetCar();
            int? freight = null;
            string? cargoRaw = null;
            string? trackId = null;
            var line = string.Empty;
            if (car != null)
            {
                freight = TryFreightNumber(car);
                cargoRaw = TryGetCargoTypeName(car);
                trackId = TryGetTrackId(car);
                line = BuildLine(car, freight, cargoRaw, trackId) ?? string.Empty;
            }

            var visible = !string.IsNullOrWhiteSpace(line);
            if (force || line != _lastLine)
            {
                _lastLine = line;
                YmsEventBus.RaiseLookAtBarChanged(new HudBarSnapshot(line, visible));
            }

            var token = car == null
                ? LookAtBarTelemetry.CarTokenUnknown
                : LookAtBarTelemetry.CarToken(car.IsLoco, freight);
            var jobId = car == null ? null : TryGetJobId(car);
            var msg = LookAtBarTelemetry.Observe(visible, token, cargoRaw, trackId, ref _logCache, jobId);
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        private static string? BuildLine(
            TrainCar car,
            int? freight,
            string? cargoRaw,
            string? trackId)
        {
            try
            {
                var pipe = BrakePipeDisplay.FormatBar(TryGetBrakePipeBar(car));
                var handbrake = HandbrakeDisplay.FormatCount(TryGetHandbrakeCount(car));
                var couplers = CouplingDisplay.FormatHud(
                    CouplerProbe.TryGetLinkStatus(car.frontCoupler),
                    CouplerProbe.TryGetLinkStatus(car.rearCoupler));
                var carNumber = CarNumberDisplay.Format(car.IsLoco, freight);
                var job = JobDisplay.Format(TryGetJobId(car));
                var track = TrackDisplay.Format(trackId);
                var cargo = CargoDisplay.Format(car.IsLoco, cargoRaw);
                var locoType = LocoTypeDisplay.Format(car.IsLoco, TryGetLocoType(car));
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

        private int? TryFreightNumber(TrainCar car)
        {
            if (car.IsLoco)
            {
                return null;
            }

            TrainCar? loco;
            try
            {
                loco = UsableTrainProbe.TryGetUsableLoco();
            }
            catch
            {
                return null;
            }

            var cars = car.trainset != null ? car.trainset.cars : null;
            if (loco == null || cars == null || cars.Count == 0)
            {
                return null;
            }

            _isLocoFlags.Clear();
            var locoIndex = -1;
            var carIndex = -1;
            for (var i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                var isLoco = c != null && c.IsLoco;
                _isLocoFlags.Add(isLoco);
                if (c == loco)
                {
                    locoIndex = i;
                }

                if (c == car)
                {
                    carIndex = i;
                }
            }

            return CarNumberDisplay.FreightNumberFromLoco(locoIndex, carIndex, _isLocoFlags);
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

        private static string? TryGetJobId(TrainCar car)
        {
            try
            {
                var logicCar = car.logicCar;
                if (logicCar == null || JobsManager.Instance == null)
                {
                    return null;
                }

                var job = JobsManager.Instance.GetJobOfCar(logicCar);
                var id = job?.ID?.Trim();
                return string.IsNullOrEmpty(id) ? null : id;
            }
            catch
            {
                return null;
            }
        }

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

        private static string? TryGetCargoTypeName(TrainCar car)
        {
            try
            {
                return car.LoadedCargo.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string? TryGetLocoType(TrainCar car)
        {
            try
            {
                var id = car.carLivery?.parentType?.id ?? car.carLivery?.id;
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                return car.carType.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
