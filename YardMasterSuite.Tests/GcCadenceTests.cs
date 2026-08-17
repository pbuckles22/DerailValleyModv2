using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: 3.1 hitch volume / alert fatigue — 100 ms gate, world session only.
/// </summary>
public class GcCadenceTests
{
    [Fact]
    public void Observe_first_sample_is_silent()
    {
        var state = GcCadenceState.Initial();
        var msg = GcCadence.Observe(now: 1f, gc0: 3, ref state, worldSessionActive: true);

        Assert.Null(msg);
        Assert.Equal(1f, state.LastFrameAt);
        Assert.Equal(3, state.LastGc0);
    }

    [Fact]
    public void Yard_play_under_100ms_is_silent()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 0 };
        var msg = GcCadence.Observe(now: 1.05f, gc0: 0, ref state, worldSessionActive: true);

        Assert.Null(msg);
        Assert.Equal(1.05f, state.LastFrameAt);
    }

    [Fact]
    public void In_world_hitch_over_100ms_logs_T2()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 0 };
        var msg = GcCadence.Observe(now: 1.12f, gc0: 0, ref state, worldSessionActive: true);

        Assert.Equal("T2 hitch-spike: dt=120ms", msg);
        Assert.Equal(1.12f, state.LastLogAt);
    }

    [Fact]
    public void Load_menu_hitch_is_silent_without_world_session()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 2 };
        var msg = GcCadence.Observe(now: 14f, gc0: 5, ref state, worldSessionActive: false);

        Assert.False(HudWorldSession.IsActive(playerTransformPresent: false));
        Assert.Null(msg);
        Assert.Equal(14f, state.LastFrameAt);
        Assert.Equal(5, state.LastGc0);
        Assert.Equal(-999f, state.LastLogAt);
    }

    [Fact]
    public void Load_dt_does_not_carry_into_first_world_frame()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 0 };
        GcCadence.Observe(now: 14f, gc0: 0, ref state, worldSessionActive: false);

        var msg = GcCadence.Observe(now: 14.016f, gc0: 0, ref state, worldSessionActive: true);

        Assert.True(HudWorldSession.IsActive(playerTransformPresent: true));
        Assert.Null(msg);
    }

    [Fact]
    public void Observe_throttles_hitch_log_spam()
    {
        var state = new GcCadenceState { LastFrameAt = 1.9f, LastLogAt = 1.5f, LastGc0 = 0 };
        var msg = GcCadence.Observe(now: 2.12f, gc0: 0, ref state, worldSessionActive: true);

        Assert.Null(msg);
        Assert.Equal(1.5f, state.LastLogAt);

        state.LastFrameAt = 2.9f;
        msg = GcCadence.Observe(now: 3.12f, gc0: 0, ref state, worldSessionActive: true);

        Assert.Equal("T2 hitch-spike: dt=220ms", msg);
        Assert.Equal(3.12f, state.LastLogAt);
    }

    [Fact]
    public void Observe_annotates_gc0_when_collection_ran_on_a_spike()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 4 };
        var msg = GcCadence.Observe(now: 1.12f, gc0: 5, ref state, worldSessionActive: true);

        Assert.Equal("T2 hitch-spike: dt=120ms gc0=+1", msg);
        Assert.Equal(5, state.LastGc0);
    }

    [Fact]
    public void Observe_silent_when_gc0_increases_without_a_frame_spike()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 4 };
        var msg = GcCadence.Observe(now: 1.016f, gc0: 5, ref state, worldSessionActive: true);

        Assert.Null(msg);
        Assert.Equal(5, state.LastGc0);
    }
}

/// <summary>
/// Smoke harvest: 3.1 Player.log hitch classes (PERFORMANCE_LOG).
/// </summary>
public class HitchBandTests
{
    [Fact]
    public void Yard_play_50ms_is_below_gate()
    {
        Assert.Equal(HitchBand.BelowGate, GcCadence.Classify(0.050f));
    }

    [Fact]
    public void Cab_look_120ms_is_feature_hitch()
    {
        Assert.Equal(HitchBand.Feature, GcCadence.Classify(0.120f));
    }

    [Fact]
    public void Autosave_141ms_is_feature_hitch()
    {
        Assert.Equal(HitchBand.Feature, GcCadence.Classify(0.141f));
    }

    [Fact]
    public void Streaming_1003ms_is_load_scale()
    {
        Assert.Equal(HitchBand.LoadScale, GcCadence.Classify(1.003f));
    }

    [Fact]
    public void Player_create_13s_is_load_scale()
    {
        Assert.Equal(HitchBand.LoadScale, GcCadence.Classify(13.096f));
    }
}

/// <summary>
/// Smoke harvest: 40–99 ms is invisible at the 100 ms spike gate.
/// Count it in a windowed T2 hitch-summary (not per-frame).
/// </summary>
public class HitchSummaryTests
{
    [Fact]
    public void Yard_play_16ms_counts_fine_not_below()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.016f, gcDelta: 0, now: 1f, ref hist);

        Assert.Equal(1, hist.Frames);
        Assert.Equal(1, hist.Fine);
        Assert.Equal(0, hist.Below);
        Assert.Equal(0, hist.Feature);
        Assert.Equal(0, hist.Load);
        Assert.Equal(0, hist.MaxBelowMs);
        Assert.Equal(0, hist.BelowGc0);
    }

    [Fact]
    public void Yard_play_50ms_counts_below_gate_band()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.050f, gcDelta: 0, now: 1f, ref hist);

        Assert.Equal(1, hist.Frames);
        Assert.Equal(0, hist.Fine);
        Assert.Equal(1, hist.Below);
        Assert.Equal(50, hist.MaxBelowMs);
        Assert.Equal(0, hist.BelowGc0);
    }

    [Fact]
    public void Observe_silent_when_gc0_increases_without_a_frame_spike_still_counts_below_gc0()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.066f, gcDelta: 1, now: 1f, ref hist);

        Assert.Equal(1, hist.Below);
        Assert.Equal(1, hist.BelowGc0);
        Assert.Equal(66, hist.MaxBelowMs);
    }

    [Fact]
    public void Cab_look_120ms_counts_feature_not_below()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.120f, gcDelta: 0, now: 1f, ref hist);

        Assert.Equal(1, hist.Feature);
        Assert.Equal(0, hist.Below);
        Assert.Equal(0, hist.MaxBelowMs);
    }

    [Fact]
    public void Hitch_summary_silent_before_30s_window()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.050f, gcDelta: 0, now: 10f, ref hist);

        Assert.Null(GcCadence.MaybeSummary(now: 39.9f, force: false, ref hist));
        Assert.Equal(1, hist.Frames);
    }

    [Fact]
    public void Hitch_summary_emits_after_30s_and_resets()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.016f, gcDelta: 0, now: 10f, ref hist);
        GcCadence.Record(dtSeconds: 0.050f, gcDelta: 1, now: 10.05f, ref hist);
        GcCadence.Record(dtSeconds: 0.120f, gcDelta: 0, now: 10.17f, ref hist);
        GcCadence.Record(dtSeconds: 1.2f, gcDelta: 0, now: 11.37f, ref hist);

        var line = GcCadence.MaybeSummary(now: 40f, force: false, ref hist);

        Assert.Equal("T2 hitch-summary: n=4 fine=1 below=1 max=50ms gc0=1 feature=1 load=1", line);
        Assert.Equal(0, hist.Frames);
        Assert.Equal(0, hist.Below);
    }

    [Fact]
    public void Hitch_summary_flush_on_leave_world_before_30s()
    {
        var hist = default(GcHitchHistogram);
        GcCadence.Record(dtSeconds: 0.050f, gcDelta: 0, now: 10f, ref hist);

        var line = GcCadence.MaybeSummary(now: 12f, force: true, ref hist);

        Assert.Equal("T2 hitch-summary: n=1 fine=0 below=1 max=50ms gc0=0 feature=0 load=0", line);
        Assert.Equal(0, hist.Frames);
    }

    [Fact]
    public void Hitch_summary_silent_when_no_frames()
    {
        var hist = default(GcHitchHistogram);
        Assert.Null(GcCadence.MaybeSummary(now: 40f, force: true, ref hist));
    }
}
