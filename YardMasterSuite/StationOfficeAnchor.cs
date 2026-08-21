using UnityEngine;

namespace YardMasterSuite
{
    /// <summary>
    /// In-zone station office world position (StationJobGenerationRange transform).
    /// Caches the range; rescan only when the player leaves the zone.
    /// </summary>
    internal static class StationOfficeAnchor
    {
        private static StationJobGenerationRange? _range;
        private static string? _yardId;

        public static void Clear()
        {
            _range = null;
            _yardId = null;
        }

        public static bool TryGet(out Vector3 office, out float playerX, out float playerZ) =>
            TryGet(out office, out playerX, out playerZ, out _);

        public static bool TryGet(
            out Vector3 office,
            out float playerX,
            out float playerZ,
            out string? yardId)
        {
            office = default;
            playerX = 0f;
            playerZ = 0f;
            yardId = null;
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                Clear();
                return false;
            }

            var pos = player.position;
            playerX = pos.x;
            playerZ = pos.z;

            if (_range != null)
            {
                var sqr = _range.PlayerSqrDistanceFromStationCenter;
                if (_range.IsPlayerInJobGenerationZone(sqr))
                {
                    office = _range.transform.position;
                    yardId = _yardId;
                    return true;
                }

                Clear();
            }

            if (!TryScan(out var range, out var id) || range == null)
            {
                return false;
            }

            _range = range;
            _yardId = id;
            yardId = id;
            office = range.transform.position;
            return true;
        }

        private static bool TryScan(out StationJobGenerationRange? range, out string? yardId)
        {
            range = null;
            yardId = null;
            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return false;
            }

            StationController? bestStation = null;
            StationJobGenerationRange? best = null;
            var bestSqr = float.MaxValue;
            for (var i = 0; i < stations.Count; i++)
            {
                var candidate = stations[i];
                if (candidate == null || !candidate.StationInfoValid)
                {
                    continue;
                }

                var jobRange = candidate.GetComponent<StationJobGenerationRange>();
                if (jobRange == null)
                {
                    continue;
                }

                var sqr = jobRange.PlayerSqrDistanceFromStationCenter;
                if (!jobRange.IsPlayerInJobGenerationZone(sqr) || sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = jobRange;
                bestStation = candidate;
            }

            range = best;
            yardId = YardIdOf(bestStation);
            return best != null;
        }

        private static string? YardIdOf(StationController? station)
        {
            if (station == null)
            {
                return null;
            }

            try
            {
                var id = station.stationInfo?.YardID;
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return id;
                }

                var name = station.stationInfo?.Name;
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch
            {
                return null;
            }
        }
    }
}
