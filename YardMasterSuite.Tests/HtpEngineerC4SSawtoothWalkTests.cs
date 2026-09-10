using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Engineer-correct HTP: B4L → C4S must GO the pin/pose, not the bind word.
/// Harvest graph is track-forward (reverseCount=0). Cab 2.13.2.5.18 still
/// needed Reverse because the pin was behind.
/// </summary>
[Collection("StaticSessions")]
public sealed class HtpEngineerC4SSawtoothWalkTests
{
    public HtpEngineerC4SSawtoothWalkTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_path_travel_owns_go_when_bind_says_the_other_way()
    {
        Assert.True(EngineerFacingPolicy.GoNeedsReverse(
            planTravelReverse: true,
            bindNeedsReverse: false,
            pinStepActive: true,
            pinTravelReverse: false,
            destBehind: false,
            RouteClearancePhase.AtSwitch));
        Assert.True(EngineerFacingPolicy.GoNeedsReverse(
            planTravelReverse: null,
            bindNeedsReverse: false,
            pinStepActive: true,
            pinTravelReverse: true,
            destBehind: false,
            RouteClearancePhase.AtSwitch));
        Assert.False(EngineerFacingPolicy.GoNeedsReverse(
            planTravelReverse: null,
            bindNeedsReverse: true,
            pinStepActive: true,
            pinTravelReverse: false,
            destBehind: true,
            RouteClearancePhase.AtSwitch));
        Assert.True(EngineerFacingPolicy.BindLies(
            engineerNeedsReverse: true,
            bindNeedsReverse: false));
        Assert.True(EngineerFacingPolicy.HasDirectionChange(
            previousTravelReverse: false,
            nextTravelReverse: true));
    }

    [Fact]
    public void Smoke_B4L_to_C4S_harvest_pin_behind_beats_bind_forward()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55ViaSpur,
            HtpSetDestAuditTests.Sl55SecondPickup,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        Assert.Equal(HtpSetDestAuditTests.Sl55ViaSpur, plan.TrackIds[0]);
        Assert.Equal(
            HtpSetDestAuditTests.Sl55SecondPickup,
            plan.TrackIds[plan.TrackIds.Count - 1]);
        Assert.Null(EngineerFacingPolicy.PlanTravelReverse(plan));

        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    7,
                    SwitchListStepKind.Transit,
                    "SW",
                    HtpSetDestAuditTests.Sl55SecondPickup,
                    "Set Forward · Past switch → SW-C4S until CLEARED",
                    bindNeedsReverse: false),
            });
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: true);
        Assert.True(RoutePinLatch.HasLatch);
        Assert.True(RoutePinLatch.TravelUsesReverse);

        Assert.True(EngineerFacingPolicy.BindLies(
            engineerNeedsReverse: true,
            bindNeedsReverse: false));
        Assert.True(PidSpeedFacing.LegNeedsReverse(
            pinStepActive: true,
            pinStepReverse: RoutePinLatch.TravelUsesReverse,
            destBehind: true,
            RouteClearancePhase.AtSwitch,
            bindNeedsReverse: false,
            planTravelReverse: EngineerFacingPolicy.PlanTravelReverse(plan)));
    }

    [Fact]
    public void Smoke_B1S_to_B4L_observe_ahead_stays_forward_despite_bind_or_stale_pin()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55FirstPickup,
            HtpSetDestAuditTests.Sl55ViaSpur,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, plan.Status);
        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    HtpSetDestAuditTests.Sl55ViaSpur,
                    "Set Forward · Past switch → SW-B4L until CLEARED",
                    bindNeedsReverse: false),
            });
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: false);
        Assert.False(RoutePinLatch.TravelUsesReverse);
        Assert.False(
            PidSpeedFacing.LegNeedsReverse(
                pinStepActive: true,
                pinStepReverse: RoutePinLatch.TravelUsesReverse,
                destBehind: false,
                RouteClearancePhase.AtSwitch,
                bindNeedsReverse: true,
                planTravelReverse: EngineerFacingPolicy.PlanTravelReverse(plan)));
    }

    [Fact]
    public void Smoke_consecutive_pin_legs_flip_travel_B4L_then_C4S()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var b4l = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55FirstPickup,
            HtpSetDestAuditTests.Sl55ViaSpur,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        var c4s = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55ViaSpur,
            HtpSetDestAuditTests.Sl55SecondPickup,
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        Assert.NotEqual(PathCheckStatus.NoPath, b4l.Status);
        Assert.NotEqual(PathCheckStatus.NoPath, c4s.Status);

        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    6,
                    SwitchListStepKind.Transit,
                    "SW",
                    HtpSetDestAuditTests.Sl55ViaSpur,
                    "Past",
                    bindNeedsReverse: false),
            });
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", b4l, pinIsBehind: false);
        var first = RoutePinLatch.TravelUsesReverse;

        SwitchListSession.Bind(
            "SW-SL-55",
            new[]
            {
                new SwitchListStep(
                    7,
                    SwitchListStepKind.Transit,
                    "SW",
                    HtpSetDestAuditTests.Sl55SecondPickup,
                    "Past",
                    bindNeedsReverse: false),
            });
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", c4s, pinIsBehind: true);
        var second = RoutePinLatch.TravelUsesReverse;
        Assert.True(EngineerFacingPolicy.HasDirectionChange(first, second));
    }
}
