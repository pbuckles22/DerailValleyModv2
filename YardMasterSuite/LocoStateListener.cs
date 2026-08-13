using System;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Subscribes to vanilla <see cref="PlayerManager.CarChanged"/> (no polling).
    /// Caches the boarded loco and raises <see cref="YmsEventBus.OnPlayerBoardedTrain"/>.
    /// </summary>
    public sealed class LocoStateListener : MonoBehaviour
    {
        /// <summary>UMM logger sink; Main sets this on activate and clears on deactivate.</summary>
        internal static Action<string>? EmitLog;

        private LocoStateCache _cache;

        private void OnEnable()
        {
            _cache = default;
            PlayerManager.CarChanged += OnCarChanged;
            OnCarChanged(PlayerManager.Car);
        }

        private void OnDisable()
        {
            PlayerManager.CarChanged -= OnCarChanged;
            _cache = default;
        }

        private void OnCarChanged(TrainCar car)
        {
            var id = ResolveBoardedLocoInstanceId(car);
            var msg = LocoState.Observe(id, ref _cache);
            if (msg == null)
            {
                return;
            }

            EmitLog?.Invoke(msg);
            YmsEventBus.RaisePlayerBoardedTrain(new LocoPresence(_cache.CurrentInstanceId));
        }

        /// <summary>
        /// Boarded locomotive only. Freight / caboose / on-foot → 0 (unboarded).
        /// </summary>
        internal static int ResolveBoardedLocoInstanceId(TrainCar car)
        {
            if (car == null || !car.IsLoco)
            {
                return 0;
            }

            return car.GetInstanceID();
        }
    }
}
