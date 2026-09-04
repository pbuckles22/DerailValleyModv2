using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// **13.2.2** / <b>13.4</b> — Prep dest rem-to-aim ≤ d_stop → at-track.
/// Ambiguous track never auto-advances to at-spur. Does not Next the list.
/// </summary>
[Collection("StaticSessions")]
public class HtpPrepTrackArrivalTests
{
    public HtpPrepTrackArrivalTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_13_2_2_prep_dest_track_span_is_at_track()
    {
        // Aim = 80 − 8 = 72; crawl in the aim band.
        Assert.Equal(72f, PrepTrackArrivalGate.AimAlongMeters(80f));
        Assert.Equal(
            PrepTrackArrival.AtTrack,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                destTrackId: "SW-C1O",
                locoTrackId: "SW-C1O",
                spanMeters: 72f,
                trackLengthMeters: 80f,
                uniqueTrack: true,
                speedKmh: 0f));
        Assert.True(PrepTrackArrivalGate.ShouldAdvanceToAtSpur(PrepTrackArrival.AtTrack));
        Assert.Equal("T2 prep: at track", SwitchListRunnerTelemetry.PrepAtTrack);
        Assert.Equal("at track SW-C1O", PrepTrackArrivalGate.FormatDeskCue("SW-C1O"));
    }

    /// <summary>
    /// Same overshoot class as TT: entry at cruise must arm early; crawl far from aim waits.
    /// </summary>
    [Fact]
    public void Smoke_13_4_17_prep_hot_entry_arms_crawl_waits()
    {
        Assert.Equal(
            PrepTrackArrival.OffTrack,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                "SW-C1O",
                "SW-C1O",
                spanMeters: 12f,
                trackLengthMeters: 80f,
                uniqueTrack: true,
                speedKmh: 5f));
        Assert.Equal(
            PrepTrackArrival.AtTrack,
            PrepTrackArrivalGate.Evaluate(
                SwitchListStepKind.Prep,
                "SW-C1O",
                "SW-C1O",
                spanMeters: 42f,
                trackLengthMeters: 80f,
                uniqueTrack: true,
                speedKmh: 40f));
        Assert.Equal(
            "T2 prep: at track along=42 len=80 spd=40",
            PrepTrackArrivalGate.FormatLatchLog(42f, 80f, 40f));
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
            spanMeters: 72f,
            trackLengthMeters: 80f,
            uniqueTrack: true));
        Assert.True(PrepTrackArrivalSession.AtSpur);
        Assert.Equal(0, SwitchListSession.CurrentIndex);
        Assert.Equal(SwitchListStepKind.Prep, SwitchListSession.CurrentStep!.Kind);
        Assert.False(SwitchListSession.TryArrivePrepTrack(
            destTrackId: "SW-C1O",
            locoTrackId: "SW-C1O",
            spanMeters: 72f,
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
            spanMeters: 32f,
            trackLengthMeters: 40f,
            uniqueTrack: true));
        Assert.True(PrepTrackArrivalSession.AtSpur);

        YmsRouteSessions.ClearAll();
        Assert.False(PrepTrackArrivalSession.AtSpur);
        Assert.False(SwitchListSession.HasActive);
    }
}
