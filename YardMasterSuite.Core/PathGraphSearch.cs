using System.Collections.Generic;

namespace YardMasterSuite.Core
{
    /// <summary>
    /// A* with a zero heuristic (Dijkstra). Runs on a frozen <see cref="PathGraph"/>
    /// so a worker may call it without touching Unity APIs.
    /// </summary>
    public static class PathGraphSearch
    {
        public static PathSearchResult Find(PathGraph graph, int startId, int goalId)
        {
            if (graph == null || graph.NodeCount == 0)
            {
                return PathSearchResult.NotFound;
            }

            if (!graph.TryIndex(startId, out var start) || !graph.TryIndex(goalId, out var goal))
            {
                return PathSearchResult.NotFound;
            }

            if (start == goal)
            {
                return new PathSearchResult(true, 0, 0f);
            }

            var n = graph.NodeCount;
            var adj = BuildAdj(graph, n);
            var gScore = new float[n];
            var hops = new int[n];
            var closed = new bool[n];
            for (var i = 0; i < n; i++)
            {
                gScore[i] = float.PositiveInfinity;
            }

            gScore[start] = 0f;
            var openCount = n;
            while (openCount > 0)
            {
                var u = -1;
                var best = float.PositiveInfinity;
                for (var i = 0; i < n; i++)
                {
                    if (closed[i] || gScore[i] >= best)
                    {
                        continue;
                    }

                    best = gScore[i];
                    u = i;
                }

                if (u < 0)
                {
                    break;
                }

                if (u == goal)
                {
                    return new PathSearchResult(true, hops[u], gScore[u]);
                }

                closed[u] = true;
                openCount--;
                var neighbors = adj[u];
                if (neighbors == null)
                {
                    continue;
                }

                for (var k = 0; k < neighbors.Count; k++)
                {
                    var e = neighbors[k];
                    if (!graph.TryIndex(e.ToId, out var v) || closed[v])
                    {
                        continue;
                    }

                    var tentative = gScore[u] + e.Cost;
                    if (tentative >= gScore[v])
                    {
                        continue;
                    }

                    gScore[v] = tentative;
                    hops[v] = hops[u] + 1;
                }
            }

            return PathSearchResult.NotFound;
        }

        private static List<PathGraphEdge>?[] BuildAdj(PathGraph graph, int n)
        {
            var adj = new List<PathGraphEdge>?[n];
            var edges = graph.Edges;
            for (var i = 0; i < edges.Count; i++)
            {
                var e = edges[i];
                if (!graph.TryIndex(e.FromId, out var from))
                {
                    continue;
                }

                var list = adj[from];
                if (list == null)
                {
                    list = new List<PathGraphEdge>(4);
                    adj[from] = list;
                }

                list.Add(e);
            }

            return adj;
        }
    }
}
