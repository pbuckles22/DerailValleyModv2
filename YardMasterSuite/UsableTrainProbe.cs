using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Target car + usable loco train probe (v1 TelemetryReader subset).
    /// Look-at wins; standing is fallback (**6.3**).
    /// </summary>
    internal static class UsableTrainProbe
    {
        private static int _trainLookMask = -1;
        private static readonly HashSet<TrainCar> WalkVisited = new HashSet<TrainCar>();
        private static readonly Stack<TrainCar> WalkStack = new Stack<TrainCar>();

        internal static TrainCar? TryGetLookAtCar()
        {
            try
            {
                var cam = PlayerManager.ActiveCamera ?? Camera.main;
                if (cam == null)
                {
                    return null;
                }

                var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.SphereCast(
                        ray,
                        LookAtTargeting.SphereRadiusMeters,
                        out var hit,
                        LookAtTargeting.MaxDistanceMeters,
                        TrainLookMask()))
                {
                    return TrainCar.Resolve(hit.collider.transform);
                }
            }
            catch
            {
                // fail closed
            }

            return null;
        }

        internal static TrainCar? TryGetStandingCar()
        {
            try
            {
                return PlayerManager.Car;
            }
            catch
            {
                return null;
            }
        }

        internal static TrainCar? TryGetTargetCar()
        {
            var standing = TryGetStandingCar();
            var lookAt = TryGetLookAtCar();
            return TargetCarSelection.Resolve(standing != null, lookAt != null) switch
            {
                TargetCarSource.Standing => standing,
                TargetCarSource.LookAt => lookAt,
                _ => null,
            };
        }

        /// <summary>
        /// First loco in the usable coupler component of the target car, or null.
        /// </summary>
        internal static TrainCar? TryGetUsableLoco()
        {
            try
            {
                var target = TryGetTargetCar();
                if (target == null)
                {
                    return null;
                }

                return FindLocoInUsableComponent(target);
            }
            catch
            {
                return null;
            }
        }

        internal static bool HasUsableLocoTrain() => TryGetUsableLoco() != null;

        private static TrainCar? FindLocoInUsableComponent(TrainCar start)
        {
            WalkVisited.Clear();
            WalkStack.Clear();
            WalkStack.Push(start);

            try
            {
                while (WalkStack.Count > 0)
                {
                    var car = WalkStack.Pop();
                    if (car == null || !WalkVisited.Add(car))
                    {
                        continue;
                    }

                    if (car.IsLoco)
                    {
                        return car;
                    }

                    TryWalk(car.frontCoupler, WalkStack);
                    TryWalk(car.rearCoupler, WalkStack);
                }
            }
            finally
            {
                WalkVisited.Clear();
                WalkStack.Clear();
            }

            return null;
        }

        private static void TryWalk(Coupler? coupler, Stack<TrainCar> stack)
        {
            var status = CouplerProbe.TryGetLinkStatus(coupler);
            if (status is null || !CouplingLink.IsUsable(status.Value))
            {
                return;
            }

            var other = coupler!.GetCoupled() ?? coupler.coupledTo;
            var otherCar = other?.train;
            if (otherCar != null)
            {
                stack.Push(otherCar);
            }
        }

        private static int TrainLookMask()
        {
            if (_trainLookMask < 0)
            {
                _trainLookMask = LayerMask.GetMask("Train_Big_Collider", "Train_Interior");
            }

            return _trainLookMask;
        }
    }
}
