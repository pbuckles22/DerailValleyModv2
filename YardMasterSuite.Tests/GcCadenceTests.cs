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
