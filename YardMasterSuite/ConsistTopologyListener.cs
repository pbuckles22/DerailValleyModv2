using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Caches consist length and mass on board and on native couple/uncouple
    /// (<see cref="TrainCar.TrainsetChanged"/> + coupler Coupled/Uncoupled).
    /// Keeps binds after unboard so yard pin-pulls still publish. Does not poll
    /// and does not update on cargo load.
    /// </summary>
    public sealed class ConsistTopologyListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private readonly List<Coupler> _boundCouplers = new List<Coupler>(16);
        private ConsistCache _cache;
        private TrainCar? _car;
        private int _boundLocoId;

        private void OnEnable()
        {
            ConsistTopology.Reset(ref _cache);
            _boundLocoId = 0;
            _car = null;
            YmsEventBus.OnPlayerBoardedTrain += OnLocoPresence;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPlayerBoardedTrain -= OnLocoPresence;
            Unbind();
            ConsistTopology.Reset(ref _cache);
            _boundLocoId = 0;
        }

        private void OnLocoPresence(LocoPresence presence)
        {
            var action = ConsistTopology.PrepareForLoco(
                presence.InstanceId,
                ref _cache,
                ref _boundLocoId);
            if (action == ConsistBindAction.KeepListening)
            {
                if (presence.IsBoarded && _car != null)
                {
                    PublishFrom(_car);
                }

                return;
            }

            Unbind();
            var car = PlayerManager.Car;
            if (car == null || !car.IsLoco)
            {
                ConsistTopology.Reset(ref _cache);
                _boundLocoId = 0;
                return;
            }

            Bind(car);
            PublishFrom(car);
        }

        private void Bind(TrainCar car)
        {
            _car = car;
            car.TrainsetChanged += OnTrainsetChanged;
            BindCouplers(car);
        }

        private void Unbind()
        {
            if (_car != null)
            {
                _car.TrainsetChanged -= OnTrainsetChanged;
            }

            for (var i = 0; i < _boundCouplers.Count; i++)
            {
                var coupler = _boundCouplers[i];
                if (coupler == null)
                {
                    continue;
                }

                coupler.Coupled -= OnCoupled;
                coupler.Uncoupled -= OnUncoupled;
            }

            _boundCouplers.Clear();
            _car = null;
        }

        private void BindCouplers(TrainCar seed)
        {
            var cars = seed.trainset != null ? seed.trainset.cars : null;
            if (cars == null || cars.Count == 0)
            {
                BindCarCouplers(seed);
                return;
            }

            for (var i = 0; i < cars.Count; i++)
            {
                BindCarCouplers(cars[i]);
            }
        }

        private void BindCarCouplers(TrainCar car)
        {
            if (car == null)
            {
                return;
            }

            BindOne(car.frontCoupler);
            BindOne(car.rearCoupler);
        }

        private void BindOne(Coupler coupler)
        {
            if (coupler == null)
            {
                return;
            }

            coupler.Coupled += OnCoupled;
            coupler.Uncoupled += OnUncoupled;
            _boundCouplers.Add(coupler);
        }

        private void OnTrainsetChanged(Trainset _)
        {
            RebindAndPublish();
        }

        private void OnCoupled(object sender, CoupleEventArgs e)
        {
            RebindAndPublish();
        }

        private void OnUncoupled(object sender, UncoupleEventArgs e)
        {
            RebindAndPublish();
        }

        private void RebindAndPublish()
        {
            var car = _car;
            if (car == null)
            {
                return;
            }

            UnbindCouplersOnly();
            BindCouplers(car);
            PublishFrom(car);
        }

        private void UnbindCouplersOnly()
        {
            for (var i = 0; i < _boundCouplers.Count; i++)
            {
                var coupler = _boundCouplers[i];
                if (coupler == null)
                {
                    continue;
                }

                coupler.Coupled -= OnCoupled;
                coupler.Uncoupled -= OnUncoupled;
            }

            _boundCouplers.Clear();
        }

        private void PublishFrom(TrainCar car)
        {
            ReadConsist(car, out var cars, out var kg);
            var msg = ConsistTopology.Observe(cars, kg, ref _cache);
            if (msg == null)
            {
                return;
            }

            EmitLog?.Invoke(msg);
            YmsEventBus.RaiseConsistChanged(new ConsistSnapshot(_cache.CarCount, _cache.MassTonnes));
        }

        internal static void ReadConsist(TrainCar car, out int carCount, out float massKg)
        {
            carCount = 0;
            massKg = 0f;
            var cars = car.trainset != null ? car.trainset.cars : null;
            if (cars == null || cars.Count == 0)
            {
                carCount = 1;
                if (car.massController != null)
                {
                    massKg = car.massController.TotalMass;
                }

                return;
            }

            for (var i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                if (c == null)
                {
                    continue;
                }

                carCount++;
                if (c.massController != null)
                {
                    massKg += c.massController.TotalMass;
                }
            }
        }
    }
}
