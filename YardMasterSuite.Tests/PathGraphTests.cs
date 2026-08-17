using System;
using System.Collections.Generic;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PathGraphBuildPumpTests
{
    [Fact]
    public void Begin_starts_mapping_at_zero_progress()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(200);

        Assert.True(pump.IsMapping);
        Assert.Equal(PathGraphBuildPump.State.Mapping, pump.Current);
        Assert.Equal(200, pump.TotalUnits);
        Assert.Equal(0, pump.CompletedUnits);
        Assert.Equal(0f, pump.Progress01);
        Assert.Equal(200, pump.RemainingUnits);
    }

    [Fact]
    public void AddCompleted_advances_progress_in_budget_chunks()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(100);

        pump.AddCompleted(40);
        Assert.Equal(0.4f, pump.Progress01, precision: 3);
        Assert.Equal(60, pump.RemainingUnits);

        pump.AddCompleted(60);
        Assert.Equal(1f, pump.Progress01, precision: 3);
        Assert.Equal(0, pump.RemainingUnits);
        Assert.True(pump.IsMapping);
    }

    [Fact]
    public void Complete_marks_ready()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(80);
        pump.AddCompleted(20);
        pump.Complete();

        Assert.Equal(PathGraphBuildPump.State.Ready, pump.Current);
        Assert.False(pump.IsMapping);
        Assert.Equal(1f, pump.Progress01);
    }

    [Fact]
    public void Fail_and_Reset_leave_not_mapping()
    {
        var pump = new PathGraphBuildPump();
        pump.Begin(50);
        pump.Fail();
        Assert.Equal(PathGraphBuildPump.State.Failed, pump.Current);

        pump.Reset();
        Assert.Equal(PathGraphBuildPump.State.Idle, pump.Current);
        Assert.Equal(0f, pump.Progress01);
    }

    [Theory]
    [InlineData(0f, "Track graph… 0%")]
    [InlineData(0.35f, "Track graph… 35%")]
    [InlineData(1f, "Track graph… 100%")]
    public void FormatBanner_shows_percent(float progress, string expected)
    {
        Assert.Equal(expected, PathGraphBuildPump.FormatBanner(progress));
    }

    [Fact]
    public void Simulated_frame_budget_never_processes_more_than_max_per_tick()
    {
        const int total = 250;
        var pump = new PathGraphBuildPump();
        pump.Begin(total);

        var ticks = 0;
        while (pump.RemainingUnits > 0)
        {
            var chunk = pump.BudgetThisTick(PathGraphBuildPump.MaxUnitsPerTick);
            Assert.True(chunk <= PathGraphBuildPump.MaxUnitsPerTick);
            Assert.True(chunk > 0);
            pump.AddCompleted(chunk);
            ticks++;
        }

        Assert.True(ticks >= 4);
        pump.Complete();
        Assert.Equal(PathGraphBuildPump.State.Ready, pump.Current);
    }
}

public class PathGraphTests
{
    [Fact]
    public void AddEdge_creates_nodes_and_directed_hops()
    {
        var g = new PathGraph();
        g.AddEdge(1, 2, 1f);
        g.AddEdge(2, 3, 1f);

        Assert.Equal(3, g.NodeCount);
        Assert.Equal(2, g.EdgeCount);
        Assert.Equal(1, g.FirstId);
        Assert.Equal(3, g.LastId);
    }

    [Fact]
    public void Freeze_rejects_further_edges()
    {
        var g = new PathGraph();
        g.AddEdge(1, 2, 1f);
        g.Freeze();
        g.AddEdge(2, 3, 1f);

        Assert.Equal(2, g.NodeCount);
        Assert.Equal(1, g.EdgeCount);
        Assert.True(g.IsFrozen);
    }

    [Fact]
    public void AStar_same_node_is_zero_hops()
    {
        var g = new PathGraph();
        g.EnsureNode(7);
        var r = PathGraphSearch.Find(g, 7, 7);
        Assert.True(r.Found);
        Assert.Equal(0, r.Hops);
        Assert.Equal(0f, r.Cost);
    }

    [Fact]
    public void AStar_finds_cheaper_branch()
    {
        var g = new PathGraph();
        g.AddEdge(1, 2, 1f);
        g.AddEdge(2, 3, 1f);
        g.AddEdge(1, 3, 10f);
        g.Freeze();

        var r = PathGraphSearch.Find(g, 1, 3);
        Assert.True(r.Found);
        Assert.Equal(2, r.Hops);
        Assert.Equal(2f, r.Cost);
    }

    [Fact]
    public void AStar_disconnected_is_not_found()
    {
        var g = new PathGraph();
        g.EnsureNode(1);
        g.EnsureNode(9);
        g.Freeze();

        var r = PathGraphSearch.Find(g, 1, 9);
        Assert.False(r.Found);
    }

    [Fact]
    public void FormatStart_fail_when_empty()
    {
        Assert.Equal("T2 graph fail", PathGraphTelemetry.FormatStart(0));
        Assert.Equal("T2 graph start: units=400", PathGraphTelemetry.FormatStart(400));
        Assert.Equal("T2 graph fail", PathGraphTelemetry.FormatFail());
    }

    [Fact]
    public void FormatReady_uses_dash_when_no_path()
    {
        var ready = new PathGraphReady(1, 12, 20, pathFound: false, pathHops: 0, pathCost: 0f);
        Assert.Equal("T2 graph ready: nodes=12 edges=20 hops=—", PathGraphTelemetry.FormatReady(ready));

        ready = new PathGraphReady(1, 12, 20, pathFound: true, pathHops: 5, pathCost: 5f);
        Assert.Equal("T2 graph ready: nodes=12 edges=20 hops=5", PathGraphTelemetry.FormatReady(ready));
    }
}

[Collection("YmsEventBus")]
public class PathGraphBusTests : IDisposable
{
    public PathGraphBusTests()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    public void Dispose()
    {
        YmsEventBus.ClearAllSubscriptions();
    }

    [Fact]
    public void DrainPathGraph_raises_Type_A_after_worker_would_enqueue()
    {
        PathGraphReady received = default;
        var calls = 0;
        void Handler(PathGraphReady item)
        {
            received = item;
            calls++;
        }

        YmsEventBus.OnPathGraphReady += Handler;
        var payload = new PathGraphReady(3, 10, 14, true, 4, 4f);
        YmsEventBus.PathGraph.Enqueue(payload);

        Assert.Equal(0, calls);
        Assert.Equal(1, YmsEventBus.DrainPathGraph(YmsMailbox<PathGraphReady>.MaxDrainPerFrame));
        Assert.Equal(1, calls);
        Assert.Equal(10, received.NodeCount);
        Assert.Equal(4, received.PathHops);
    }

    [Fact]
    public void ClearAllSubscriptions_drops_pending_path_graph_items()
    {
        var calls = 0;
        YmsEventBus.OnPathGraphReady += _ => calls++;
        YmsEventBus.PathGraph.Enqueue(new PathGraphReady(1, 1, 1, false, 0, 0f));
        YmsEventBus.ClearAllSubscriptions();

        Assert.Equal(0, YmsEventBus.DrainPathGraph(8));
        Assert.Equal(0, calls);
    }
}
