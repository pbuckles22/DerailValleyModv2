using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: cab Mass + Grade (story 6.5). Publish on display-bucket
/// change only — not every 10 Hz tick, and not only when handbrakes change.
/// </summary>
public class TrainGadgetTelemetryTests
{
    [Fact]
    public void Smoke_cab_grade_ticks_when_slope_changes_with_handbrake_held()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0f,
            massTonnes: 74f,
            handbrakes: 2,
            ref cache));
        Assert.Equal(
            "T2 gadgets init: grade=0.0 mass=74",
            TrainGadgetTelemetry.NextLog(
                gradePercent: 0f,
                massTonnes: 74f,
                TrainGadgetLogKind.Init,
                nowSeconds: 0f,
                lastChangeLogAt: ref lastAt));

        Assert.True(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 1.24f,
            massTonnes: 74f,
            handbrakes: 2,
            ref cache));
        lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=+1.2 mass=74",
            TrainGadgetTelemetry.NextLog(
                gradePercent: 1.24f,
                massTonnes: 74f,
                TrainGadgetLogKind.Change,
                nowSeconds: 10f,
                lastChangeLogAt: ref lastAt));
    }

    [Fact]
    public void Smoke_sw_b3i_cab_held_emits_T2_gadgets_init_grade_plus_04_mass_74()
    {
        var cache = default(TrainGadgetCache);
        var lastAt = 0f;
        Assert.True(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0.4f,
            massTonnes: 74f,
            handbrakes: 1,
            ref cache));
        Assert.Equal(
            "T2 gadgets init: grade=+0.4 mass=74",
            TrainGadgetTelemetry.NextLog(
                gradePercent: 0.4f,
                massTonnes: 74f,
                TrainGadgetLogKind.Init,
                nowSeconds: 0f,
                lastChangeLogAt: ref lastAt));
    }

    [Fact]
    public void Smoke_solo_de2_drive_emits_T2_gadgets_change_grade_minus_16()
    {
        var cache = default(TrainGadgetCache);
        TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0.4f,
            massTonnes: 38f,
            handbrakes: 0,
            ref cache);

        Assert.True(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: -1.6f,
            massTonnes: 38f,
            handbrakes: 0,
            ref cache));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=-1.6 mass=38",
            TrainGadgetTelemetry.NextLog(
                gradePercent: -1.6f,
                massTonnes: 38f,
                TrainGadgetLogKind.Change,
                nowSeconds: 10f,
                lastChangeLogAt: ref lastAt));
    }

    [Fact]
    public void Smoke_cab_mass_ticks_when_tonnes_change_with_handbrake_held()
    {
        var cache = default(TrainGadgetCache);
        TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0f,
            massTonnes: 74f,
            handbrakes: 1,
            ref cache);

        Assert.True(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0f,
            massTonnes: 90f,
            handbrakes: 1,
            ref cache));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets change: grade=0.0 mass=90",
            TrainGadgetTelemetry.NextLog(
                gradePercent: 0f,
                massTonnes: 90f,
                TrainGadgetLogKind.Change,
                nowSeconds: 10f,
                lastChangeLogAt: ref lastAt));
    }

    [Fact]
    public void Same_grade_mass_and_handbrake_is_silent()
    {
        var cache = default(TrainGadgetCache);
        TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 1.24f,
            massTonnes: 74.4f,
            handbrakes: 0,
            ref cache);

        Assert.False(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 1.20f,
            massTonnes: 74.2f,
            handbrakes: 0,
            ref cache));
    }

    [Fact]
    public void Handbrake_count_change_still_publishes()
    {
        var cache = default(TrainGadgetCache);
        TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0f,
            massTonnes: 74f,
            handbrakes: 0,
            ref cache);

        Assert.True(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0f,
            massTonnes: 74f,
            handbrakes: 1,
            ref cache));
    }

    [Fact]
    public void Grade_wobble_inside_display_bucket_is_silent()
    {
        var cache = default(TrainGadgetCache);
        TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0.01f,
            massTonnes: 74f,
            handbrakes: 0,
            ref cache);

        Assert.False(TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0.04f,
            massTonnes: 74f,
            handbrakes: 0,
            ref cache));
    }

    [Fact]
    public void First_unknown_is_silent()
    {
        var cache = default(TrainGadgetCache);

        Assert.False(TrainGadgetTelemetry.Observe(
            known: false,
            gradePercent: null,
            massTonnes: null,
            handbrakes: null,
            ref cache));
    }

    [Fact]
    public void Unknown_after_known_emits_T2_gadgets_hide()
    {
        var cache = default(TrainGadgetCache);
        TrainGadgetTelemetry.Observe(
            known: true,
            gradePercent: 0f,
            massTonnes: 74f,
            handbrakes: 0,
            ref cache);

        Assert.True(TrainGadgetTelemetry.Observe(
            known: false,
            gradePercent: null,
            massTonnes: null,
            handbrakes: null,
            ref cache));
        var lastAt = 0f;
        Assert.Equal(
            "T2 gadgets hide",
            TrainGadgetTelemetry.NextLog(
                gradePercent: null,
                massTonnes: null,
                TrainGadgetLogKind.Hide,
                nowSeconds: 0f,
                lastChangeLogAt: ref lastAt));
    }

    [Fact]
    public void Change_log_is_throttled_like_heading()
    {
        var lastAt = -TrainGadgetTelemetry.MinChangeLogSeconds;
        var first = TrainGadgetTelemetry.NextLog(
            gradePercent: 1.24f,
            massTonnes: 74f,
            TrainGadgetLogKind.Change,
            nowSeconds: 1f,
            lastChangeLogAt: ref lastAt);
        Assert.Equal("T2 gadgets change: grade=+1.2 mass=74", first);

        var suppressed = TrainGadgetTelemetry.NextLog(
            gradePercent: 2.0f,
            massTonnes: 74f,
            TrainGadgetLogKind.Change,
            nowSeconds: 2f,
            lastChangeLogAt: ref lastAt);
        Assert.Null(suppressed);
    }
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
}
