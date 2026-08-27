using System.Collections.Generic;
using System.Text;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Hybrid FoT → Core resolver for Town TT (**8.4**). Call only from desk Set dest /
    /// Recheck — never HUD tick. Candidates are cached until <see cref="Invalidate"/>
    /// so a second click does not re-FoT (v1 0.6.49 stutter lesson).
    /// </summary>
    internal static class TurntableLocator
    {
        private static List<TurntableCandidate>? _cached;
        private static string? _lastDiag;

        public static void Invalidate()
        {
            _cached = null;
            _lastDiag = null;
        }

        /// <summary>Cached FoT candidates for Switch List TurnAround inject (**8.5**).</summary>
        public static IReadOnlyList<TurntableCandidate>? PeekCandidates(float originX, float originZ) =>
            EnsureCandidates(originX, originZ);

        /// <summary>
        /// Finds turntable rail for <paramref name="yardId"/> (prefer yard meta; else nearest in town).
        /// </summary>
        public static string? TryResolveTrackId(string yardId, float originX, float originZ)
        {
            if (string.IsNullOrWhiteSpace(yardId))
            {
                return null;
            }

            var candidates = EnsureCandidates(originX, originZ);
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            if (_lastDiag != null)
            {
                MapsDeskPanel.EmitLog?.Invoke(_lastDiag);
                _lastDiag = null;
            }

            return TurntableTrackResolver.PickBest(
                yardId,
                candidates,
                TurntableTrackResolver.DefaultNearestFallbackMaxMeters,
                playerYardId: TryPlayerYardId());
        }

        private static List<TurntableCandidate>? EnsureCandidates(float originX, float originZ)
        {
            if (_cached != null)
            {
                return _cached;
            }

            try
            {
                var tables = Object.FindObjectsOfType<TurntableController>();
                if (tables == null || tables.Length == 0)
                {
                    MapsDeskPanel.EmitLog?.Invoke("T2 maps: TT FoT count=0");
                    _cached = new List<TurntableCandidate>(0);
                    return _cached;
                }

                var candidates = new List<TurntableCandidate>(tables.Length);
                var skippedNoKey = 0;
                for (var i = 0; i < tables.Length; i++)
                {
                    var ctrl = tables[i];
                    if (ctrl == null || ctrl.turntable == null)
                    {
                        continue;
                    }

                    RailTrack? rail = null;
                    try
                    {
                        rail = ctrl.turntable.Track;
                    }
                    catch
                    {
                        rail = null;
                    }

                    var key = LogicTrackKey.FromRail(rail);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        skippedNoKey++;
                        continue;
                    }

                    Vector3 p;
                    try
                    {
                        p = rail != null ? rail.transform.position : ctrl.transform.position;
                    }
                    catch
                    {
                        continue;
                    }

                    var yard = MapsDeskCatalog.YardIdOf(rail)
                        ?? DestinationCatalog.YardIdFromTrackKey(key)
                        ?? InferYardNear(p)
                        ?? string.Empty;

                    var dx = p.x - originX;
                    var dz = p.z - originZ;
                    var dist = Mathf.Sqrt((dx * dx) + (dz * dz));
                    candidates.Add(new TurntableCandidate(key!, yard, dist));
                }

                _lastDiag = FormatCandidateDiag(tables.Length, skippedNoKey, candidates);
                _cached = candidates;
                return _cached;
            }
            catch
            {
                return null;
            }
        }

        private static string? TryPlayerYardId()
        {
            try
            {
                var origin = RouteOriginProbe.TryGet();
                if (!string.IsNullOrEmpty(origin))
                {
                    var fromKey = PathRouteConstraints.YardIdOf(origin)
                        ?? DestinationCatalog.YardIdFromTrackKey(origin);
                    if (LocoRadarDisplay.IsUsableCityYardId(fromKey))
                    {
                        return fromKey;
                    }
                }
            }
            catch
            {
                // fall through
            }

            return null;
        }

        private static string FormatCandidateDiag(
            int fotCount,
            int skippedNoKey,
            List<TurntableCandidate> candidates)
        {
            var sb = new StringBuilder(128);
            sb.Append("T2 maps: TT FoT=")
                .Append(fotCount)
                .Append(" cand=")
                .Append(candidates.Count)
                .Append(" noKey=")
                .Append(skippedNoKey);
            var n = Mathf.Min(candidates.Count, 6);
            for (var i = 0; i < n; i++)
            {
                var c = candidates[i];
                sb.Append(" | ")
                    .Append(string.IsNullOrEmpty(c.YardId) ? "—" : c.YardId)
                    .Append(':')
                    .Append(c.TrackId)
                    .Append('@')
                    .Append(c.DistanceMeters.ToString("0"));
            }

            return sb.ToString();
        }

        /// <summary>Nearest named yard within 200 m of the turntable (blank bridge meta).</summary>
        private static string? InferYardNear(Vector3 world)
        {
            try
            {
                var tracks = RailTrackRegistry.Instance != null
                    ? RailTrackRegistry.Instance.AllTracks
                    : RailTrackRegistry.RailTracks;
                if (tracks == null || tracks.Length == 0)
                {
                    return null;
                }

                const float radiusSq = 200f * 200f;
                string? best = null;
                var bestDistSq = float.PositiveInfinity;
                var scanned = 0;
                const int maxScan = 256;
                for (var i = 0; i < tracks.Length && scanned < maxScan; i++)
                {
                    var rail = tracks[i];
                    if (rail == null)
                    {
                        continue;
                    }

                    scanned++;
                    Vector3 p;
                    try
                    {
                        p = rail.transform.position;
                    }
                    catch
                    {
                        continue;
                    }

                    var dx = p.x - world.x;
                    var dz = p.z - world.z;
                    var dSq = (dx * dx) + (dz * dz);
                    if (dSq > radiusSq || dSq >= bestDistSq)
                    {
                        continue;
                    }

                    var key = LogicTrackKey.FromRail(rail);
                    var yard = MapsDeskCatalog.YardIdOf(rail)
                        ?? DestinationCatalog.YardIdFromTrackKey(key);
                    if (!LocoRadarDisplay.IsUsableCityYardId(yard))
                    {
                        continue;
                    }

                    bestDistSq = dSq;
                    best = yard;
                }

                return best;
            }
            catch
            {
                return null;
            }
        }
    }
}
