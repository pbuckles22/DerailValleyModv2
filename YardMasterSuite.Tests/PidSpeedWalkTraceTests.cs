using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class PidSpeedWalkTraceTests
{
    [Fact]
    public void Observe_is_change_only()
    {
        var cache = default(PidSpeedWalkTraceCache);
        Assert.True(
            PidSpeedWalkTrace.Observe(1, 9, 0, 9, 0, PidSpeedMode.Hold, ref cache));
        Assert.False(
            PidSpeedWalkTrace.Observe(1, 9, 0, 9, 0, PidSpeedMode.Hold, ref cache));
        Assert.True(
            PidSpeedWalkTrace.Observe(1, 18, 0, 9, 0, PidSpeedMode.Hold, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_on_hot_path()
    {
        var cache = default(PidSpeedWalkTraceCache);
        PidSpeedWalkTrace.Observe(20, 18, 0, 18, 0, PidSpeedMode.Hold, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            PidSpeedWalkTrace.Observe(20, 18, 0, 18, 0, PidSpeedMode.Hold, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
