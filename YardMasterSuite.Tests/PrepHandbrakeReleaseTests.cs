using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.13.2.5.16: after Prep couple, HUD still <c>Handbrakes 1</c> on the
/// parked cut — GO Past B4L drags the set with a parking brake on.
/// Cab 2.13.2.5.22.16: Next off Prep then physical couple — still release.
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
    public void Smoke_SL55_couple_after_Next_off_Prep_still_releases()
    {
        Assert.True(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Transit,
            coupleSuccess: true));
        Assert.True(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Pivot,
            coupleSuccess: true));
        Assert.True(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.ReverseInto,
            coupleSuccess: true));
        Assert.True(PrepHandbrakeRelease.ShouldReleaseOnTonnesJoin(
            38,
            86,
            SwitchListStepKind.Transit));
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnTonnesJoin(
            38,
            40,
            SwitchListStepKind.Transit));
    }

    [Fact]
    public void Smoke_prep_couple_does_not_release_on_delivery()
    {
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Delivery,
            coupleSuccess: true));
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            SwitchListStepKind.Prep,
            coupleSuccess: false));
        Assert.False(PrepHandbrakeRelease.ShouldReleaseOnCoupleSuccess(
            kind: null,
            coupleSuccess: true));
        Assert.Null(PrepHandbrakeRelease.FormatLog(releasedCount: 0));
    }
}
