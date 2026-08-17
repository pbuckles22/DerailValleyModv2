using System;
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
        private MapPhase _phase;
        private RailTrack[]? _tracks;
        private Junction[]? _junctions;
        private int _trackIndex;
        private int _junctionIndex;
        private bool _wasInWorld;
        private int _generation;

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

                    var otherId = other.GetInstanceID();
                    _graph.AddEdge(stemId, otherId, 1f);
                    _graph.AddEdge(otherId, stemId, 1f);
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

                var fromId = track.GetInstanceID();
                _graph.EnsureNode(fromId);
                if (track.outJunction == null && track.outIsConnected)
                {
                    AddPlain(fromId, track.outBranch.track);
                }

                if (track.inJunction == null && track.inIsConnected)
                {
                    AddPlain(fromId, track.inBranch.track);
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

        private void FinishMap()
        {
            _pump.AddCompleted(1);
            _graph.Freeze();
            _pump.Complete();
            _phase = MapPhase.None;
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
