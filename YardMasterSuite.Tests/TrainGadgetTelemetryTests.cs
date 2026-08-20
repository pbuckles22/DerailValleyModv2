using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: cab Mass + Grade (**6.5**), Load + Motors + Fluids (**6.6**), MU (**6.7**).
/// Publish on display-bucket change only — not every 10 Hz tick.
/// </summary>
public class TrainGadgetTelemetryTests
{
    [Fact]
    public void Smoke_cab_grade_ticks_when_slope_changes_with_handbrake_held()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(Observe(ref cache, gradePercent: 0f, massTonnes: 74f, handbrakes: 2));
        Assert.Equal(
            "T2 gadgets init: grade=0.0 mass=74 load=— fuel=— oil=— motors=— mu=—",
            NextLog(TrainGadgetLogKind.Init, 0f, ref lastAt, gradePercent: 0f, massTonnes: 74f));

        Assert.True(Observe(ref cache, gradePercent: 1.24f, massTonnes: 74f, handbrakes: 2));
        lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=+1.2 mass=74 load=— fuel=— oil=— motors=— mu=—",
            NextLog(TrainGadgetLogKind.Change, 10f, ref lastAt, gradePercent: 1.24f, massTonnes: 74f));
    }

    [Fact]
    public void Smoke_sw_b3i_cab_held_emits_T2_gadgets_init_grade_plus_04_mass_74()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(Observe(ref cache, gradePercent: 0.4f, massTonnes: 74f, handbrakes: 1));
        Assert.Equal(
            "T2 gadgets init: grade=+0.4 mass=74 load=— fuel=— oil=— motors=— mu=—",
            NextLog(TrainGadgetLogKind.Init, 0f, ref lastAt, gradePercent: 0.4f, massTonnes: 74f));
    }

    [Fact]
    public void Smoke_solo_de2_drive_emits_T2_gadgets_change_grade_minus_16()
    {
        var cache = default(TrainGadgetCache);
        Observe(ref cache, gradePercent: 0.4f, massTonnes: 38f, handbrakes: 0);

        Assert.True(Observe(ref cache, gradePercent: -1.6f, massTonnes: 38f, handbrakes: 0));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=-1.6 mass=38 load=— fuel=— oil=— motors=— mu=—",
            NextLog(TrainGadgetLogKind.Change, 10f, ref lastAt, gradePercent: -1.6f, massTonnes: 38f));
    }

    [Fact]
    public void Smoke_cab_de2_emits_T2_gadgets_init_with_load_fuel_oil_motors()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            handbrakes: 1,
            fuelPercent: 80.4f,
            oilPercent: 90.2f,
            loadPercent: 12.4f,
            motors: MotorStatus.Ok));
        Assert.Equal(
            "T2 gadgets init: grade=+0.4 mass=74 load=12 fuel=80 oil=90 motors=OK mu=—",
            NextLog(
                TrainGadgetLogKind.Init,
                0f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 74f,
                fuelPercent: 80.4f,
                oilPercent: 90.2f,
                loadPercent: 12.4f,
                motors: MotorStatus.Ok));
    }

    [Fact]
    public void Smoke_sw_b3i_cab_emits_T2_gadgets_init_load_0_fuel_96_oil_92_motors_ok()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            handbrakes: 1,
            fuelPercent: 96f,
            oilPercent: 92f,
            loadPercent: 0f,
            motors: MotorStatus.Ok));
        Assert.Equal(
            "T2 gadgets init: grade=+0.4 mass=74 load=0 fuel=96 oil=92 motors=OK mu=—",
            NextLog(
                TrainGadgetLogKind.Init,
                0f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 74f,
                fuelPercent: 96f,
                oilPercent: 92f,
                loadPercent: 0f,
                motors: MotorStatus.Ok));
    }

    [Fact]
    public void Smoke_sw_b3i_cab_load_ticks_to_40_under_power()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            handbrakes: 1,
            fuelPercent: 96f,
            oilPercent: 92f,
            loadPercent: 0f,
            motors: MotorStatus.Ok);

        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            handbrakes: 1,
            fuelPercent: 96f,
            oilPercent: 92f,
            loadPercent: 40f,
            motors: MotorStatus.Ok));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=+0.4 mass=74 load=40 fuel=96 oil=92 motors=OK mu=—",
            NextLog(
                TrainGadgetLogKind.Change,
                10f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 74f,
                fuelPercent: 96f,
                oilPercent: 92f,
                loadPercent: 40f,
                motors: MotorStatus.Ok));
    }

    [Fact]
    public void Smoke_cab_load_ticks_when_amps_bucket_changes()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            fuelPercent: 80f,
            oilPercent: 90f,
            loadPercent: 12f,
            motors: MotorStatus.Ok);

        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            fuelPercent: 80f,
            oilPercent: 90f,
            loadPercent: 40f,
            motors: MotorStatus.Ok));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=+0.4 mass=74 load=40 fuel=80 oil=90 motors=OK mu=—",
            NextLog(
                TrainGadgetLogKind.Change,
                10f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 74f,
                fuelPercent: 80f,
                oilPercent: 90f,
                loadPercent: 40f,
                motors: MotorStatus.Ok));
    }

    [Fact]
    public void Smoke_cab_motors_dead_when_tm_knife_drops()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0f,
            massTonnes: 38f,
            loadPercent: 0f,
            motors: MotorStatus.Ok);

        Assert.True(Observe(
            ref cache,
            gradePercent: 0f,
            massTonnes: 38f,
            loadPercent: 0f,
            motors: MotorStatus.Dead));
    }

    [Fact]
    public void Smoke_cab_mass_ticks_when_tonnes_change_with_handbrake_held()
    {
        var cache = default(TrainGadgetCache);
        Observe(ref cache, gradePercent: 0f, massTonnes: 74f, handbrakes: 1);

        Assert.True(Observe(ref cache, gradePercent: 0f, massTonnes: 90f, handbrakes: 1));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=0.0 mass=90 load=— fuel=— oil=— motors=— mu=—",
            NextLog(TrainGadgetLogKind.Change, 10f, ref lastAt, gradePercent: 0f, massTonnes: 90f));
    }

    [Fact]
    public void Same_grade_mass_and_handbrake_is_silent()
    {
        var cache = default(TrainGadgetCache);
        Observe(ref cache, gradePercent: 1.24f, massTonnes: 74.4f, handbrakes: 0);

        Assert.False(Observe(ref cache, gradePercent: 1.20f, massTonnes: 74.2f, handbrakes: 0));
    }

    [Fact]
    public void Same_whole_percent_load_and_fluids_is_silent()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            fuelPercent: 80.4f,
            oilPercent: 90.2f,
            loadPercent: 12.4f,
            motors: MotorStatus.Ok);

        Assert.False(Observe(
            ref cache,
            fuelPercent: 80.1f,
            oilPercent: 90.4f,
            loadPercent: 12.2f,
            motors: MotorStatus.Ok));
    }

    [Fact]
    public void Handbrake_count_change_still_publishes()
    {
        var cache = default(TrainGadgetCache);
        Observe(ref cache, gradePercent: 0f, massTonnes: 74f, handbrakes: 0);

        Assert.True(Observe(ref cache, gradePercent: 0f, massTonnes: 74f, handbrakes: 1));
    }

    [Fact]
    public void Grade_wobble_inside_display_bucket_is_silent()
    {
        var cache = default(TrainGadgetCache);
        Observe(ref cache, gradePercent: 0.01f, massTonnes: 74f, handbrakes: 0);

        Assert.False(Observe(ref cache, gradePercent: 0.04f, massTonnes: 74f, handbrakes: 0));
    }

    [Fact]
    public void First_unknown_is_silent()
    {
        var cache = default(TrainGadgetCache);

        Assert.False(Observe(
            ref cache,
            known: false,
            gradePercent: null,
            massTonnes: null,
            handbrakes: null));
    }

    [Fact]
    public void Unknown_after_known_emits_T2_gadgets_hide()
    {
        var cache = default(TrainGadgetCache);
        Observe(ref cache, gradePercent: 0f, massTonnes: 74f, handbrakes: 0);

        Assert.True(Observe(
            ref cache,
            known: false,
            gradePercent: null,
            massTonnes: null,
            handbrakes: null));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets hide",
            NextLog(TrainGadgetLogKind.Hide, 0f, ref lastAt, gradePercent: null, massTonnes: null));
    }

    [Fact]
    public void Change_log_is_throttled_like_heading()
    {
        var lastAt = -TrainGadgetTelemetry.MinChangeLogSeconds;
        var first = NextLog(
            TrainGadgetLogKind.Change,
            1f,
            ref lastAt,
            gradePercent: 1.24f,
            massTonnes: 74f);
        Assert.Equal("T2 gadgets change: grade=+1.2 mass=74 load=— fuel=— oil=— motors=— mu=—", first);

        var suppressed = NextLog(
            TrainGadgetLogKind.Change,
            2f,
            ref lastAt,
            gradePercent: 2.0f,
            massTonnes: 74f);
        Assert.Null(suppressed);
    }

    [Fact]
    public void Observe_does_not_allocate_when_buckets_hold()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 74f,
            fuelPercent: 80f,
            oilPercent: 90f,
            loadPercent: 12f,
            motors: MotorStatus.Ok);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            Observe(
                ref cache,
                gradePercent: 0.41f,
                massTonnes: 74.2f,
                fuelPercent: 80.4f,
                oilPercent: 90.2f,
                loadPercent: 12.4f,
                motors: MotorStatus.Ok);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Smoke_two_de2s_synced_emits_T2_gadgets_init_mu_dash()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            motors: MotorStatus.Ok,
            mu: FreeMotionSeverity.None));
        Assert.Equal(
            "T2 gadgets init: grade=+0.4 mass=76 load=— fuel=— oil=— motors=OK mu=—",
            NextLog(
                TrainGadgetLogKind.Init,
                0f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 76f,
                motors: MotorStatus.Ok,
                mu: FreeMotionSeverity.None));
    }

    [Fact]
    public void Smoke_trailing_neutral_emits_T2_gadgets_change_mu_idle()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            motors: MotorStatus.Ok,
            mu: FreeMotionSeverity.None);

        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            motors: MotorStatus.Ok,
            mu: FreeMotionSeverity.Yellow));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=+0.4 mass=76 load=— fuel=— oil=— motors=OK mu=idle",
            NextLog(
                TrainGadgetLogKind.Change,
                10f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 76f,
                motors: MotorStatus.Ok,
                mu: FreeMotionSeverity.Yellow));
    }

    [Fact]
    public void Smoke_brake_fight_emits_T2_gadgets_change_mu_desync()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            motors: MotorStatus.Ok,
            mu: FreeMotionSeverity.Yellow);

        Assert.True(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            motors: MotorStatus.Ok,
            mu: FreeMotionSeverity.Red));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=+0.4 mass=76 load=— fuel=— oil=— motors=OK mu=desync",
            NextLog(
                TrainGadgetLogKind.Change,
                10f,
                ref lastAt,
                gradePercent: 0.4f,
                massTonnes: 76f,
                motors: MotorStatus.Ok,
                mu: FreeMotionSeverity.Red));
    }

    [Fact]
    public void Same_mu_severity_is_silent()
    {
        var cache = default(TrainGadgetCache);
        Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            mu: FreeMotionSeverity.Yellow);

        Assert.False(Observe(
            ref cache,
            gradePercent: 0.4f,
            massTonnes: 76f,
            mu: FreeMotionSeverity.Yellow));
    }

    private static bool Observe(
        ref TrainGadgetCache cache,
        bool known = true,
        float? gradePercent = 0f,
        float? massTonnes = 74f,
        int? handbrakes = 0,
        float? fuelPercent = null,
        float? oilPercent = null,
        float? loadPercent = null,
        MotorStatus? motors = null,
        FreeMotionSeverity mu = FreeMotionSeverity.None) =>
        TrainGadgetTelemetry.Observe(
            known,
            gradePercent,
            massTonnes,
            handbrakes,
            fuelPercent,
            oilPercent,
            loadPercent,
            motors,
            mu,
            ref cache);

    private static string? NextLog(
        TrainGadgetLogKind kind,
        float nowSeconds,
        ref float lastChangeLogAt,
        float? gradePercent,
        float? massTonnes,
        float? fuelPercent = null,
        float? oilPercent = null,
        float? loadPercent = null,
        MotorStatus? motors = null,
        FreeMotionSeverity mu = FreeMotionSeverity.None) =>
        TrainGadgetTelemetry.NextLog(
            gradePercent,
            massTonnes,
            fuelPercent,
            oilPercent,
            loadPercent,
            motors,
            mu,
            kind,
            nowSeconds,
            ref lastChangeLogAt);
}

public class GradeDisplayTests
{
    [Fact]
    public void PercentFromDirection_level_track_is_zero()
    {
        Assert.Equal(0f, GradeDisplay.PercentFromDirection(1f, 0f, 0f), precision: 2);
    }

    [Fact]
    public void PercentFromDirection_one_percent_climb()
    {
        Assert.Equal(1f, GradeDisplay.PercentFromDirection(100f, 1f, 0f), precision: 2);
    }

    [Fact]
    public void PercentFromDirection_descent_is_negative()
    {
        Assert.Equal(-2f, GradeDisplay.PercentFromDirection(50f, -1f, 0f), precision: 2);
    }

    [Fact]
    public void FormatPercent_shows_sign_and_placeholder()
    {
        Assert.Equal("— Grade", GradeDisplay.FormatPercent(null));
        Assert.Equal("Grade +1.2 %", GradeDisplay.FormatPercent(1.24f));
        Assert.Equal("Grade -0.5 %", GradeDisplay.FormatPercent(-0.54f));
        Assert.Equal("Grade 0.0 %", GradeDisplay.FormatPercent(0.01f));
    }

    [Fact]
    public void Smoke_sw_b3i_cab_held_matches_grade_plus_04_mass_74()
    {
        Assert.Equal("Grade +0.4 %", GradeDisplay.FormatPercent(0.4f));
        Assert.Equal("Mass 74 t", TonnageDisplay.FormatTonnes(74f));
    }

    [Fact]
    public void Smoke_solo_de2_drive_matches_grade_minus_16_mass_38()
    {
        Assert.Equal("Grade -1.6 %", GradeDisplay.FormatPercent(-1.6f));
        Assert.Equal("Mass 38 t", TonnageDisplay.FormatTonnes(38f));
    }

    [Fact]
    public void Smoke_look_at_full_tank_loco_does_not_use_last_locos_empty_oil()
    {
        const int fullTankLookAt = 2;
        const int emptyLastBoarded = 1;
        Assert.Equal(
            fullTankLookAt,
            GadgetLocoSelection.ResolveInstanceId(fullTankLookAt, emptyLastBoarded));
        Assert.Equal(
            emptyLastBoarded,
            GadgetLocoSelection.ResolveInstanceId(usableLocoId: 0, lastLocoId: emptyLastBoarded));
    }
}
