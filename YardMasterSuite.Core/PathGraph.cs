using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>Directed hop between track instance ids. No strings.</summary>
    public readonly struct PathGraphEdge
    {
        public readonly int FromId;
        public readonly int ToId;
        public readonly float Cost;

        public PathGraphEdge(int fromId, int toId, float cost)
        {
            FromId = fromId;
            ToId = toId;
            Cost = cost > 0f ? cost : 1f;
        }
    }

    /// <summary>A* / Dijkstra result. Safe as a Type B mailbox payload.</summary>
    public readonly struct PathSearchResult
    {
        public readonly bool Found;
        public readonly int Hops;
        public readonly float Cost;

        public PathSearchResult(bool found, int hops, float cost)
        {
            Found = found;
            Hops = hops;
            Cost = cost;
        }

        public static PathSearchResult NotFound => default;
    }

    /// <summary>Graph freeze + probe path, published via Type B.</summary>
    public readonly struct PathGraphReady
    {
        public readonly int Generation;
        public readonly int NodeCount;
        public readonly int EdgeCount;
        public readonly bool PathFound;
        public readonly int PathHops;
        public readonly float PathCost;

        public PathGraphReady(
            int generation,
            int nodeCount,
            int edgeCount,
            bool pathFound,
            int pathHops,
            float pathCost)
        {
            Generation = generation;
            NodeCount = nodeCount;
            EdgeCount = edgeCount;
            PathFound = pathFound;
            PathHops = pathHops;
            PathCost = pathCost;
        }
    }

    /// <summary>
    /// Session track graph of int node ids. Built on the main thread in budgeted
    /// ticks; search may run on a worker after <see cref="Freeze"/>.
    /// </summary>
    public sealed class PathGraph
    {
        private readonly List<int> _ids = new List<int>();
        private readonly Dictionary<int, int> _index = new Dictionary<int, int>();
        private readonly List<PathGraphEdge> _edges = new List<PathGraphEdge>();
        private bool _frozen;

        public int NodeCount => _ids.Count;

        public int EdgeCount => _edges.Count;

        public bool IsFrozen => _frozen;

        public int FirstId => _ids.Count > 0 ? _ids[0] : 0;

        public int LastId => _ids.Count > 0 ? _ids[_ids.Count - 1] : 0;

        public IReadOnlyList<PathGraphEdge> Edges => _edges;

        public void Clear()
        {
            _ids.Clear();
            _index.Clear();
            _edges.Clear();
            _frozen = false;
        }

        public void Freeze()
        {
            _frozen = true;
        }

        public bool TryIndex(int id, out int index)
        {
            return _index.TryGetValue(id, out index);
        }

        public void EnsureNode(int id)
        {
            if (_frozen || _index.ContainsKey(id))
            {
                return;
            }

            _index[id] = _ids.Count;
            _ids.Add(id);
        }

        public void AddEdge(int fromId, int toId, float cost)
        {
            if (_frozen || fromId == 0 || toId == 0)
            {
                return;
            }

            EnsureNode(fromId);
            EnsureNode(toId);
            _edges.Add(new PathGraphEdge(fromId, toId, cost));
        }
    }
}
