using DV;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Consist-max wheel-lift / tip-over buildup vs game threshold (**6.19**).
    /// Coupler <c>TrainStress.stress</c> is not read. Walks the trainset with no extra lists.
    /// HUD uses max; T2 also logs boarded-loco lead.
    /// </summary>
    internal static class DerailRiskReader
    {
        internal static DerailRiskScan ReadConsist(TrainCar? lead)
        {
            if (lead == null)
            {
                return default;
            }

            try
            {
                var threshold = Globals.G?.GameParams != null
                    ? Globals.G.GameParams.DerailBuildUpThreshold
                    : (float?)null;
                var leadPercent = PercentFromCar(lead, threshold);
                float? worst = leadPercent;
                var cars = lead.trainset?.cars;
                if (cars == null || cars.Count == 0)
                {
                    return new DerailRiskScan(leadPercent, leadPercent);
                }

                for (var i = 0; i < cars.Count; i++)
                {
                    var car = cars[i];
                    if (car == null)
                    {
                        continue;
                    }

                    DerailRiskDisplay.ConsiderMax(ref worst, PercentFromCar(car, threshold));
                }

                return new DerailRiskScan(leadPercent, worst);
            }
            catch
            {
                return default;
            }
        }

        private static float? PercentFromCar(TrainCar car, float? threshold)
        {
            try
            {
                var trainStress = car.stress;
                if (trainStress == null)
                {
                    return null;
                }

                return DerailRiskDisplay.PercentOfBuildUp(trainStress.derailBuildUp, threshold);
            }
            catch
            {
                return null;
            }
        }
    }
}
