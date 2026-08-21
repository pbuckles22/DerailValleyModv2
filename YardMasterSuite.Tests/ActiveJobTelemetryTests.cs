using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// T2 job-bar logs on appear / hide / id / GO-HOLD / bonus-minute — not every second (**6.13**).
/// </summary>
public class ActiveJobTelemetryTests
{
    [Fact]
    public void Smoke_no_taken_job_emits_T2_job_init_hidden()
    {
        var cache = default(ActiveJobCache);
        Assert.True(ActiveJobTelemetry.Observe(
            visible: false,
            jobId: null,
            extraCount: 0,
            JobConsistStatus.Missing,
            remainingSeconds: null,
            ref cache));
        Assert.Equal(
            "T2 job init (hidden)",
            ActiveJobTelemetry.NextLog(null, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_taken_job_emits_T2_job_init_go()
    {
        var cache = default(ActiveJobCache);
        Assert.True(ActiveJobTelemetry.Observe(
            visible: true,
            jobId: "SM-FH-12",
            extraCount: 0,
            JobConsistStatus.Ready,
            remainingSeconds: 14 * 60f + 32f,
            ref cache));
        Assert.Equal(
            "T2 job init: job=SM-FH-12 extra=0 status=GO bonus=14",
            ActiveJobTelemetry.NextLog(null, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_take_job_emits_T2_job_appear()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            false, null, 0, JobConsistStatus.Missing, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Missing, 600f, ref cache));
        Assert.Equal(
            "T2 job appear: job=SM-FH-12 extra=0 status=RED bonus=10",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_complete_job_emits_T2_job_hide()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Ready, 600f, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.Observe(
            false, null, 0, JobConsistStatus.Missing, null, ref cache));
        Assert.Equal(
            "T2 job hide",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_foreign_freight_changes_status_to_hold()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Ready, 600f, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Hold, 600f, ref cache));
        Assert.Equal(
            "T2 job change: job=SM-FH-12 extra=0 status=HOLD bonus=10",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Bonus_second_tick_is_silent_on_T2()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Ready, 14 * 60f + 32f, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.False(ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Ready, 14 * 60f + 10f, ref cache));
        Assert.Null(ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Hidden_again_is_silent()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(false, null, 0, JobConsistStatus.Missing, null, ref cache);
        Assert.False(ActiveJobTelemetry.Observe(false, null, 0, JobConsistStatus.Missing, null, ref cache));
    }

    [Fact]
    public void Observe_does_not_allocate_when_buckets_hold()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Ready, 600f, ref cache);
        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ActiveJobTelemetry.Observe(
                true, "SM-FH-12", 0, JobConsistStatus.Ready, 600f, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
