using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 13.6.1 remote take — Preview countdown is not the path to taken.
/// Desk take when API allows. Yard GO does not take; haul Transit GO after Prep does.
/// </summary>
public class RemoteTakeGateTests
{
    [Fact]
    public void Smoke_13_6_1_preview_countdown_is_not_the_only_path_to_taken()
    {
        var outOfYard = PreviewArmed(previewMeters: -1f, deskTake: true, goArm: false);
        Assert.Equal(RemoteTakeDecision.Request, RemoteTakeGate.Evaluate(in outOfYard));

        var stillInYard = PreviewArmed(previewMeters: 900f, deskTake: true, goArm: false);
        Assert.Equal(RemoteTakeDecision.Request, RemoteTakeGate.Evaluate(in stillInYard));

        var countdownOnly = PreviewArmed(previewMeters: -1f, deskTake: false, goArm: false);
        Assert.Equal(RemoteTakeDecision.NoOp, RemoteTakeGate.Evaluate(in countdownOnly));
    }

    [Fact]
    public void Smoke_13_6_1_preview_plus_desk_requests_take_when_api_allows()
    {
        var desk = PreviewArmed(previewMeters: 400f, deskTake: true, goArm: false);
        Assert.Equal(RemoteTakeDecision.Request, RemoteTakeGate.Evaluate(in desk));
    }

    [Fact]
    public void Smoke_13_4_yard_go_arm_does_not_take()
    {
        var go = PreviewArmed(previewMeters: 400f, deskTake: false, goArm: true);
        Assert.Equal(RemoteTakeDecision.NoOp, RemoteTakeGate.Evaluate(in go));
    }

    [Fact]
    public void Smoke_13_4_haul_transit_go_after_prep_requests_take()
    {
        var go = new RemoteTakeInput(
            previewHeld: true,
            alreadyTaken: false,
            switchListLoaded: true,
            listJobMatchesHeld: true,
            goArm: true,
            deskTake: false,
            apiAllowsTake: true,
            previewMetersRemaining: 400f,
            haulTransitTakeArm: true);
        Assert.Equal(RemoteTakeDecision.Request, RemoteTakeGate.Evaluate(in go));
    }

    [Fact]
    public void Smoke_13_4_take_arm_only_after_last_prep()
    {
        var steps = new[]
        {
            new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past switch"),
            new SwitchListStep(2, SwitchListStepKind.Transit, "SW", "#Y-TT", "to TT"),
            new SwitchListStep(3, SwitchListStepKind.TurnAround, "SW", "TT", "TT turn around"),
            new SwitchListStep(4, SwitchListStepKind.Transit, "SW", "#Y-leave", "Past switch"),
            new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep"),
            new SwitchListStep(6, SwitchListStepKind.Transit, "GF", "GF-D5I", "Transit"),
            new SwitchListStep(7, SwitchListStepKind.Delivery, "GF", "GF-D5I", "Delivery"),
        };
        Assert.False(SwitchListTakeArm.IsHaulTransitTake(steps, 0, steps[0]));
        Assert.False(SwitchListTakeArm.IsHaulTransitTake(steps, 3, steps[3]));
        Assert.False(SwitchListTakeArm.IsHaulTransitTake(steps, 4, steps[4]));
        Assert.True(SwitchListTakeArm.IsHaulTransitTake(steps, 5, steps[5]));
    }

    [Fact]
    public void Smoke_13_6_1_refuse_when_office_required()
    {
        var input = PreviewArmed(previewMeters: 400f, deskTake: true, goArm: false, apiAllowsTake: false);
        Assert.Equal(RemoteTakeDecision.RefuseOfficeRequired, RemoteTakeGate.Evaluate(in input));
    }

    [Fact]
    public void Smoke_13_6_1_refuse_job_not_on_loaded_switch_list()
    {
        var noList = PreviewArmed(
            previewMeters: 400f,
            deskTake: true,
            goArm: false,
            switchListLoaded: false,
            listJobMatchesHeld: false);
        Assert.Equal(RemoteTakeDecision.RefuseNotOnList, RemoteTakeGate.Evaluate(in noList));

        var mismatch = PreviewArmed(
            previewMeters: 400f,
            deskTake: true,
            goArm: false,
            switchListLoaded: true,
            listJobMatchesHeld: false);
        Assert.Equal(RemoteTakeDecision.RefuseNotOnList, RemoteTakeGate.Evaluate(in mismatch));
    }

    [Fact]
    public void Smoke_13_6_1_noop_when_already_taken()
    {
        var input = new RemoteTakeInput(
            previewHeld: true,
            alreadyTaken: true,
            switchListLoaded: true,
            listJobMatchesHeld: true,
            goArm: true,
            deskTake: true,
            apiAllowsTake: true,
            previewMetersRemaining: 400f);
        Assert.Equal(RemoteTakeDecision.NoOp, RemoteTakeGate.Evaluate(in input));
    }

    [Fact]
    public void CanOfferDeskTake_when_preview_list_match_and_api_allows()
    {
        Assert.True(RemoteTakeGate.CanOfferDeskTake(
            previewHeld: true,
            alreadyTaken: false,
            switchListLoaded: true,
            listJobMatchesHeld: true,
            apiAllowsTake: true));
        Assert.False(RemoteTakeGate.CanOfferDeskTake(
            previewHeld: true,
            alreadyTaken: false,
            switchListLoaded: true,
            listJobMatchesHeld: true,
            apiAllowsTake: false));
    }

    [Fact]
    public void Format_taken_line_names_the_job()
    {
        Assert.Equal(
            "T2 job-take: taken=1 job=SW-FH-82",
            RemoteTakeTelemetry.FormatTaken("SW-FH-82"));
        Assert.Equal(
            "T2 job-take: request job=SW-FH-82 src=go",
            RemoteTakeTelemetry.FormatRequest("SW-FH-82", RemoteTakeSource.Go));
        Assert.Equal(RemoteTakeTelemetry.RefuseOfficeRequired, RemoteTakeTelemetry.FormatRefuse(RemoteTakeDecision.RefuseOfficeRequired));
        Assert.Equal(RemoteTakeTelemetry.RefuseNotOnList, RemoteTakeTelemetry.FormatRefuse(RemoteTakeDecision.RefuseNotOnList));
    }

    private static RemoteTakeInput PreviewArmed(
        float previewMeters,
        bool deskTake,
        bool goArm,
        bool switchListLoaded = true,
        bool listJobMatchesHeld = true,
        bool apiAllowsTake = true) =>
        new(
            previewHeld: true,
            alreadyTaken: false,
            switchListLoaded,
            listJobMatchesHeld,
            goArm,
            deskTake,
            apiAllowsTake,
            previewMetersRemaining: previewMeters);
}
