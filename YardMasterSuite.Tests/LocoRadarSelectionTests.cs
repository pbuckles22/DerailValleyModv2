using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16): nearest other locos ≤600 m, up to 3, exclude self/consist.
/// </summary>
public class LocoRadarSelectionTests
{
    [Fact]
    public void Smoke_empty_yard_ranks_zero_other_locos()
    {
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(
            Array.Empty<LocoRadarCandidate>(),
            excludeIds: null,
            maxResults: 3,
            rankedIds: dest);
        Assert.Equal(0, n);
    }

    [Fact]
    public void Smoke_other_loco_within_600m_ranks_nearest()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(10, distanceSq: 400f),
            new LocoRadarCandidate(20, distanceSq: 100f),
            new LocoRadarCandidate(30, distanceSq: 225f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, 3, dest);
        Assert.Equal(3, n);
        Assert.Equal(new[] { 20, 30, 10 }, dest);
    }

    [Fact]
    public void Smoke_caps_at_three_nearest()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 9f),
            new LocoRadarCandidate(2, 4f),
            new LocoRadarCandidate(3, 1f),
            new LocoRadarCandidate(4, 16f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, maxResults: 2, dest);
        Assert.Equal(2, n);
        Assert.Equal(3, dest[0]);
        Assert.Equal(2, dest[1]);
    }

    [Fact]
    public void Smoke_boarded_loco_is_not_radar_target()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 1f),
            new LocoRadarCandidate(2, 4f),
            new LocoRadarCandidate(3, 9f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(
            candidates,
            excludeIds: new HashSet<int> { 1 },
            maxResults: 3,
            rankedIds: dest);
        Assert.Equal(2, n);
        Assert.Equal(new[] { 2, 3 }, dest.Take(2).ToArray());
    }

    [Fact]
    public void RankNearest_ignores_non_positive_maxResults()
    {
        var candidates = new[] { new LocoRadarCandidate(1, 1f) };
        var dest = new int[1];
        Assert.Equal(0, LocoRadarSelection.RankNearest(candidates, null, 0, dest));
        Assert.Equal(0, LocoRadarSelection.RankNearest(candidates, null, -1, dest));
    }

    [Fact]
    public void RankNearest_does_not_overflow_destination()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 1f),
            new LocoRadarCandidate(2, 4f),
            new LocoRadarCandidate(3, 9f),
        };
        var dest = new int[1];
        var n = LocoRadarSelection.RankNearest(candidates, null, maxResults: 3, rankedIds: dest);
        Assert.Equal(1, n);
        Assert.Equal(1, dest[0]);
    }

    [Fact]
    public void RankNearest_respects_candidateCount()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(1, 1f),
            new LocoRadarCandidate(2, 4f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, 3, dest, candidateCount: 1);
        Assert.Equal(1, n);
        Assert.Equal(1, dest[0]);
    }

    [Fact]
    public void Smoke_max_range_is_600m_for_yard_walk()
    {
        Assert.Equal(600f, LocoRadarSelection.MaxRangeMeters);
        Assert.Equal(3, LocoRadarSelection.DefaultMaxResults);
    }

    [Fact]
    public void Smoke_loco_beyond_600m_is_dropped()
    {
        var justIn = LocoRadarSelection.MaxRangeMeters;
        var justOut = justIn + 1f;
        var candidates = new[]
        {
            new LocoRadarCandidate(1, justIn * justIn),
            new LocoRadarCandidate(2, justOut * justOut),
            new LocoRadarCandidate(3, 100f),
            new LocoRadarCandidate(4, 2500f * 2500f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, 3, dest);
        Assert.Equal(2, n);
        Assert.Equal(3, dest[0]);
        Assert.Equal(1, dest[1]);
    }

    [Fact]
    public void RankNearest_can_return_zero_or_one_inside_range()
    {
        var far = LocoRadarSelection.MaxRangeMeters + 50f;
        var candidates = new[]
        {
            new LocoRadarCandidate(1, far * far),
            new LocoRadarCandidate(2, 25f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, null, 3, dest);
        Assert.Equal(1, n);
        Assert.Equal(2, dest[0]);
    }

    [Fact]
    public void RankNearest_does_not_allocate_on_steady_scan()
    {
        var candidates = new[]
        {
            new LocoRadarCandidate(10, 400f),
            new LocoRadarCandidate(20, 100f),
            new LocoRadarCandidate(30, 225f),
        };
        var dest = new int[3];
        LocoRadarSelection.RankNearest(candidates, null, 3, dest);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            LocoRadarSelection.RankNearest(candidates, null, 3, dest);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
