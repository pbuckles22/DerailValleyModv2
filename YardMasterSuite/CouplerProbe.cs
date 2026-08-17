using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Coupler link resolution for usable-consist walks (**4.3** / **6.3**).
    /// </summary>
    internal static class CouplerProbe
    {
        public static CouplerLinkStatus? TryGetLinkStatus(Coupler? coupler)
        {
            if (coupler == null)
            {
                return null;
            }

            var other = coupler.GetCoupled() ?? coupler.coupledTo;
            var mechanicallyCoupled = coupler.IsCoupled();
            var tightened = mechanicallyCoupled
                && (coupler.IsTightened() || (other != null && other.IsTightened()));
            var airHoseConnected = IsAirHoseConnected(coupler);
            var cockOpenThisEnd = coupler.IsCockOpen;
            var cocksOpen = AreCocksOpenBothSides(coupler);
            TryGetMuCableState(coupler, other, out var muPresent, out var muConnected);
            return CouplingLink.Resolve(
                mechanicallyCoupled,
                tightened,
                airHoseConnected,
                cocksOpen,
                cockOpenThisEnd,
                muPresent,
                muConnected);
        }

        private static bool IsAirHoseConnected(Coupler coupler)
        {
            try
            {
                if (coupler.GetAirHoseConnectedTo() != null)
                {
                    return true;
                }

                var hoseAndCock = coupler.hoseAndCock;
                return hoseAndCock != null && hoseAndCock.IsHoseConnected;
            }
            catch
            {
                return false;
            }
        }

        private static bool AreCocksOpenBothSides(Coupler coupler)
        {
            if (!coupler.IsCockOpen)
            {
                return false;
            }

            var other = coupler.GetCoupled() ?? coupler.coupledTo;
            return other == null || other.IsCockOpen;
        }

        private static void TryGetMuCableState(
            Coupler coupler,
            Coupler? other,
            out bool muPresent,
            out bool muConnected)
        {
            muPresent = false;
            muConnected = false;

            var car = coupler.train;
            if (car == null || !car.IsMultipleUnit)
            {
                return;
            }

            var mod = car.muModule;
            if (mod != null)
            {
                muConnected = coupler.isFrontCoupler ? mod.ConnectedFront : mod.ConnectedRear;
            }

            var otherCar = other?.train;
            if (otherCar != null && otherCar.IsMultipleUnit)
            {
                muPresent = true;
            }
            else if (muConnected)
            {
                muPresent = true;
            }
        }
    }
}
