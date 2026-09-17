using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.13.2.5.16: after Prep couple, HUD still <c>Handbrakes 1</c> on the
/// parked cut — GO Past B4L drags the set with a parking brake on.
/// </summary>
[Collection("StaticSessions")]
public class PrepHandbrakeReleaseTests
{
    public PrepHandbrakeReleaseTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_prep_couple_still_Handbrakes_1_releases_on_couple_success()
    {
        Assert.True(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Prep,
            coupleSuccess: true));
        Assert.Equal(
            SwitchListRunnerTelemetry.PrepHandbrakeRelease + "1",
            PrepHandbrakeRelease.FormatLog(releasedCount: 1));
        Assert.Equal(0f, PrepHandbrakeRelease.ReleasedPosition);
    }

    [Fact]
    public void Smoke_prep_couple_does_not_release_on_transit_or_delivery()
    {
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Transit,
            coupleSuccess: true));
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Delivery,
            coupleSuccess: true));
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Prep,
            coupleSuccess: false));
        Assert.Null(PrepHandbrakeRelease.FormatLog(releasedCount: 0));
    }

    [Fact]
    public void Smoke_pull_out_waits_until_loco_and_cars_handbrakes_are_off()
    {
        Assert.True(PrepHandbrakeRelease.AllowsMove(appliedCount: 0));
        Assert.True(PrepHandbrakeRelease.AllowsMove(appliedCount: null));
        Assert.False(PrepHandbrakeRelease.AllowsMove(appliedCount: 1));
        Assert.Equal(
            "T2 prep: handbrake-wait n=1",
            PrepHandbrakeRelease.FormatWait(appliedCount: 1));
        Assert.Null(PrepHandbrakeRelease.FormatWait(appliedCount: 0));
        var pullOut = new SwitchListStep(
            2,
            SwitchListStepKind.Transit,
            "SW",
            "SW-C4S",
            "Set Forward · Past switch → SW-C4S until CLEARED",
            bindNeedsReverse: false);
        Assert.False(
            SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
                pullOut,
                holdAfterCouple: true,
                fullyStoppedAndIdle: true,
                reverserMatches: true,
                motorsHealthy: true,
                handbrakesReleased: false));
        Assert.True(
            SwitchListYardChain.ShouldClearCoupleHoldAndResumeGo(
                pullOut,
                holdAfterCouple: true,
                fullyStoppedAndIdle: true,
                reverserMatches: true,
                motorsHealthy: true,
                handbrakesReleased: true));
    }
}
