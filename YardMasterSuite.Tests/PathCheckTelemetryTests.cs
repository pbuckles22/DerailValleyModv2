using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// T2 path logs on dest set / status change / clear — not every LateUpdate (**6.11**).
/// </summary>
public class PathCheckTelemetryTests
{
    [Fact]
    public void Smoke_end_dest_same_track_emits_T2_path_init_ok()
    {
        var cache = default(PathCheckCache);
        Assert.True(PathCheckTelemetry.Observe(
            hasDest: true,
            PathCheckStatus.Aligned,
            misaligned: 0,
            ref cache));
        Assert.Equal(
            "T2 path init: Path OK",
            PathCheckTelemetry.NextLog(PathCheckLogKind.Init, PathCheckStatus.Aligned, 0));
    }

    [Fact]
    public void Smoke_shift_end_emits_T2_path_cleared()
    {
        var cache = default(PathCheckCache);
        PathCheckTelemetry.Observe(true, PathCheckStatus.Aligned, 0, ref cache);
        Assert.True(PathCheckTelemetry.Observe(false, PathCheckStatus.NoDestination, 0, ref cache));
        Assert.Equal(
            "T2 path cleared",
            PathCheckTelemetry.NextLog(PathCheckLogKind.Cleared, PathCheckStatus.NoDestination, 0));
    }

    [Fact]
    public void Status_change_emits_T2_path_change()
    {
        var cache = default(PathCheckCache);
        PathCheckTelemetry.Observe(true, PathCheckStatus.Aligned, 0, ref cache);
        Assert.True(PathCheckTelemetry.Observe(true, PathCheckStatus.Misaligned, 1, ref cache));
        Assert.Equal(
            "T2 path change: Path 1 switch",
            PathCheckTelemetry.NextLog(PathCheckLogKind.Change, PathCheckStatus.Misaligned, 1));
    }

    [Fact]
    public void Same_status_is_silent()
    {
        var cache = default(PathCheckCache);
        PathCheckTelemetry.Observe(true, PathCheckStatus.Aligned, 0, ref cache);
        Assert.False(PathCheckTelemetry.Observe(true, PathCheckStatus.Aligned, 0, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_buckets_hold()
    {
        var cache = default(PathCheckCache);
        PathCheckTelemetry.Observe(true, PathCheckStatus.Aligned, 0, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            PathCheckTelemetry.Observe(true, PathCheckStatus.Aligned, 0, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
