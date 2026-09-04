using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// <b>13.4.18</b> Gemini B — yard crawl cap on drive-to-TT / Prep so d_stop fits on rail.
/// </summary>
public class HtpYardApproachStopTests
{
    [Fact]
    public void Smoke_13_4_18_drive_to_tt_request_capped_at_yard_crawl()
    {
        var toTt = new SwitchListStep(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));
        var prep = new SwitchListStep(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");
        var transit = new SwitchListStep(1, SwitchListStepKind.Transit, "SW", "SW-B4L", "Past switch");

        Assert.Equal(10f, PidSpeedTarget.YardApproachRequestKmh);
        Assert.True(PidSpeedTarget.WantsYardApproachCap(toTt));
        Assert.True(PidSpeedTarget.WantsYardApproachCap(prep));
        Assert.False(PidSpeedTarget.WantsYardApproachCap(transit));
        Assert.Equal(PidSpeedTarget.YardApproachRequestKmh, PidSpeedTarget.RequestForStep(toTt));
        Assert.Equal(PidSpeedTarget.YardApproachRequestKmh, PidSpeedTarget.RequestForStep(prep));
        Assert.Equal(PidSpeedTarget.DefaultRequestKmh, PidSpeedTarget.RequestForStep(transit));
        Assert.Equal(
            10f,
            PidSpeedTarget.Resolve(PidSpeedTarget.RequestForStep(toTt), postedKmh: 40f));
    }

    [Fact]
    public void Smoke_13_4_18_yard_crawl_d_stop_fits_half_of_25m_tt()
    {
        var dStop = YardStopKinematics.StoppingDistanceMeters(PidSpeedTarget.YardApproachRequestKmh);
        Assert.True(dStop < 12.5f);
        Assert.True(
            YardStopKinematics.ShouldStartStop(
                remToAimMeters: dStop,
                speedKmh: PidSpeedTarget.YardApproachRequestKmh,
                aimToleranceMeters: TurntableArrivalGate.MidpointToleranceMeters));
        Assert.False(
            YardStopKinematics.ShouldStartStop(
                remToAimMeters: 12f,
                speedKmh: PidSpeedTarget.YardApproachRequestKmh,
                aimToleranceMeters: TurntableArrivalGate.MidpointToleranceMeters));
    }
}
