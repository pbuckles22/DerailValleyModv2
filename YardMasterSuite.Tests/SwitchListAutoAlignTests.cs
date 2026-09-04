using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 13.4 foundational step rule: CLEARED ends approach (stop); Align is a
/// prerequisite of the drive step you enter / GO — not of the ending row.
/// Gate still 8.7.
/// </summary>
[Collection("StaticSessions")]
public class SwitchListAutoAlignTests
{
    public SwitchListAutoAlignTests()
    {
        PidGoFacingSession.Clear();
    }
    [Fact]
    public void Smoke_13_4_cleared_ends_step_align_is_next_step_prereq()
    {
        // CLEARED gate Ok is necessary for Align — but rise-to-CLEARED itself
        // must not be treated as "do Align now on the ending step." Policy
        // only answers: may the *current* drive step Align?
        var leave = new SwitchListStep(5, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Transit");
        Assert.True(SwitchListAutoAlign.StepWantsAlignPrep(leave));
        Assert.False(
            SwitchListAutoAlign.ShouldAutoAlign(
                leave,
                pinBlocksAlign: true,
                RouteClearancePhase.AtSwitch));
        Assert.True(
            SwitchListAutoAlign.ShouldAutoAlign(
                leave,
                pinBlocksAlign: true,
                RouteClearancePhase.Cleared));
    }

    [Fact]
    public void Smoke_13_4_sawtooth_auto_align_only_when_cleared()
    {
        var transit = new SwitchListStep(4, SwitchListStepKind.Transit, "SW", "#Y-#S969#T", "Past switch");
        Assert.True(SwitchListAutoAlign.StepWantsAlignPrep(transit));
        Assert.False(
            SwitchListAutoAlign.ShouldAutoAlign(
                transit,
                pinBlocksAlign: true,
                RouteClearancePhase.AtSwitch));
        Assert.True(
            SwitchListAutoAlign.ShouldAutoAlign(
                transit,
                pinBlocksAlign: true,
                RouteClearancePhase.Cleared));
    }

    [Fact]
    public void Smoke_13_4_delivery_does_not_want_align_prep()
    {
        var delivery = new SwitchListStep(7, SwitchListStepKind.Delivery, "SW", "SW-C1O", "Deliver");
        Assert.False(SwitchListAutoAlign.StepWantsAlignPrep(delivery));
        Assert.False(
            SwitchListAutoAlign.ShouldAutoAlign(
                delivery,
                pinBlocksAlign: false,
                RouteClearancePhase.Cleared));
    }

    [Fact]
    public void Smoke_13_4_go_facing_latches_until_cleared()
    {
        PidGoFacingSession.Clear();
        Assert.False(PidGoFacingSession.Active);
        Assert.True(PidGoFacingSession.Resolve(liveNeedsReverse: true));

        PidGoFacingSession.Latch(needsReverse: true);
        Assert.True(PidGoFacingSession.Active);
        Assert.True(PidGoFacingSession.Resolve(liveNeedsReverse: false));

        PidGoFacingSession.Clear();
        Assert.False(PidGoFacingSession.Active);
        Assert.False(PidGoFacingSession.Resolve(liveNeedsReverse: false));
    }

    [Fact]
    public void Smoke_13_4_step_prereq_facing_from_label_or_live()
    {
        var transit = new SwitchListStep(
            1,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            SwitchListDriveFacing.FormatDriveLabel(true, "Past switch", "SW-B4L"));
        Assert.True(SwitchListStepPrereq.WantsFacingPrep(transit));
        Assert.True(SwitchListStepPrereq.ResolveNeedsReverse(transit.Label, liveNeedsReverse: null));
        Assert.False(SwitchListStepPrereq.ResolveNeedsReverse(transit.Label, liveNeedsReverse: false));
        Assert.Equal(PidSpeedGear.ReverseValue, SwitchListStepPrereq.TargetReverser(true));
        Assert.Equal(PidSpeedGear.ForwardValue, SwitchListStepPrereq.TargetReverser(false));
    }

    [Fact]
    public void Smoke_13_4_gear_mismatch_holds_indy_pad_before_thr()
    {
        var state = default(PidSpeedState);
        var cmd = PidSpeedHold.Tick(
            new PidSpeedInput(
                0.05f,
                speedKmh: 0f,
                requestKmh: 25f,
                postedKmh: null,
                throttle: 0f,
                independent: 0f,
                armed: true,
                derailIntervening: false,
                thermalCeiling: 1f,
                reverser: ProximityTravelDirectionGate.NeutralValue,
                legNeedsReverse: true,
                trainBrake: 0f),
            ref state);
        Assert.True(cmd.GearPending);
        Assert.Equal(0f, cmd.DesiredThrottle);
        Assert.True(cmd.DesiredIndependent > 0f);
        Assert.True(cmd.BrakePending);
    }
}
