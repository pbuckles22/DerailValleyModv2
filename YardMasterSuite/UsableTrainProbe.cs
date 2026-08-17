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

        internal static bool HasUsableLocoTrain()
        {
            try
            {
                var target = TryGetTargetCar();
                if (target == null)
                {
                    return false;
                }

                var component = CollectFullyLinkedComponent(target);
                foreach (var c in component)
                {
                    if (c != null && c.IsLoco)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // fail closed
            }

            return false;
        }

        internal static HashSet<TrainCar> CollectFullyLinkedComponent(TrainCar start)
        {
            var visited = new HashSet<TrainCar>();
            var stack = new Stack<TrainCar>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var car = stack.Pop();
                if (car == null || !visited.Add(car))
                {
                    continue;
                }

                TryWalk(car.frontCoupler, stack);
                TryWalk(car.rearCoupler, stack);
            }

            return visited;
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
