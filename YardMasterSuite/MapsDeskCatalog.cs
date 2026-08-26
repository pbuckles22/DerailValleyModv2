using System;
using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Time-sliced city/track catalog for the Maps desk (v1 PathGraphBuilder catalog
    /// half). No graph edges and no Dijkstra — mapping starts when the desk opens.
    /// </summary>
    internal static class MapsDeskCatalog
    {
        public const int BudgetPerFrame = PathGraphBuildPump.MaxUnitsPerTick;

        private enum Phase
        {
            None,
            Tracks,
            Stations,
            Done,
        }

        private static readonly PathGraphBuildPump Pump = new PathGraphBuildPump();
        private static readonly List<(string YardId, string TrackId)> Entries = new List<(string, string)>(256);
        private static readonly HashSet<string> Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static Phase _phase;
        private static RailTrack[]? _tracks;
        private static int _trackIndex;
        private static bool _stationsDone;

        public static bool IsMapping => Pump.IsMapping;

        public static bool HasReady => _phase == Phase.Done && Entries.Count > 0;

        public static IReadOnlyList<(string YardId, string TrackId)> Catalog => Entries;

        public static float Progress01 => Pump.Progress01;

        public static string MappingBanner
        {
            get
            {
                if (!Pump.IsMapping)
                {
                    return string.Empty;
                }

                var pct = (int)Math.Round(Pump.Progress01 * 100.0, MidpointRounding.AwayFromZero);
                return "Station mapping… " + pct + "%";
            }
        }

        public static void Invalidate()
        {
            Pump.Reset();
            Entries.Clear();
            Seen.Clear();
            _phase = Phase.None;
            _tracks = null;
            _trackIndex = 0;
            _stationsDone = false;
        }

        public static void EnsureStarted()
        {
            if (HasReady || Pump.IsMapping)
            {
                return;
            }

            Invalidate();
            _tracks = ResolveTracks();
            var total = _tracks != null ? _tracks.Length : 0;
            total += 1;
            Pump.Begin(total);
            _phase = Phase.Tracks;
            _trackIndex = 0;
        }

        /// <summary>Returns true on the frame mapping finishes.</summary>
        public static bool Tick(int budget = BudgetPerFrame)
        {
            if (!Pump.IsMapping)
            {
                return false;
            }

            if (budget < 1)
            {
                budget = 1;
            }

            while (budget > 0 && Pump.IsMapping)
            {
                if (_phase == Phase.Tracks)
                {
                    budget = TickTracks(budget);
                }
                else if (_phase == Phase.Stations)
                {
                    AppendStationCatalog();
                    Pump.AddCompleted(1);
                    _phase = Phase.Done;
                    Pump.Complete();
                    return true;
                }
                else
                {
                    break;
                }
            }

            return _phase == Phase.Done;
        }

        private static int TickTracks(int budget)
        {
            var tracks = _tracks;
            if (tracks == null || _trackIndex >= tracks.Length)
            {
                _phase = Phase.Stations;
                return budget;
            }

            while (budget > 0 && _trackIndex < tracks.Length)
            {
                var rail = tracks[_trackIndex++];
                budget--;
                Pump.AddCompleted(1);
                TryAddRail(rail);
            }

            if (_trackIndex >= tracks.Length)
            {
                _phase = Phase.Stations;
            }

            return budget;
        }

        private static void TryAddRail(RailTrack? rail)
        {
            if (rail == null)
            {
                return;
            }

            var display = LogicTrackKey.FromRail(rail);
            var yard = YardIdOf(rail) ?? DestinationCatalog.YardIdFromTrackKey(display);
            TryAddEntry(yard, display);
        }

        private static void AppendStationCatalog()
        {
            if (_stationsDone)
            {
                return;
            }

            _stationsDone = true;
            try
            {
                var stations = StationController.allStations;
                if (stations == null || stations.Count == 0)
                {
                    return;
                }

                for (var s = 0; s < stations.Count; s++)
                {
                    var station = stations[s];
                    if (station == null)
                    {
                        continue;
                    }

                    var yard = station.stationInfo?.YardID?.Trim();
                    var list = station.AllStationTracks;
                    if (list == null)
                    {
                        continue;
                    }

                    foreach (var rail in list)
                    {
                        if (rail == null)
                        {
                            continue;
                        }

                        TryAddEntry(yard, LogicTrackKey.FromRail(rail));
                    }
                }
            }
            catch
            {
                // Station list unavailable — registry catalog only.
            }
        }

        private static void TryAddEntry(string? yard, string? track)
        {
            if (!DestinationCatalog.TryAdd(Entries, yard, track))
            {
                return;
            }

            var token = yard!.Trim() + "\0" + track!.Trim();
            if (!Seen.Add(token))
            {
                Entries.RemoveAt(Entries.Count - 1);
            }
        }

        internal static string? YardIdOf(RailTrack? rail)
        {
            try
            {
                var map = RailTrackRegistry.RailTrackToLogicTrack;
                if (map != null && map.TryGetValue(rail, out var logic) && logic?.ID != null)
                {
                    var yard = logic.ID.yardId?.Trim();
                    if (LocoRadarDisplay.IsUsableCityYardId(yard))
                    {
                        return yard;
                    }
                }
            }
            catch
            {
                // fall through
            }

            return DestinationCatalog.YardIdFromTrackKey(LogicTrackKey.FromRail(rail));
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
    }
}
