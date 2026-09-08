using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 4.13 e2e PASS: dest-entry rem + auto-spin. Consist-mid kiss lead
/// (<c>along=21 spd=19</c> → aim 18.5).
/// </summary>
[Collection("StaticSessions")]
public class HtpTurntableMidSpinTests
{
    public HtpTurntableMidSpinTests() => YmsRouteSessions.ClearAll();

    private const float TableLen = 25f;
    private const float ConsistLen = 12f;

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

    private static SwitchListStep Spin() =>
        new(
            3,
            SwitchListStepKind.TurnAround,
            "SW",
            "#Y-#S1774#T",
            SwitchListDriveFacing.TurnAroundOnTurntable);

    private static SwitchListStep Leave() =>
        new(4, SwitchListStepKind.Transit, "SW", "#Y-#S1512#T", "Past switch until CLEARED");

    private static SwitchListStep Prep() =>
        new(5, SwitchListStepKind.Prep, "SW", "SW-C1O", "Prep → SW-C1O");

    [Fact]
    public void Smoke_tt_rem_is_consist_center_vs_table_mid()
    {
        Assert.Equal(12.5f, TurntableArrivalGate.MidpointAlongMeters(TableLen));
        Assert.Equal(
            18.5f,
            TurntableArrivalGate.LeadingAlongForConsistMid(TableLen, ConsistLen),
            3);

        // Front bogie at 6 → consist center at 0; still 12.5 m to table mid.
        Assert.Equal(
            12.5f,
            TurntableArrivalGate.RemToConsistMidMeters(6f, TableLen, ConsistLen),
            3);

        // Consist center on mid when leading along = 18.5.
        Assert.Equal(
            0f,
            TurntableArrivalGate.RemToConsistMidMeters(18.5f, TableLen, ConsistLen),
            3);

        // Cab 4.13: on TT along=21 — 2.5 m past consist-mid (aim 18.5).
        Assert.Equal(
            2.5f,
            TurntableArrivalGate.RemToConsistMidMeters(21f, TableLen, ConsistLen),
            3);

        // Unknown consist keeps bogie-at-mid (legacy 12.5).
        Assert.Equal(
            12.5f,
            TurntableArrivalGate.RemToConsistMidMeters(0f, TableLen, 0f),
            3);
    }

    [Fact]
    public void Smoke_tt_off_rail_rem_is_corridor_plus_mid_plus_half_consist()
    {
        Assert.Equal(
            52.5f,
            YardApproachKinematics.SynthesizeRemToAim(
                ToTt(),
                corridorRemMeters: 40f,
                hudProximityMeters: null,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null));
        Assert.Equal(
            58.5f,
            YardApproachKinematics.SynthesizeRemToAim(
                ToTt(),
                corridorRemMeters: 40f,
                hudProximityMeters: null,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null,
                consistLengthMeters: ConsistLen));
        Assert.Equal(
            19f,
            YardApproachKinematics.SynthesizeRemToAim(
                ToTt(),
                corridorRemMeters: 6.5f,
                hudProximityMeters: null,
                pinRemToClearedMeters: null,
                ttRemToMidMeters: null,
                consistLengthMeters: 0f));
    }

    [Fact]
    public void Smoke_tt_kiss_fires_off_rail_before_along_21()
    {
        var cruise = YardKissPolicy.CruiseKmh;
        var kissRem = YardArrivalStopPolicy.KissTriggerRemMeters(
            cruise,
            YardKissAim.TurntableMid);
        var steps = new[] { ToTt(), Spin(), Leave() };
        var offRailKiss = YardApproachKinematics.SynthesizeRemToAim(
            ToTt(),
            corridorRemMeters: 6.5f,
            hudProximityMeters: null,
            pinRemToClearedMeters: null,
            ttRemToMidMeters: null,
            consistLengthMeters: ConsistLen);

        Assert.True(offRailKiss is float rem && rem <= kissRem);
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), offRailKiss, cruise));
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
                remToAimMeters: offRailKiss,
                speedKmh: cruise));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), 80f, cruise));
    }

    [Fact]
    public void Smoke_tt_kiss_fires_off_rail_using_entry_rem()
    {
        var cruise = YardKissPolicy.CruiseKmh;
        var steps = new[] { ToTt(), Spin(), Leave() };

        // 20 m from dest hop entry, consist 12 → 20 + 12.5 + 6 = 38.5; not yet kiss.
        var far = YardApproachKinematics.SynthesizeRemToAim(
            ToTt(),
            corridorRemMeters: 20f,
            hudProximityMeters: null,
            pinRemToClearedMeters: null,
            ttRemToMidMeters: null,
            consistLengthMeters: ConsistLen);
        Assert.Equal(38.5f, far);
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), far, cruise));

        // 6 m from dest hop entry → 24.5; Stop GO before the rail.
        var near = YardApproachKinematics.SynthesizeRemToAim(
            ToTt(),
            corridorRemMeters: 6f,
            hudProximityMeters: null,
            pinRemToClearedMeters: null,
            ttRemToMidMeters: null,
            consistLengthMeters: ConsistLen);
        Assert.Equal(24.5f, near);
        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), near, cruise));
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
                remToAimMeters: near,
                speedKmh: cruise));

        // Cab FAIL rem=124 was walker leftover, not dest entry.
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), 124f, cruise));
    }

    [Fact]
    public void Smoke_tt_live_corridor_uses_dest_entry_not_path_end()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B3I", "#Y-#S1774#T" },
            System.Array.Empty<PathJunctionEval>(),
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 50f);
        RoutePlanSession.SetPlan(plan, "SW-B3I", travelEtaSeconds: 50f);
        ConsistLengthSession.Observe(ConsistLen);
        RoutePlanSession.SetRemainingEta(10f, 124f, 4000f, 0.5f, 0f, "live", remToDestEntryMeters: 6f);
        Assert.Equal(24.5f, YardApproachKinematics.FromLiveSessions(ToTt()));

        RoutePlanSession.SetRemainingEta(10f, 124f, 4000f, 0.5f, 0f, "live");
        Assert.Null(YardApproachKinematics.FromLiveSessions(ToTt()));
    }

    [Fact]
    public void Smoke_tt_rem_observes_dest_bogie_without_unique_track()
    {
        Assert.Equal(
            17.5f,
            TurntableArrivalGate.RemToMidOnDestTrack(
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 1f,
                trackLengthMeters: TableLen,
                uniqueTrack: false,
                consistLengthMeters: ConsistLen));
        Assert.Equal(
            TurntableArrival.Ambiguous,
            TurntableArrivalGate.Evaluate(
                ToTt(),
                "#Y-#S1774#T",
                "#Y-#S1774#T",
                spanMeters: 1f,
                trackLengthMeters: TableLen,
                uniqueTrack: false,
                speedKmh: 25f,
                consistLengthMeters: ConsistLen));
    }

    [Fact]
    public void Smoke_tt_stop_then_auto_spin_then_leave()
    {
        var steps = new[] { ToTt(), Spin(), Leave() };

        Assert.True(TurntableSpinPolicy.StepIsSpin(Spin()));
        Assert.False(TurntableSpinPolicy.StepIsSpin(ToTt()));
        Assert.False(SwitchListRunner.StepSupportsGo(Spin()));

        Assert.True(TurntableArrivalSession.TryArrive(TurntableArrival.AtTrack));
        Assert.Equal(
            SwitchListYardChainAction.AdvanceToTtSpin,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[0],
                steps,
                currentIndex: 0,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: true,
                speedKmh: 0f));

        Assert.Equal(
            SwitchListYardChainAction.StartTtSpin,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: true,
                ttSpinActive: false,
                ttSpinLocked: false,
                speedKmh: 0f));

        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: true,
                ttSpinActive: true,
                ttSpinLocked: false,
                speedKmh: 0f));

        Assert.Equal(
            SwitchListYardChainAction.SpinDoneNext,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: true,
                ttSpinActive: true,
                ttSpinLocked: true,
                speedKmh: 0f));
    }

    [Fact]
    public void Smoke_tt_on_step_entered_keeps_on_table_through_spin()
    {
        Assert.True(TurntableArrivalSession.TryArrive(TurntableArrival.AtTrack));
        SwitchListRunnerSession.OnStepEntered(Spin());
        Assert.True(TurntableArrivalSession.OnTable);
        SwitchListRunnerSession.OnStepEntered(Leave());
        Assert.False(TurntableArrivalSession.OnTable);
    }

    [Fact]
    public void Smoke_tt_spin_target_is_180_from_arrival_yaw()
    {
        Assert.Equal(180f, TurntableSpinPolicy.OppositeYaw(0f), 3);
        Assert.Equal(10f, TurntableSpinPolicy.OppositeYaw(190f), 3);
        Assert.True(TurntableSpinPolicy.IsLocked(0.4f));
        Assert.False(TurntableSpinPolicy.IsLocked(1f));
        Assert.Equal(12f, TurntableSpinPolicy.MaxDegreesPerSecond);
    }

    [Fact]
    public void Smoke_tt_cab_413_along_21_spd_19_kiss_2_5m_earlier_lands_consist_mid()
    {
        // Cab 4.13: Prep-biased kiss at rem=26 rested along=21. Lead 2.5 → 18.5.
        var cruise = YardKissPolicy.CruiseKmh;
        var prepTrigger = YardArrivalStopPolicy.KissTriggerRemMeters(cruise);
        var ttTrigger = YardArrivalStopPolicy.KissTriggerRemMeters(
            cruise,
            YardKissAim.TurntableMid);
        var aimAlong = TurntableArrivalGate.LeadingAlongForConsistMid(TableLen, ConsistLen);

        Assert.Equal(2.5f, YardArrivalStopPolicy.TurntableMidLeadMeters, 3);
        Assert.Equal(prepTrigger + 2.5f, ttTrigger, 3);
        Assert.Equal(18.5f, aimAlong, 3);
        Assert.Equal(18.5f, 21f - YardArrivalStopPolicy.TurntableMidLeadMeters, 3);
        Assert.Equal(
            0f,
            TurntableArrivalGate.RemToConsistMidMeters(aimAlong, TableLen, ConsistLen),
            3);

        Assert.Equal(
            SwitchListYardChainAction.StopGoAtTurntable,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), ttTrigger, cruise));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, ToTt(), ttTrigger + 1f, cruise));

        // Same 25-brake as Prep; do not steal the knuckle's 2 m-later bias.
        Assert.Equal(
            SwitchListYardChainAction.StopGoKissPrep,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, Prep(), prepTrigger, cruise));
        Assert.Equal(
            SwitchListYardChainAction.None,
            YardKissPolicy.TryKiss(SwitchListRunMode.Go, Prep(), ttTrigger, cruise));
    }

    [Fact]
    public void Smoke_tt_pit_unique_false_does_not_spin()
    {
        var steps = new[] { ToTt(), Spin(), Leave() };

        // Cab FAIL: OnTable sticky after along=20 spd=24, then spin into the pit.
        Assert.False(
            TurntableSpinPolicy.ShouldAdvanceToSpin(
                SwitchListRunMode.Manual,
                ToTt(),
                Spin(),
                onTurntable: true,
                goStopActive: false,
                speedKmh: 0f,
                uniqueOnDest: false));
        Assert.False(
            TurntableSpinPolicy.ShouldStartSpin(
                SwitchListRunMode.Manual,
                Spin(),
                onTurntable: true,
                spinActive: false,
                spinLocked: false,
                uniqueOnDest: false));
        Assert.Equal(
            SwitchListYardChainAction.None,
            SwitchListYardChain.Evaluate(
                SwitchListRunMode.Manual,
                steps[1],
                steps,
                currentIndex: 1,
                RouteClearancePhase.Idle,
                prepAtSpur: false,
                hasPlan: true,
                onTurntable: true,
                ttSpinActive: false,
                ttSpinLocked: false,
                speedKmh: 0f,
                uniqueOnDest: false));
    }
}
