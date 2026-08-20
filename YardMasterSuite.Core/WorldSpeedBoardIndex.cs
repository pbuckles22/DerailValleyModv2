using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// Persistent track-keyed posted boards. Seeds Limit from a board already
    /// behind the loco (cold start). Same-travel only. Does not allocate on
    /// <see cref="SeedBehind"/>.
    /// </summary>
    public sealed class WorldSpeedBoardIndex
    {
        public readonly struct Pin
        {
            public Pin(
                int trackId,
                float kmh,
                float worldX,
                float worldY,
                float worldZ,
                float travelX,
                float travelZ)
            {
                TrackId = trackId;
                Kmh = kmh;
                WorldX = worldX;
                WorldY = worldY;
                WorldZ = worldZ;
                TravelX = travelX;
                TravelZ = travelZ;
            }

            public int TrackId { get; }
            public float Kmh { get; }
            public float WorldX { get; }
            public float WorldY { get; }
            public float WorldZ { get; }
            public float TravelX { get; }
            public float TravelZ { get; }
        }

        private readonly Dictionary<long, Pin> _byKey = new Dictionary<long, Pin>();
        private readonly Dictionary<int, List<long>> _keysByTrack = new Dictionary<int, List<long>>();

        public int Count => _byKey.Count;

        public void Clear()
        {
            _byKey.Clear();
            _keysByTrack.Clear();
        }

        public int CountForTrack(int trackId)
        {
            return _keysByTrack.TryGetValue(trackId, out var keys) ? keys.Count : 0;
        }

        public bool TryGetFirst(int trackId, out Pin pin)
        {
            pin = default;
            if (!_keysByTrack.TryGetValue(trackId, out var keys) || keys.Count == 0)
            {
                return false;
            }

            return _byKey.TryGetValue(keys[0], out pin);
        }

        public void Remember(
            int trackId,
            float kmh,
            float worldX,
            float worldY,
            float worldZ,
            float travelX,
            float travelZ)
        {
            if (trackId == 0 || !IsFinite(kmh) || kmh <= 0f)
            {
                return;
            }

            if (!IsFinite(worldX) || !IsFinite(worldY) || !IsFinite(worldZ))
            {
                return;
            }

            if (!TryNormalize(travelX, travelZ, out var tx, out var tz))
            {
                return;
            }

            var key = MakeKey(trackId, kmh, worldX, worldY, worldZ);
            var pin = new Pin(trackId, kmh, worldX, worldY, worldZ, tx, tz);
            if (_byKey.ContainsKey(key))
            {
                _byKey[key] = pin;
                return;
            }

            _byKey[key] = pin;
            if (!_keysByTrack.TryGetValue(trackId, out var list))
            {
                list = new List<long>(4);
                _keysByTrack[trackId] = list;
            }

            list.Add(key);
        }

        /// <summary>Nearest remembered board behind origin on this track, same travel.</summary>
        public float? SeedBehind(
            int trackId,
            float originX,
            float originY,
            float originZ,
            float travelX,
            float travelZ,
            float lookbackMeters)
        {
            if (trackId == 0 || lookbackMeters <= 0f)
            {
                return null;
            }

            if (!_keysByTrack.TryGetValue(trackId, out var keys) || keys.Count == 0)
            {
                return null;
            }

            if (!TryNormalize(travelX, travelZ, out var tx, out var tz))
            {
                return null;
            }

            float? bestKmh = null;
            var bestAlong = float.NegativeInfinity;
            for (var i = 0; i < keys.Count; i++)
            {
                if (!_byKey.TryGetValue(keys[i], out var pin))
                {
                    continue;
                }

                if (!SameTravel(pin, tx, tz))
                {
                    continue;
                }

                var dx = pin.WorldX - originX;
                var dz = pin.WorldZ - originZ;
                var along = (dx * tx) + (dz * tz);
                if (along >= 0f || along < -lookbackMeters)
                {
                    continue;
                }

                if (along <= bestAlong)
                {
                    continue;
                }

                bestAlong = along;
                bestKmh = pin.Kmh;
            }

            return bestKmh;
        }

        public static bool SameTravel(Pin pin, float travelX, float travelZ)
        {
            if (!TryNormalize(travelX, travelZ, out var tx, out var tz))
            {
                return false;
            }

            return (pin.TravelX * tx) + (pin.TravelZ * tz) >= 0.5f;
        }

        public static long MakeKey(int trackId, float kmh, float worldX, float worldY, float worldZ)
        {
            var whole = (int)System.Math.Round(kmh, System.MidpointRounding.AwayFromZero);
            var cx = (int)System.Math.Floor(worldX / 25f);
            var cy = (int)System.Math.Floor(worldY / 25f);
            var cz = (int)System.Math.Floor(worldZ / 25f);
            unchecked
            {
                long h = trackId;
                h = (h * 397) ^ whole;
                h = (h * 397) ^ cx;
                h = (h * 397) ^ cy;
                h = (h * 397) ^ cz;
                return h;
            }
        }

        private static bool TryNormalize(float x, float z, out float nx, out float nz)
        {
            var len = (float)System.Math.Sqrt((x * x) + (z * z));
            if (len < 1e-4f)
            {
                nx = nz = 0f;
                return false;
            }

            nx = x / len;
            nz = z / len;
            return true;
        }

        private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}
