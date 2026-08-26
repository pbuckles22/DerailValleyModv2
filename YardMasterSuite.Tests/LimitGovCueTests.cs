using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LimitGovCueTests
{
    [Fact]
    public void GovernorFlash_lit_half_of_period()
    {
        Assert.True(GovernorFlash.Lit(0f));
        Assert.True(GovernorFlash.Lit(0.19f));
        Assert.False(GovernorFlash.Lit(0.20f));
        Assert.False(GovernorFlash.Lit(0.39f));
        Assert.True(GovernorFlash.Lit(0.40f));
        Assert.False(GovernorFlash.Lit(-1f));
        Assert.False(GovernorFlash.Lit(float.NaN));
    }

    [Fact]
    public void Observe_emits_on_change_only()
    {
        var cache = default(LimitGovCueCache);
        var moving = new LimitGovCue(true, true, false);
        Assert.True(LimitGovCueTelemetry.Observe(moving, ref cache));
        Assert.False(LimitGovCueTelemetry.Observe(moving, ref cache));
        Assert.True(LimitGovCueTelemetry.Observe(LimitGovCue.None, ref cache));
        Assert.False(LimitGovCueTelemetry.Observe(LimitGovCue.None, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_cue_holds()
    {
        var cache = default(LimitGovCueCache);
        var cue = new LimitGovCue(true, false, true);
        LimitGovCueTelemetry.Observe(cue, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            LimitGovCueTelemetry.Observe(cue, ref cache);
            GovernorFlash.Lit(i * 0.016f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
