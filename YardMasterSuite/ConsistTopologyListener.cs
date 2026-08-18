using System;
using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Caches consist length and mass on board, on look-at usable train (**6.3**),
    /// and on native couple/uncouple (<see cref="TrainCar.TrainsetChanged"/> +
    /// coupler Coupled/Uncoupled). Keeps binds after unboard so yard pin-pulls
    /// still publish. Does not poll and does not update on cargo load.
    /// </summary>
    public sealed class ConsistTopologyListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private readonly List<Coupler> _boundCouplers = new List<Coupler>(16);
        private ConsistCache _cache;
        private TrainCar? _car;
        private int _boundLocoId;
        private bool _isBoarded;

        private void OnEnable()
        {
            ConsistTopology.Reset(ref _cache);
            _boundLocoId = 0;
            _car = null;
            _isBoarded = false;
            YmsEventBus.OnPlayerBoardedTrain += OnLocoPresence;
            YmsEventBus.OnUsableTrainChanged += OnUsableTrain;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPlayerBoardedTrain -= OnLocoPresence;
            YmsEventBus.OnUsableTrainChanged -= OnUsableTrain;
            Unbind();
            ConsistTopology.Reset(ref _cache);
            _boundLocoId = 0;
            _isBoarded = false;
        }

        private void OnLocoPresence(LocoPresence presence)
        {
            _isBoarded = presence.IsBoarded;
            var action = ConsistTopology.PrepareForLoco(
                presence.InstanceId,
                ref _cache,
                ref _boundLocoId);
            if (action == ConsistBindAction.KeepListening)
            {
                if (_car != null)
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

        private void OnUsableTrain(UsableTrainState state)
        {
            var boardedId = _isBoarded ? _boundLocoId : 0;
            var anchorId = ConsistTopology.ResolveConsistAnchor(boardedId, state.ConsistAnchorId);
            ApplyLookAtAnchor(anchorId);
        }

        private void ApplyLookAtAnchor(int anchorId)
        {
            if (anchorId == 0)
            {
                return;
            }

            var action = ConsistTopology.PrepareForLoco(anchorId, ref _cache, ref _boundLocoId);
            if (action == ConsistBindAction.BindNewLoco)
            {
                Unbind();
                var car = ResolveAnchorCar(anchorId);
                if (car == null || !car.IsLoco)
                {
                    ConsistTopology.Reset(ref _cache);
                    _boundLocoId = 0;
                    return;
                }

                Bind(car);
                PublishFrom(car);
                return;
            }

            if (_car != null && ConsistTopology.ShouldForceHudPublish(action, anchorId))
            {
                PublishFrom(_car);
            }
        }

        private static TrainCar? ResolveAnchorCar(int anchorId)
        {
            try
            {
                var boarded = PlayerManager.Car;
                if (boarded != null && boarded.IsLoco && boarded.GetInstanceID() == anchorId)
                {
                    return boarded;
                }
            }
            catch
            {
                // fall through to look-at
            }

            var lookAt = UsableTrainProbe.TryGetUsableLoco();
            if (lookAt == null)
            {
                return null;
            }

            try
            {
                return lookAt.GetInstanceID() == anchorId ? lookAt : null;
            }
            catch
            {
                return null;
            }
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
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }

            if (!_cache.Seeded)
            {
                return;
            }

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
