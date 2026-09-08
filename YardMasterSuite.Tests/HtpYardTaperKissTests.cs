using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Gemini 4.9 kiss: synthesize rem off-aim so taper is not blind-12; Stop GO
/// before the frog / TT mid / cars. Cab FAIL on play 4.9 was <c>along=21 spd=12</c>.
/// </summary>
public class HtpYardTaperKissTests
{
    private static SwitchListStep ToTt() =>
        new(
            2,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.FormatDriveLabel(
                false,
                SwitchListDriveFacing.ToTurntableAction,
                "#Y-#S1774#T"));

    private static SwitchListStep Past() =>
        new(4, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Past switch until CLEARED");

    private static SwitchListStep Prep() =>
        new(5, SwitchListStepKind.Prep, "SW", "SW-B1S", "Prep → SW-B1S");

    [Fact]
    public void Smoke_tt_approach_uses_corridor_plus_midpoint_not_blind_12()
    {
        var step = ToTt();
        var rem = YardApproachKinematics.SynthesizeRemToAim(
            step,
            corridorRemMeters: 40f,
            hudProximityMeters: null,
            pinRemToClearedMeters: null,
            ttRemToMidMeters: null);
        Assert.Equal(52.5f, rem);
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(
                step,
                corridorRemMeters: 40f,
                hudProximityMeters: null,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(
                step,
                40f,
                null,
                null,
                null));
    }

    [Fact]
    public void Smoke_predictive_stop_fires_before_aim_reached()
    {
        var dStop = YardStopKinematics.StoppingDistanceMeters(10f);
        Assert.True(
            YardArrivalStopPolicy.ShouldStopGo(
                SwitchListRunMode.Go,
                remToAimMeters: dStop,
                speedKmh: 10f,
                aimToleranceMeters: 2f));
        Assert.True(
            YardArrivalStopPolicy.ShouldStopGo(
                SwitchListRunMode.Go,
                remToAimMeters: 2f,
                speedKmh: 10f,
                aimToleranceMeters: 2f));
        Assert.False(
            YardArrivalStopPolicy.ShouldStopGo(
                SwitchListRunMode.Go,
                remToAimMeters: 40f,
                speedKmh: 10f,
                aimToleranceMeters: 2f));
    }

    [Fact]
    public void Smoke_sl55_past_switch_ignores_corridor_until_pin_rem()
    {
        var past = Past();
        Assert.Null(
            YardApproachKinematics.SynthesizeRemToAim(
                past,
                corridorRemMeters: 12f,
                hudProximityMeters: null,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null));
        Assert.Equal(
            40f,
            YardApproachKinematics.SynthesizeRemToAim(
                past,
                corridorRemMeters: 12f,
                hudProximityMeters: null,
                pinRemToClearedMeters: 40f,
                ttRemToMidMeters: null));
        Assert.Equal(
            YardApproachKinematics.CruiseSpeedKmh,
            PidSpeedTarget.RequestForYardStep(past, 12f, null, null, null));
        Assert.Equal(
            YardApproachKinematics.CruiseSpeedKmh,
            PidSpeedTarget.RequestForYardStep(past, 12f, null, 40f, null));
    }

    [Fact]
    public void Smoke_sl55_past_switch_kiss_stop_does_not_next_before_cleared()
    {
        var steps = new[] { Past(), Prep() };
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissCleared,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: 1f,
                speedKmh: 5f));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                goStopActive: true,
                remToAimMeters: 0f,
                speedKmh: 0f));
    }

    [Fact]
    public void Smoke_sl55_past_switch_nexts_only_after_cleared_and_stopped()
    {
        var steps = new[] { Past(), Prep() };
        Assert.Equal(
            SwitchListYardChainAction.StopGoCompleteCleared,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.Cleared,
                prepAtSpur: false,
                hasPlan: true,
                goStopActive: false,
                remToAimMeters: 0f,
                speedKmh: 0f));
    }

    [Fact]
    public void Smoke_sl55_past_switch_holds_25_until_kiss_zone_then_stop_not_rear()
    {
        var past = Past();
        var steps = new[] { past, Prep() };
        var cruise = PidSpeedTarget.DefaultRequestKmh;
        var kissRem = YardArrivalStopPolicy.KissTriggerRemMeters(cruise);

        Assert.False(
            YardArrivalStopPolicy.ShouldKissCleared(SwitchListRunMode.Go, remToClearedMeters: 80f, cruise));
        Assert.True(
            YardArrivalStopPolicy.ShouldKissCleared(SwitchListRunMode.Go, kissRem, cruise));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                past,
                steps,
                currentIndex: 0,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: 80f,
                speedKmh: cruise));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissCleared,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                past,
                steps,
                currentIndex: 0,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: kissRem,
                speedKmh: cruise));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                past,
                steps,
                currentIndex: 0,
                RouteClearancePhase.AtSwitch,
                prepAtSpur: false,
                hasPlan: true,
                goStopActive: false,
                remToAimMeters: 0f,
                speedKmh: 0f));
    }

    [Fact]
    public void Smoke_prep_and_tt_cruise_25_until_kiss_not_blind_12_or_taper()
    {
        var prep = Prep();
        var toTt = ToTt();
        Assert.Equal(YardKissAim.PrepCars, YardKissPolicy.AimFor(prep));
        Assert.Equal(YardKissAim.TurntableMid, YardKissPolicy.AimFor(toTt));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, 80f, 80f, null, null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, 80f, 1.5f, null, null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForStep(prep));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForStep(toTt));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(toTt, 40f, null, null, null));
    }

    [Fact]
    public void Smoke_kiss_aim_tt_mid_and_prep_cars_same_zone_as_cleared()
    {
        var cruise = YardKissPolicy.CruiseKmh;
        var kissRem = YardArrivalStopPolicy.KissTriggerRemMeters(cruise);
        var steps = new[] { ToTt(), Prep() };

        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), 80f, cruise));
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), kissRem, cruise));
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: kissRem,
                speedKmh: cruise));

        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, Prep(), 80f, cruise));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, Prep(), kissRem, cruise));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: kissRem,
                speedKmh: cruise));
    }

    [Fact]
    public void Smoke_past_switch_far_is_cruise_not_flat_10()
    {
        var past = Past();
        Assert.True(PidSpeedTarget.WantsYardTaper(past));
        Assert.Equal(
            YardApproachKinematics.CruiseSpeedKmh,
            PidSpeedTarget.RequestForYardStep(past, 80f, null, null, null));
        Assert.Equal(
            YardApproachKinematics.CruiseSpeedKmh,
            PidSpeedTarget.RequestForYardStep(past, 80f, null, 12f, null));
    }

    [Fact]
    public void Smoke_prep_kiss_car_hud_not_corridor_pad_or_spur_stop()
    {
        var prep = Prep();
        var cruise = YardKissPolicy.CruiseKmh;
        var kissRem = YardArrivalStopPolicy.KissTriggerRemMeters(cruise);
        var steps = new[] { Past(), prep };

        Assert.Null(
            YardApproachKinematics.SynthesizeRemToAim(
                prep,
                corridorRemMeters: 40f,
                hudProximityMeters: null,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null));
        Assert.Equal(
            80f,
            YardApproachKinematics.SynthesizeRemToAim(
                prep,
                corridorRemMeters: 40f,
                hudProximityMeters: 80f,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null));
        Assert.Equal(
            cruise,
            PidSpeedTarget.RequestForYardStep(prep, 40f, null, null, null));

        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: true,
                hasPlan: true,
                remToAimMeters: 80f,
                speedKmh: cruise));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: true,
                hasPlan: true,
                remToAimMeters: kissRem,
                speedKmh: cruise));
        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: kissRem,
                speedKmh: 0f));
    }

    [Fact]
    public void Smoke_kiss_fires_two_meters_later_than_slack_envelope()
    {
        var cruise = YardKissPolicy.CruiseKmh;
        var oldEnvelope = YardStopKinematics.StoppingDistanceMeters(cruise)
            + YardArrivalStopPolicy.ClearedKissSlackMeters;
        var kissRem = YardArrivalStopPolicy.KissTriggerRemMeters(cruise);

        Assert.Equal(
            oldEnvelope - YardArrivalStopPolicy.KissLandingBiasMeters,
            kissRem,
            precision: 3);
        Assert.False(YardKissPolicy.InKissZone(oldEnvelope, cruise));
        Assert.True(YardKissPolicy.InKissZone(kissRem, cruise));
    }

    [Fact]
    public void Smoke_prep_kiss_lands_in_couple_scan_no_second_go()
    {
        var prep = Prep();
        var steps = new[] { Past(), prep };
        const float leftoverRem = 2.1f;
        const float landedRem = 1.2f;

        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, null, leftoverRem, null, null));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                leftoverRem,
                speedKmh: 0f));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: leftoverRem,
                speedKmh: 0f));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                landedRem,
                speedKmh: YardKissPolicy.CruiseKmh));
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtCouple,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleStop: true,
                remToAimMeters: landedRem,
                speedKmh: YardKissPolicy.CruiseKmh));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                prepCoupleHold: true,
                remToAimMeters: landedRem,
                speedKmh: 0f));
        Assert.Equal(
            SwitchListYardChainAction.ArmGo,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                prep,
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: 25f,
                speedKmh: 0f));
    }
}
