using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Gemini 4.9 kiss: synthesize rem off-aim so taper is not blind-12; Stop GO
/// before the frog / TT mid / cars. Cab FAIL on play 4.9 was <c>along=21 spd=12</c>.
/// </summary>
[Collection("StaticSessions")]
public class HtpYardTaperKissTests
{
    public HtpYardTaperKissTests() => YmsRouteSessions.ClearAll();

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

    /// <summary>
    /// Cab 2.13.2.5.11 SL-55 C4S: B1S solo 38 t kissed at rem=25 spd=25 and coupled;
    /// 86 t kissed at rem=13 still 25 km/h and hit. d_stop must lengthen with mass.
    /// </summary>
    [Fact]
    public void Smoke_sl55_c4s_86t_prep_kiss_leads_solo_38t()
    {
        var cruise = YardKissPolicy.CruiseKmh;
        var prep = Prep();
        const float leftoverAfterBlindLock = 30f;

        var soloTrigger = YardArrivalStopPolicy.KissTriggerRemMeters(
            cruise,
            YardKissAim.PrepCars,
            massTonnes: 38f);
        var heavyTrigger = YardArrivalStopPolicy.KissTriggerRemMeters(
            cruise,
            YardKissAim.PrepCars,
            massTonnes: 86f);

        Assert.Equal(
            YardArrivalStopPolicy.KissTriggerRemMeters(cruise, YardKissAim.PrepCars),
            soloTrigger,
            precision: 3);
        Assert.True(soloTrigger < leftoverAfterBlindLock);
        Assert.True(heavyTrigger > leftoverAfterBlindLock);
        Assert.True(heavyTrigger > 13.5f);

        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                leftoverAfterBlindLock,
                cruise,
                massTonnes: 38f));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            YardKissPolicy.TryKiss(
                SwitchListRunMode.Go,
                prep,
                leftoverAfterBlindLock,
                cruise,
                massTonnes: 86f));
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Go,
                prep,
                new[] { Past(), prep },
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                remToAimMeters: leftoverAfterBlindLock,
                speedKmh: cruise,
                massTonnes: 86f));
    }

    /// <summary>
    /// Cab 2.13.2.5.12: first Prep crawled 10 from the frog. Restore 25 cruise
    /// until kiss even when the knuckle laser is blind.
    /// </summary>
    [Fact]
    public void Smoke_sl55_first_prep_cruises_25_from_switch_when_laser_blind()
    {
        var prep = Prep();
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, 67f, null, null, null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForStep(prep));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(prep, 40f, 79.4f, null, null));
        Assert.Equal(
            YardKissPolicy.CruiseKmh,
            PidSpeedTarget.RequestForYardStep(ToTt(), 40f, null, null, null));
        Assert.Equal(2, ConsistTravelLead.ApproachTipIndex(carCount: 3, useFront: false));
        Assert.Equal(0, ConsistTravelLead.ApproachTipIndex(carCount: 3, useFront: true));
    }

    /// <summary>
    /// Cab 2.13.2.5.14: after B1S couple, GO Forward Past B4L stopped At switch,
    /// speed 0, loco on frog <c>#Y-#S23#T</c>, 86 t consist still on the points.
    /// Kiss rem is frog + new consist length, not loco-nose. Couple length
    /// Observe must extend the same braking curve — no inch-forward after stop.
    /// </summary>
    [Fact]
    public void Smoke_sl55_after_couple_past_B4L_loco_on_frog_is_not_CLEARED_kiss()
    {
        const float bobtailM = 18f;
        const float coupledM = 70f;
        const float frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        const float noseOnFrog = 0f;
        const float massT = 86f;
        var cruise = YardKissPolicy.CruiseKmh;
        var past = new SwitchListStep(
            6,
            SwitchListStepKind.Transit,
            "SW",
            "SW-B4L",
            "Set Forward · Past switch → SW-B4L until CLEARED",
            bindNeedsReverse: false);
        var steps = new[] { past, Prep() };

        var noseAimRem = YardApproachKinematics.PinApproachRemFromNosePast(noseOnFrog);
        Assert.Equal(0f, noseAimRem);
        Assert.True(
            YardKissPolicy.InKissZone(noseAimRem, cruise, YardKissAim.Cleared, massT),
            "loco-nose rem=0 is the cab fail — must not be the Past-switch aim");

        var bobtailRem = YardApproachKinematics.RemToClearedMeters(noseOnFrog, bobtailM, frog);
        var coupledRem = YardApproachKinematics.RemToClearedMeters(noseOnFrog, coupledM, frog);
        Assert.Equal(frog + bobtailM, bobtailRem);
        Assert.Equal(frog + coupledM, coupledRem);
        Assert.True(
            coupledRem > YardArrivalStopPolicy.KissTriggerRemMeters(
                cruise,
                YardKissAim.Cleared,
                massT));
        Assert.False(
            YardKissPolicy.InKissZone(coupledRem, cruise, YardKissAim.Cleared, massT));
        Assert.False(
            RouteClearanceEval.IsClearedOfFrog(
                new RouteClearanceSample(true, noseOnFrog, coupledM, frog, 120f)));
        Assert.True(
            RouteClearanceEval.IsClearedOfFrog(
                new RouteClearanceSample(true, frog + coupledM, coupledM, frog, 120f)));

        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.AtSwitch,
                fouling: true,
                canThrowAlign: false,
                canAdvanceNext: false,
                caption: "At switch"),
            pinJunctionId: "Y-S23",
            pinX: 0f,
            pinY: 0f,
            pinZ: 0f,
            nosePastJunctionM: noseOnFrog,
            consistLengthM: bobtailM);
        Assert.Equal(
            "T2 route-pin: consist-clear len=70",
            ConsistLengthSession.ObserveIncrease(coupledM));

        Assert.Equal(coupledRem, RouteClearanceSession.RemToClearedMeters);
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
                remToAimMeters: RouteClearanceSession.RemToClearedMeters,
                speedKmh: cruise,
                massTonnes: massT));
    }

    /// <summary>
    /// Cab 2.13.2.5.15: couple logged coupler-span <c>len=44</c> / 86 t, kissed
    /// at rem=35, sat At switch rem=7 with tail still on <c>#Y-#S23#T</c>.
    /// Occupancy (bounds vs coupler) must be the CLEARED length; mass 86 t
    /// is live at couple. Do not change Prep kiss slack here.
    /// </summary>
    [Fact]
    public void Smoke_sl55_86t_coupler_44_occupancy_not_coupler_span()
    {
        const float frog = RouteClearanceEval.DefaultFrogEnvelopeM;
        const float couplerSumM = 44f;
        const float leftoverFoulM = 7f;
        const float occupancyM = couplerSumM + leftoverFoulM;
        const float kissRemCoupler = 35f;
        const float massT = 86f;
        var cruise = YardKissPolicy.CruiseKmh;
        var nosePastAtKiss = frog + couplerSumM - kissRemCoupler;

        Assert.Equal(12f, ConsistLengthMeters.OccupancyCar(7.5f, 12f));
        Assert.Equal(occupancyM, ConsistLengthMeters.Sum(new[] { occupancyM }));

        var couplerRem = YardApproachKinematics.RemToClearedMeters(
            nosePastAtKiss,
            couplerSumM,
            frog);
        var occupancyRem = YardApproachKinematics.RemToClearedMeters(
            nosePastAtKiss,
            occupancyM,
            frog);
        Assert.Equal(kissRemCoupler, couplerRem);
        Assert.Equal(kissRemCoupler + leftoverFoulM, occupancyRem);

        ConsistMassSession.Observe(massT);
        Assert.Equal(massT, ConsistMassSession.Tonnes);
        var trigger = YardArrivalStopPolicy.KissTriggerRemMeters(
            cruise,
            YardKissAim.Cleared,
            ConsistMassSession.Tonnes);
        Assert.True(YardKissPolicy.InKissZone(couplerRem, cruise, YardKissAim.Cleared, massT));
        Assert.True(occupancyRem > trigger);
        Assert.False(
            YardKissPolicy.InKissZone(occupancyRem, cruise, YardKissAim.Cleared, massT));
        Assert.False(
            RouteClearanceEval.IsClearedOfFrog(
                new RouteClearanceSample(true, nosePastAtKiss, occupancyM, frog, 120f)));
    }
}
