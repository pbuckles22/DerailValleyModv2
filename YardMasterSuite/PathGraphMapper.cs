using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Time-sliced RailTrack walk (v1 pump). Copies hops into a struct graph,
    /// then a worker runs A* and publishes <see cref="PathGraphReady"/> via Type B.
    /// </summary>
    public sealed class PathGraphMapper : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        internal static Func<bool>? IsWorldSession;

        private enum MapPhase
        {
            None,
            Junctions,
            Plains,
            Search,
        }

        private readonly PathGraph _graph = new PathGraph();
        private readonly PathGraphBuildPump _pump = new PathGraphBuildPump();
        private readonly List<PathEdge> _checkEdges = new List<PathEdge>(512);
        private readonly Dictionary<string, Junction> _junctionsById = new Dictionary<string, Junction>(128);
        private readonly Dictionary<string, float> _enterCost = new Dictionary<string, float>(512, StringComparer.Ordinal);
        private readonly Dictionary<string, PathTrackClass> _classByKey = new Dictionary<string, PathTrackClass>(512, StringComparer.Ordinal);
        private readonly Dictionary<string, RailTrack> _railsByKey = new Dictionary<string, RailTrack>(512, StringComparer.Ordinal);
        private bool _checkFrozen;
        private MapPhase _phase;
        private RailTrack[]? _tracks;
        private Junction[]? _junctions;
        private int _trackIndex;
        private int _junctionIndex;
        private bool _wasInWorld;
        private int _generation;

        internal bool HasFrozenPathCheck => _checkFrozen;

        internal IReadOnlyList<PathEdge> PathCheckEdges => _checkEdges;

        internal PathTrackClass ClassFor(string trackId) =>
            _classByKey.TryGetValue(trackId, out var cls) ? cls : PathTrackClass.Unknown;

        internal bool TryGetJunction(string junctionId, out Junction? junction) =>
            _junctionsById.TryGetValue(junctionId, out junction);

        internal bool TryGetRailTrack(string trackKey, out RailTrack? rail) =>
            _railsByKey.TryGetValue(trackKey, out rail);

        /// <summary>Frozen rail keys for nearby pivot / TT inject (**8.5**).</summary>
        internal IEnumerable<KeyValuePair<string, RailTrack>> EnumerateRails() => _railsByKey;

        internal int JunctionFingerprint()
        {
            var hash = 17;
            var junctions = _junctions;
            if (junctions == null)
            {
                return 0;
            }

            for (var i = 0; i < junctions.Length; i++)
            {
                var junction = junctions[i];
                if (junction == null)
                {
                    continue;
                }

                hash = (hash * 31) + junction.GetInstanceID();
                hash = (hash * 31) + junction.selectedBranch;
            }

            return hash;
        }

        internal void CopyJunctionSelected(Dictionary<string, int> dest)
        {
            dest.Clear();
            var junctions = _junctions;
            if (junctions == null)
            {
                return;
            }

            for (var i = 0; i < junctions.Length; i++)
            {
                var junction = junctions[i];
                if (junction == null)
                {
                    continue;
                }

                dest[junction.GetInstanceID().ToString()] = junction.selectedBranch;
            }
        }

        internal List<RouteHarvestJunction> CopyJunctionWorld()
        {
            var list = new List<RouteHarvestJunction>(_junctionsById.Count);
            foreach (var kv in _junctionsById)
            {
                var junction = kv.Value;
                if (junction == null)
                {
                    continue;
                }

                try
                {
                    var p = junction.transform.position;
                    list.Add(new RouteHarvestJunction(kv.Key, p.x, p.z, junction.selectedBranch));
                }
                catch
                {
                    // skip
                }
            }

            return list;
        }

        private void OnEnable()
        {
            YmsEventBus.OnPathGraphReady += OnPathGraphReady;
            ResetMap();
            _wasInWorld = false;
        }

        private void OnDisable()
        {
            YmsEventBus.OnPathGraphReady -= OnPathGraphReady;
            Interlocked.Increment(ref _generation);
            ResetMap();
        }

        private void Update()
        {
            var inWorld = IsWorldSession?.Invoke() ?? false;
            if (inWorld && !_wasInWorld)
            {
                BeginMap();
            }
            else if (!inWorld && _wasInWorld)
            {
                Interlocked.Increment(ref _generation);
                ResetMap();
            }

            _wasInWorld = inWorld;
            if (_pump.IsMapping)
            {
                TickMap();
            }
        }

        private void OnPathGraphReady(PathGraphReady ready)
        {
            EmitLog?.Invoke(PathGraphTelemetry.FormatReady(ready));
        }

        private void BeginMap()
        {
            ResetMap();
            Interlocked.Increment(ref _generation);
            _tracks = ResolveTracks();
            _junctions = ResolveJunctions();
            var trackCount = _tracks?.Length ?? 0;
            var junctionCount = _junctions?.Length ?? 0;
            if (trackCount == 0)
            {
                _pump.Begin(1);
                _pump.Fail();
                EmitLog?.Invoke(PathGraphTelemetry.FormatFail());
                return;
            }

            var units = junctionCount + trackCount + 1;
            _pump.Begin(units);
            _phase = MapPhase.Junctions;
            _junctionIndex = 0;
            _trackIndex = 0;
            EmitLog?.Invoke(PathGraphTelemetry.FormatStart(units));
        }

        private void TickMap()
        {
            var budget = _pump.BudgetThisTick(PathGraphBuildPump.MaxUnitsPerTick);
            while (budget > 0 && _pump.IsMapping)
            {
                switch (_phase)
                {
                    case MapPhase.Junctions:
                        budget = TickJunctions(budget);
                        break;
                    case MapPhase.Plains:
                        budget = TickPlains(budget);
                        break;
                    case MapPhase.Search:
                        FinishMap();
                        return;
                    default:
                        _pump.Fail();
                        EmitLog?.Invoke(PathGraphTelemetry.FormatFail());
                        return;
                }
            }
        }

        private int TickJunctions(int budget)
        {
            var junctions = _junctions;
            if (junctions == null || _junctionIndex >= junctions.Length)
            {
                _phase = MapPhase.Plains;
                return budget;
            }

            while (budget > 0 && _junctionIndex < junctions.Length)
            {
                var junction = junctions[_junctionIndex++];
                budget--;
                _pump.AddCompleted(1);
                if (junction == null)
                {
                    continue;
                }

                var stem = junction.inBranch.track;
                if (stem == null)
                {
                    continue;
                }

                var stemId = stem.GetInstanceID();
                var stemKey = LogicTrackKey.FromRail(stem);
                PathTrackProbe.RegisterTrack(stem, stemKey, _enterCost, _classByKey, _railsByKey);
                var junctionKey = junction.GetInstanceID().ToString();
                _junctionsById[junctionKey] = junction;
                var outs = junction.outBranches;
                if (outs == null)
                {
                    continue;
                }

                for (var i = 0; i < outs.Count; i++)
                {
                    var other = outs[i].track;
                    if (other == null)
                    {
                        continue;
                    }

                    var otherKey = LogicTrackKey.FromRail(other);
                    PathTrackProbe.RegisterTrack(other, otherKey, _enterCost, _classByKey, _railsByKey);
                    var otherId = other.GetInstanceID();
                    _graph.AddEdge(stemId, otherId, 1f);
                    _graph.AddEdge(otherId, stemId, 1f);
                    AddCheckHop(stemKey, LogicTrackKey.FromRail(other), junctionKey, i);
                }
            }

            if (_junctionIndex >= junctions.Length)
            {
                _phase = MapPhase.Plains;
            }

            return budget;
        }

        private int TickPlains(int budget)
        {
            var tracks = _tracks;
            if (tracks == null)
            {
                _phase = MapPhase.Search;
                return budget;
            }

            while (budget > 0 && _trackIndex < tracks.Length)
            {
                var track = tracks[_trackIndex++];
                budget--;
                _pump.AddCompleted(1);
                if (track == null)
                {
                    continue;
                }

                var trackKey = LogicTrackKey.FromRail(track);
                PathTrackProbe.RegisterTrack(track, trackKey, _enterCost, _classByKey, _railsByKey);

                var fromId = track.GetInstanceID();
                _graph.EnsureNode(fromId);
                if (track.outJunction == null && track.outIsConnected)
                {
                    AddPlain(fromId, track.outBranch.track);
                    AddCheckPlain(LogicTrackKey.FromRail(track), track.outBranch.track);
                }

                if (track.inJunction == null && track.inIsConnected)
                {
                    AddPlain(fromId, track.inBranch.track);
                    AddCheckPlain(LogicTrackKey.FromRail(track), track.inBranch.track);
                }
            }

            if (_trackIndex >= tracks.Length)
            {
                _phase = MapPhase.Search;
            }

            return budget;
        }

        private void AddPlain(int fromId, RailTrack? other)
        {
            if (other == null)
            {
                return;
            }

            var toId = other.GetInstanceID();
            _graph.AddEdge(fromId, toId, 1f);
            _graph.AddEdge(toId, fromId, 1f);
        }

        private void AddCheckHop(string? fromKey, string? toKey, string junctionKey, int requiredBranch)
        {
            if (fromKey == null || toKey == null)
            {
                return;
            }

            var cost = HopCost(toKey, junctionHop: true);
            _checkEdges.Add(new PathEdge(fromKey, toKey, junctionKey, requiredBranch, cost));
            _checkEdges.Add(new PathEdge(toKey, fromKey, junctionKey, requiredBranch, cost));
        }

        private void AddCheckPlain(string? fromKey, RailTrack? other)
        {
            var toKey = LogicTrackKey.FromRail(other);
            if (fromKey == null || toKey == null)
            {
                return;
            }

            var cost = HopCost(toKey, junctionHop: false);
            _checkEdges.Add(new PathEdge(fromKey, toKey, cost: cost));
            _checkEdges.Add(new PathEdge(toKey, fromKey, cost: cost));
        }

        private float HopCost(string toKey, bool junctionHop)
        {
            if (!_enterCost.TryGetValue(toKey, out var cost) || cost <= 0f)
            {
                cost = PathTrackCosts.TravelSeconds(
                    PathTrackCosts.MinLengthMeters,
                    null,
                    ClassFor(toKey));
            }

            if (junctionHop)
            {
                cost += PathTrackCosts.JunctionPenaltySeconds;
            }

            return cost;
        }

        private void FinishMap()
        {
            _pump.AddCompleted(1);
            _graph.Freeze();
            _checkFrozen = true;
            _pump.Complete();
            _phase = MapPhase.None;
            RouteHarvestDump.WriteGraph(this);
            var gen = _generation;
            var graph = _graph;
            var start = graph.FirstId;
            var goal = graph.LastId;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                if (Volatile.Read(ref _generation) != gen)
                {
                    return;
                }

                var path = PathGraphSearch.Find(graph, start, goal);
                if (Volatile.Read(ref _generation) != gen)
                {
                    return;
                }

                YmsEventBus.PathGraph.Enqueue(new PathGraphReady(
                    gen,
                    graph.NodeCount,
                    graph.EdgeCount,
                    path.Found,
                    path.Hops,
                    path.Cost));
            });
        }

        private void ResetMap()
        {
            _pump.Reset();
            _graph.Clear();
            _checkEdges.Clear();
            _junctionsById.Clear();
            _enterCost.Clear();
            _classByKey.Clear();
            _railsByKey.Clear();
            _checkFrozen = false;
            _phase = MapPhase.None;
            _tracks = null;
            _junctions = null;
            _trackIndex = 0;
            _junctionIndex = 0;
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
    }
}
