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

        public static void Clear()
        {
            _range = null;
        }

        public static bool TryGet(out Vector3 office, out float playerX, out float playerZ)
        {
            office = default;
            playerX = 0f;
            playerZ = 0f;
            var player = PlayerManager.PlayerTransform;
            if (player == null)
            {
                _range = null;
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
                    return true;
                }

                _range = null;
            }

            if (!TryScan(out var range) || range == null)
            {
                return false;
            }

            _range = range;
            office = range.transform.position;
            return true;
        }

        private static bool TryScan(out StationJobGenerationRange? range)
        {
            range = null;
            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return false;
            }

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
            }

            range = best;
            return best != null;
        }
    }
}
