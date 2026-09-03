using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// **13.2.2** — loco on Prep dest track (unique along-span) → at-track.
/// Ambiguous track never auto-advances to at-spur. Does not Next the list.
/// </summary>
[Collection("StaticSessions")]
public class HtpPrepTrackArrivalTests
{
    public HtpPrepTrackArrivalTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_2_2_prep_dest_track_span_is_at_track()
    {
        Assert.Equal(
            PrepTrackArrival.AtTrack,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-C1O",
                spanMeters: 12f,
                trackLengthMeters: 80f,
                uniqueTrack: true));
        Assert.True(PrepTrackArrivalGate.ShouldAdvanceToAtSpur(PrepTrackArrival.AtTrack));
        Assert.Equal("T2 prep: at track", SwitchListRunnerTelemetry.PrepAtTrack);
        Assert.Equal("at track SW-C1O", PrepTrackArrivalGate.FormatDeskCue("SW-C1O"));
    }

    [Fact]
    public void Smoke_13_2_2_ambiguous_track_does_not_advance_to_at_spur()
    {
        Assert.Equal(
            PrepTrackArrival.Ambiguous,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-C1O",
                spanMeters: 12f,
                trackLengthMeters: 80f,
                uniqueTrack: false));
        Assert.Equal(
            PrepTrackArrival.Ambiguous,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: null,
                spanMeters: 12f,
                trackLengthMeters: 80f,
                uniqueTrack: true));
        Assert.Equal(
            PrepTrackArrival.Ambiguous,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-C1O",
                spanMeters: float.NaN,
                trackLengthMeters: 80f,
                uniqueTrack: true));
        Assert.Equal(
            PrepTrackArrival.Ambiguous,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-C1O",
                spanMeters: 12f,
                trackLengthMeters: 0f,
                uniqueTrack: true));
        Assert.False(PrepTrackArrivalGate.ShouldAdvanceToAtSpur(PrepTrackArrival.Ambiguous));
        Assert.False(PrepTrackArrivalSession.TryArrive(PrepTrackArrival.Ambiguous));
        Assert.False(PrepTrackArrivalSession.AtSpur);
    }

    [Fact]
    public void Smoke_13_2_2_off_track_and_non_prep_stay_off()
    {
        Assert.Equal(
            PrepTrackArrival.OffTrack,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-B4L",
                spanMeters: 12f,
                trackLengthMeters: 80f,
                uniqueTrack: true));
        Assert.Equal(
            PrepTrackArrival.OffTrack,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Transit,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-C1O",
                spanMeters: 12f,
                trackLengthMeters: 80f,
                uniqueTrack: true));
        Assert.False(PrepTrackArrivalGate.ShouldAdvanceToAtSpur(PrepTrackArrival.OffTrack));
    }

    [Fact]
    public void Smoke_13_2_2_at_track_does_not_next_the_switch_list()
    {
        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
                new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit → GF-D5I"),
            });

        Assert.True(SwitchListSession.TryArrivePrepTrack(
            destTrackId: "SW-C1O",
            locoTrackId: "SW-C1O",
            spanMeters: 12f,
            trackLengthMeters: 80f,
            uniqueTrack: true));
        Assert.True(PrepTrackArrivalSession.AtSpur);
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.False(SwitchListSession.TryArrivePrepTrack(
            destTrackId: "SW-C1O",
            locoTrackId: "SW-C1O",
            spanMeters: 12f,
            trackLengthMeters: 80f,
            uniqueTrack: true));
    }

    [Fact]
    public void Smoke_13_2_2_world_leave_clears_at_spur()
    {
        SwitchListSession.Bind(
            "SW-FH-82",
            new[]
            {
                new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O"),
            });
        Assert.True(SwitchListSession.TryArrivePrepTrack(
            destTrackId: "SW-C1O",
            locoTrackId: "SW-C1O",
            spanMeters: 4f,
            trackLengthMeters: 40f,
            uniqueTrack: true));
        Assert.True(PrepTrackArrivalSession.AtSpur);

        YmsRouteSessions.ClearAll();
        Assert.False(PrepTrackArrivalSession.AtSpur);
        Assert.False(SwitchListSession.HasActive);
    }
}
