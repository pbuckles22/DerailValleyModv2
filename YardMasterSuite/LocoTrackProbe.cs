using UnityEngine;

namespace YardMasterSuite
{
    /// <summary>Bogie track for the usable loco (posted board index).</summary>
    internal static class LocoTrackProbe
    {
        internal static RailTrack? ResolveTrack(TrainCar? car)
        {
            if (car == null || !car.IsLoco)
            {
                return null;
            }

            try
            {
                var bogie = car.FrontBogie ?? car.RearBogie;
                return bogie != null ? bogie.track : null;
            }
            catch
            {
                return null;
            }
        }

        internal static int ResolveTrackId(TrainCar? car)
        {
            var track = ResolveTrack(car);
            return track == null ? 0 : track.GetInstanceID();
        }
    }
}
