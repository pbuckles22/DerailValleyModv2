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

        /// <summary>
        /// Bogie arc position on its track. The game already maintains this, so
        /// it costs nothing per frame — no closest-point search on the loco.
        /// </summary>
        internal static bool TryResolveSpan(TrainCar? car, out int trackId, out float spanMeters)
        {
            trackId = 0;
            spanMeters = float.NaN;
            if (car == null || !car.IsLoco)
            {
                return false;
            }

            try
            {
                var bogie = car.FrontBogie ?? car.RearBogie;
                if (bogie == null || bogie.track == null)
                {
                    return false;
                }

                var traveller = bogie.traveller;
                if (traveller == null)
                {
                    return false;
                }

                trackId = bogie.track.GetInstanceID();
                spanMeters = (float)traveller.Span;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// **13.2.2:** logic-track id + span + length. Split bogies → <paramref name="uniqueTrack"/> false.
        /// </summary>
        internal static bool TryResolvePrepPose(
            TrainCar? car,
            out string? logicTrackId,
            out float spanMeters,
            out float trackLengthMeters,
            out bool uniqueTrack)
        {
            logicTrackId = null;
            spanMeters = float.NaN;
            trackLengthMeters = 0f;
            uniqueTrack = false;
            if (car == null || !car.IsLoco)
            {
                return false;
            }

            try
            {
                var front = car.FrontBogie;
                var rear = car.RearBogie;
                var frontTrack = front != null ? front.track : null;
                var rearTrack = rear != null ? rear.track : null;
                if (frontTrack == null && rearTrack == null)
                {
                    return false;
                }

                if (frontTrack != null && rearTrack != null
                    && frontTrack.GetInstanceID() != rearTrack.GetInstanceID())
                {
                    uniqueTrack = false;
                    return true;
                }

                uniqueTrack = true;
                var bogie = frontTrack != null ? front : rear;
                if (bogie == null || bogie.track == null)
                {
                    uniqueTrack = false;
                    return false;
                }

                var traveller = bogie.traveller;
                if (traveller == null)
                {
                    uniqueTrack = false;
                    return false;
                }

                logicTrackId = LogicTrackKey.FromRail(bogie.track);
                spanMeters = (float)traveller.Span;
                trackLengthMeters = ResolveLengthMeters(bogie.track);
                return true;
            }
            catch
            {
                uniqueTrack = false;
                logicTrackId = null;
                spanMeters = float.NaN;
                trackLengthMeters = 0f;
                return false;
            }
        }

        private static float ResolveLengthMeters(RailTrack track)
        {
            try
            {
                var curve = track.curve;
                if (curve == null)
                {
                    return 0f;
                }

                var length = curve.length;
                return length > 0f ? length : 0f;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
