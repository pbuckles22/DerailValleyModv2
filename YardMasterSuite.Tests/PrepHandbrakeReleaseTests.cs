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
}
