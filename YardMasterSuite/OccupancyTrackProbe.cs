using System.Collections.Generic;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Align-time occupancy for PathPlan. Not Update — FindObjects is OK on Check/Align.
    /// Cab 22.38: numbered pockets occupied → keep the unnumbered through.
    /// </summary>
    internal static class OccupancyTrackProbe
    {
        private static readonly List<string?> Keys = new List<string?>(128);

        internal static HashSet<string> ForPlan(
            IReadOnlyList<PathEdge>? edges,
            string? originTrackId,
            string? destTrackId,
            string? destYardOverride = null)
        {
            Keys.Clear();
            TrainCar[]? cars = null;
            try
            {
                cars = Object.FindObjectsOfType<TrainCar>();
            }
            catch
            {
                // fail closed: empty occupancy
            }

            if (cars != null)
            {
                for (var i = 0; i < cars.Length; i++)
                {
                    Keys.Add(LogicTrackKey.FromCar(cars[i]));
                }
            }

            return PathRouteConstraints.OccupiedForAlign(
                Keys,
                edges,
                originTrackId,
                destTrackId,
                destYardOverride);
        }
    }
}
