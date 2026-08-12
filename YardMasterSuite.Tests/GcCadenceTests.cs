using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class GcCadenceTests
{
    [Fact]
    public void Observe_first_sample_is_silent()
    {
        var state = GcCadenceState.Initial();
        var msg = GcCadence.Observe(now: 1f, gc0: 3, ref state);

        Assert.Null(msg);
        Assert.Equal(1f, state.LastFrameAt);
        Assert.Equal(3, state.LastGc0);
    }

    [Fact]
    public void Observe_silent_under_hitch_threshold()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 0 };
        var msg = GcCadence.Observe(now: 1.02f, gc0: 0, ref state);

        Assert.Null(msg);
        Assert.Equal(1.02f, state.LastFrameAt);
    }

    [Fact]
    public void Observe_logs_T2_hitch_spike_when_frame_over_40ms()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 0 };
        var msg = GcCadence.Observe(now: 1.05f, gc0: 0, ref state);

        Assert.Equal("T2 hitch-spike: dt=50ms", msg);
        Assert.Equal(1.05f, state.LastLogAt);
    }

    [Fact]
    public void Observe_throttles_hitch_log_spam()
    {
        var state = new GcCadenceState { LastFrameAt = 1.9f, LastLogAt = 1.5f, LastGc0 = 0 };
        var msg = GcCadence.Observe(now: 2f, gc0: 0, ref state);

        Assert.Null(msg);
        Assert.Equal(1.5f, state.LastLogAt);

        state.LastFrameAt = 2.9f;
        msg = GcCadence.Observe(now: 3f, gc0: 0, ref state);

        Assert.Equal("T2 hitch-spike: dt=100ms", msg);
        Assert.Equal(3f, state.LastLogAt);
    }

    [Fact]
    public void Observe_annotates_gc0_when_collection_ran_on_a_spike()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 4 };
        var msg = GcCadence.Observe(now: 1.05f, gc0: 5, ref state);

        Assert.Equal("T2 hitch-spike: dt=50ms gc0=+1", msg);
        Assert.Equal(5, state.LastGc0);
    }

    [Fact]
    public void Observe_silent_when_gc0_increases_without_a_frame_spike()
    {
        var state = new GcCadenceState { LastFrameAt = 1f, LastLogAt = -999f, LastGc0 = 4 };
        var msg = GcCadence.Observe(now: 1.016f, gc0: 5, ref state);

        Assert.Null(msg);
        Assert.Equal(5, state.LastGc0);
    }
}
