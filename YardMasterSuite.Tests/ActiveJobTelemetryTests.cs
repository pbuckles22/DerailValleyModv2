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

    [Fact]
    public void Smoke_hold_overview_emits_T2_job_appear_preview()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            false, null, 0, JobConsistStatus.Missing, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.ObservePrep(180f, licenseCodes: null, ref cache));
        Assert.Equal(
            "T2 job appear: preview=180 license=— yard=—",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_preview_out_when_past_regular_edge()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.ObservePrep(180f, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.ObservePrep(-5f, null, ref cache));
        Assert.Equal(
            "T2 job change: preview=OUT license=— yard=—",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_no_license_fh_with_preview_emits_T2()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(false, null, 0, JobConsistStatus.Missing, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.ObservePrep(180f, "FH", ref cache));
        Assert.Equal(
            "T2 job appear: preview=180 license=FH yard=—",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_sw_su_at_sw_office_emits_preview_900_yard_sw()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(false, null, 0, JobConsistStatus.Missing, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.ObservePrep(900f, null, "SW", ref cache));
        Assert.Equal(
            "T2 job appear: preview=900 license=— yard=SW",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_abandoned_taken_job_emits_T2_cancelled()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Ready, 600f, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.ObserveCancelled("SM-FH-12", ref cache));
        Assert.Equal(
            "T2 job change: job=SM-FH-12 extra=0 status=Cancelled bonus=—",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_taken_job_hides_preview()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.ObservePrep(180f, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.Observe(
            true, "SM-FH-12", 0, JobConsistStatus.Missing, 600f, ref cache));
        Assert.Equal(
            "T2 job change: job=SM-FH-12 extra=0 status=RED bonus=10",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Smoke_empty_hands_hides_prep_bar()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.ObservePrep(180f, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.True(ActiveJobTelemetry.Observe(
            false, null, 0, JobConsistStatus.Missing, null, ref cache));
        Assert.Equal(
            "T2 job hide",
            ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }

    [Fact]
    public void Preview_meter_tick_inside_bucket_is_silent_on_T2()
    {
        var cache = default(ActiveJobCache);
        ActiveJobTelemetry.ObservePrep(184f, null, ref cache);
        var prior = ActiveJobTelemetry.Snapshot(ref cache);

        Assert.False(ActiveJobTelemetry.ObservePrep(181f, null, ref cache));
        Assert.Null(ActiveJobTelemetry.NextLog(prior, ActiveJobTelemetry.Snapshot(ref cache)));
    }
}
