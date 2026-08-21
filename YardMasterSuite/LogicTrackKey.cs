using DV.Logic.Job;

namespace YardMasterSuite
{
    /// <summary>
    /// Logic-track display id for Path dest/origin and PathCheck edges (v1 TrackKey).
    /// </summary>
    internal static class LogicTrackKey
    {
        internal static string? FromCar(TrainCar? car)
        {
            if (car == null)
            {
                return null;
            }

            try
            {
                return FromLogic(car.logicCar?.CurrentTrack);
            }
            catch
            {
                return null;
            }
        }

        internal static string? FromRail(RailTrack? rail)
        {
            if (rail == null)
            {
                return null;
            }

            try
            {
                var map = RailTrackRegistry.RailTrackToLogicTrack;
                if (map != null && map.TryGetValue(rail, out var logic) && logic != null)
                {
                    return FromLogic(logic);
                }
            }
            catch
            {
                // fall through
            }

            return null;
        }

        internal static string? FromLogic(Track? logic)
        {
            if (logic?.ID == null)
            {
                return null;
            }

            try
            {
                var id = logic.ID;
                var display = id.FullDisplayID?.Trim();
                if (!string.IsNullOrEmpty(display))
                {
                    return display;
                }

                var full = id.FullID?.Trim();
                if (!string.IsNullOrEmpty(full))
                {
                    return full;
                }

                var fallback = id.ToString()?.Trim();
                return string.IsNullOrEmpty(fallback) ? null : fallback;
            }
            catch
            {
                return null;
            }
        }
    }
}
