using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// One-shot local RailTrack + Junction + board dump (9.1.3 Win 0).
    /// Sit still, switch already thrown. Not TrackPathAhead. Not full-map cache.
    /// </summary>
    internal static class TrackGraphDump
    {
        internal static Action<string>? EmitLog;

        internal const float FailedScanCooldownSeconds = 2f;

        internal static string? Write(
            string? origin,
            float locoX,
            float locoY,
            float locoZ,
            float forwardX,
            float forwardZ,
            IReadOnlyList<ParsedPostedBoard> boards,
            int boardCount)
        {
            var trackMap = new Dictionary<int, HarvestedTrack>(256);
            var junctions = new List<HarvestedJunction>(64);
            var graphBoards = new List<HarvestedGraphBoard>(32);
            Collect(locoX, locoZ, boards, boardCount, trackMap, junctions, graphBoards);
            if (trackMap.Count == 0 || junctions.Count == 0)
            {
                return null;
            }

            var tracks = new HarvestedTrack[trackMap.Count];
            trackMap.Values.CopyTo(tracks, 0);
            var juncArr = junctions.ToArray();
            var boardArr = graphBoards.ToArray();
            var text = TrackGraphHarvestCodec.Format(
                origin,
                locoX,
                locoY,
                locoZ,
                forwardX,
                forwardZ,
                tracks,
                tracks.Length,
                juncArr,
                juncArr.Length,
                boardArr,
                boardArr.Length);
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var name = "graph-" + (string.IsNullOrEmpty(origin) ? "path" : origin!.ToLowerInvariant())
                + "-" + stamp + ".txt";
            return WriteFile(
                name,
                text,
                "T2 harvest: graph trackN=" + tracks.Length.ToString()
                    + " juncN=" + juncArr.Length.ToString()
                    + " boardN=" + boardArr.Length.ToString());
        }

        private static void Collect(
            float locoX,
            float locoZ,
            IReadOnlyList<ParsedPostedBoard> boards,
            int boardCount,
            Dictionary<int, HarvestedTrack> tracks,
            List<HarvestedJunction> junctions,
            List<HarvestedGraphBoard> graphBoards)
        {
            var rails = ResolveTracks();
            if (rails != null)
            {
                for (var i = 0; i < rails.Length; i++)
                {
                    var rail = rails[i];
                    if (rail == null || !TryHarvestTrack(rail, out var harvested))
                    {
                        continue;
                    }

                    if (TrackGraphHarvestPolicy.IncludeTrack(
                            locoX,
                            locoZ,
                            harvested.InX,
                            harvested.InZ,
                            harvested.OutX,
                            harvested.OutZ))
                    {
                        tracks[harvested.Id] = harvested;
                    }
                }
            }

            var worldJunctions = ResolveJunctions();
            if (worldJunctions != null)
            {
                for (var i = 0; i < worldJunctions.Length; i++)
                {
                    var junction = worldJunctions[i];
                    if (junction == null)
                    {
                        continue;
                    }

                    if (!TryJunctionXz(junction, out var jx, out var jz)
                        || !TrackGraphHarvestPolicy.IsWithinRadius(locoX, locoZ, jx, jz))
                    {
                        continue;
                    }

                    if (!TryHarvestJunction(junction, tracks, out var harvestedJunc))
                    {
                        continue;
                    }

                    junctions.Add(harvestedJunc);
                }
            }

            var n = boardCount;
            if (boards != null && n > boards.Count)
            {
                n = boards.Count;
            }

            for (var i = 0; i < n; i++)
            {
                var b = boards![i];
                if (!TrackGraphHarvestPolicy.IsWithinRadius(locoX, locoZ, b.X, b.Z))
                {
                    continue;
                }

                graphBoards.Add(
                    new HarvestedGraphBoard(
                        b.InstanceId,
                        b.X,
                        b.Z,
                        b.ThroughKmh,
                        b.DivergeKmh,
                        b.ForwardX,
                        b.ForwardZ,
                        b.IsDual,
                        b.JunctionNearby));
            }
        }

        private static bool TryHarvestJunction(
            Junction junction,
            Dictionary<int, HarvestedTrack> tracks,
            out HarvestedJunction harvested)
        {
            harvested = default;
            int id;
            int selected;
            try
            {
                id = junction.GetInstanceID();
                selected = junction.selectedBranch;
            }
            catch
            {
                return false;
            }

            RailTrack? stem;
            try
            {
                stem = junction.inBranch.track;
            }
            catch
            {
                return false;
            }

            if (stem == null)
            {
                return false;
            }

            EnsureTrack(stem, tracks);
            var stemId = stem.GetInstanceID();
            var leftId = 0;
            var rightId = 0;
            try
            {
                var outs = junction.outBranches;
                if (outs != null && outs.Count > 0)
                {
                    var left = outs[0].track;
                    if (left != null)
                    {
                        EnsureTrack(left, tracks);
                        leftId = left.GetInstanceID();
                    }

                    if (outs.Count > 1)
                    {
                        var right = outs[1].track;
                        if (right != null)
                        {
                            EnsureTrack(right, tracks);
                            rightId = right.GetInstanceID();
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            if (selected < 0)
            {
                selected = 0;
            }

            harvested = new HarvestedJunction(id, stemId, leftId, rightId, selected);
            return true;
        }

        private static void EnsureTrack(RailTrack track, Dictionary<int, HarvestedTrack> tracks)
        {
            if (!TryHarvestTrack(track, out var harvested))
            {
                return;
            }

            tracks[harvested.Id] = harvested;
        }

        private static bool TryHarvestTrack(RailTrack track, out HarvestedTrack harvested)
        {
            harvested = default;
            if (!TryEndpoints(track, out var inPos, out var outPos, out var lengthMeters))
            {
                return false;
            }

            int id;
            try
            {
                id = track.GetInstanceID();
            }
            catch
            {
                return false;
            }

            harvested = new HarvestedTrack(id, inPos.x, inPos.z, outPos.x, outPos.z, lengthMeters);
            return true;
        }

        private static bool TryEndpoints(
            RailTrack track,
            out Vector3 inPosition,
            out Vector3 outPosition,
            out float lengthMeters)
        {
            inPosition = Vector3.zero;
            outPosition = Vector3.zero;
            lengthMeters = 0f;
            try
            {
                var curve = track.curve;
                if (curve == null || curve.pointCount < 2)
                {
                    return false;
                }

                var first = curve[0];
                var last = curve[curve.pointCount - 1];
                if (first == null || last == null)
                {
                    return false;
                }

                inPosition = first.position;
                outPosition = last.position;
                lengthMeters = curve.length;
                if (lengthMeters <= 0f)
                {
                    lengthMeters = Vector3.Distance(inPosition, outPosition);
                }

                return lengthMeters > 0f;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryJunctionXz(Junction junction, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            try
            {
                var p = junction.transform.position;
                x = p.x;
                z = p.z;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static RailTrack[]? ResolveTracks()
        {
            try
            {
                var tracks = RailTrackRegistry.Instance != null
                    ? RailTrackRegistry.Instance.AllTracks
                    : null;
                if (tracks != null && tracks.Length > 0)
                {
                    return tracks;
                }

                tracks = RailTrackRegistry.RailTracks;
                return tracks != null && tracks.Length > 0 ? tracks : null;
            }
            catch
            {
                return null;
            }
        }

        private static Junction[]? ResolveJunctions()
        {
            try
            {
                var junctions = RailTrackRegistry.Junctions;
                return junctions != null && junctions.Length > 0 ? junctions : null;
            }
            catch
            {
                return null;
            }
        }

        private static string? WriteFile(string fileName, string text, string logLine)
        {
            try
            {
                var dir = RouteHarvestDump.DirectoryPath;
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, fileName);
                RouteHarvestFiles.Replace(path, text);
                EmitLog?.Invoke(logLine + " file=" + path);
                return path;
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke("T2 harvest: graph write " + ex.GetType().Name);
                return null;
            }
        }
    }
}
