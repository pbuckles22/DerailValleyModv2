using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16.14): hop off an MU consist — LastLoco's trainset mate
/// must not rank as amber "other loco".
/// </summary>
public class LocoRadarExclusionTests
{
    [Fact]
    public void Smoke_on_foot_last_loco_excludes_mu_mate_from_radar()
    {
        var exclude = new HashSet<int>();
        LocoRadarExclusion.AddLastLocoTrainset(
            exclude,
            lastLocoId: 10,
            trainsetLocoIds: new[] { 10, 11 });

        Assert.Contains(10, exclude);
        Assert.Contains(11, exclude);

        var candidates = new[]
        {
            new LocoRadarCandidate(10, 1f),
            new LocoRadarCandidate(11, 64f),
            new LocoRadarCandidate(20, 10000f),
        };
        var dest = new int[3];
        var n = LocoRadarSelection.RankNearest(candidates, exclude, 3, dest);

        Assert.Equal(1, n);
        Assert.Equal(20, dest[0]);
    }
}
