using System;
using System.Text;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: in-world HUD shell, v1 product labels, usable-train gate (**3.3.1**).
/// </summary>
public class HudShellTests
{
    [Fact]
    public void Launcher_hides_hud_when_player_transform_missing()
    {
        Assert.False(HudWorldSession.IsActive(playerTransformPresent: false));
        Assert.True(HudWorldSession.IsActive(playerTransformPresent: true));
        Assert.False(HudShell.ShouldDraw(playerTransformPresent: false));
        Assert.True(HudShell.ShouldDraw(playerTransformPresent: true));
    }

    [Fact]
    public void Compass_label_matches_heading_point()
    {
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(sb, headingIndex: 2);

        Assert.Equal("Heading NE", sb.ToString());
    }

    [Fact]
    public void Smoke_yard_always_on_shows_heading_and_world_clock()
    {
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(sb, headingIndex: 2, clock: ClockDisplay.Format(14, 30));

        Assert.Equal("Heading NE" + MonitorHudLine.Separator + "Clock 14:30", sb.ToString());
    }

    [Fact]
    public void Smoke_office_wall_clock_heading_n_and_clock_1157()
    {
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(sb, headingIndex: 0, clock: ClockDisplay.Format(11, 57));

        Assert.Equal("Heading N" + MonitorHudLine.Separator + "Clock 11:57", sb.ToString());
    }

    [Fact]
    public void Smoke_yard_always_on_omits_clock_when_world_time_unknown()
    {
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(sb, headingIndex: 0, clock: null);

        Assert.Equal("Heading N", sb.ToString());
        Assert.DoesNotContain("Clock", sb.ToString());
    }

    [Fact]
    public void Usable_train_gate_hides_loco_bar_on_foot()
    {
        Assert.False(HudShell.ShouldDrawLocoBar(hasUsableLocoTrain: false));
        Assert.True(HudShell.ShouldDrawLocoBar(hasUsableLocoTrain: true));
    }

    [Fact]
    public void Smoke_look_at_usable_train_shows_cars_and_mass_when_consist_known()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: null,
            throttlePct: null,
            indyPct: null,
            trainBrakePct: null,
            speedLabel: string.Empty,
            limitLabel: string.Empty,
            carCount: 3,
            massTonnes: 74f);

        var line = sb.ToString();
        Assert.Contains("Cars 3", line);
        Assert.Contains("Mass 74 t", line);
    }

    [Fact]
    public void Smoke_look_at_usable_train_omits_cars_and_mass_when_consist_unknown()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: null,
            throttlePct: null,
            indyPct: null,
            trainBrakePct: null,
            speedLabel: string.Empty,
            limitLabel: string.Empty,
            carCount: null,
            massTonnes: null);

        var line = sb.ToString();
        Assert.DoesNotContain("Cars 0", line);
        Assert.DoesNotContain("Mass 0", line);
    }

    [Fact]
    public void Loco_bar_uses_product_labels_not_debug_telemetry()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 35f,
            indyPct: 0f,
            trainBrakePct: 35f,
            speedLabel: "Speed 36 km/h",
            limitLabel: "Limit 40",
            carCount: 3,
            massTonnes: 128f);

        var line = sb.ToString();
        Assert.Contains("TrainBrake 35 %", line);
        Assert.Contains("Throttle 35 %", line);
        Assert.Contains("Cars 3", line);
        Assert.DoesNotContain("thr=", line);
        Assert.DoesNotContain("cars=", line);
    }

    [Fact]
    public void On_foot_empty_yard_shows_heading_only_when_not_usable()
    {
        var sb = new StringBuilder();
        HudShell.AppendTopBar(
            sb,
            hasUsable: false,
            cars: 3,
            tonnes: 128f,
            hasCab: false,
            reverser01: null,
            throttlePct: null,
            indyPct: null,
            trainBrakePct: null);

        Assert.Equal(0, sb.Length);
    }

    [Fact]
    public void Smoke_cab_shows_mass_and_grade()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 0f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 0 km/h",
            limitLabel: "Limit 40",
            carCount: 3,
            massTonnes: 74f,
            grade: GradeDisplay.FormatPercent(1.24f));

        var line = sb.ToString();
        Assert.Contains("Mass 74 t", line);
        Assert.Contains("Grade +1.2 %", line);
    }

    [Fact]
    public void Smoke_cab_shows_load_motors_fuel_oil()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 0f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 0 km/h",
            limitLabel: "Limit 40",
            carCount: 3,
            massTonnes: 74f,
            fuel: FluidDisplay.FormatFuelHud(80f, 90f),
            oil: FluidDisplay.FormatOilHud(80f, 90f),
            grade: GradeDisplay.FormatPercent(0.4f),
            load: LoadDisplay.FormatHud(12f),
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            handbrakes: HandbrakeDisplay.FormatTotal(1));

        var line = sb.ToString();
        Assert.Contains("Fuel 80 %", line);
        Assert.Contains("Oil 90 %", line);
        Assert.Contains("Load 12 %", line);
        Assert.Contains("Motors OK", line);
        Assert.Contains("Handbrakes 1", line);
        Assert.Contains("Mass 74 t", line);
        Assert.Contains("Grade +0.4 %", line);
    }

    [Fact]
    public void Smoke_sw_b3i_cab_shows_fuel_96_oil_92_load_43_motors_ok()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 9f,
            indyPct: 0f,
            trainBrakePct: 100f,
            speedLabel: "Speed 0 km/h",
            limitLabel: "Limit 120",
            carCount: 3,
            massTonnes: 74f,
            fuel: FluidDisplay.FormatFuelHud(96f, 92f),
            oil: FluidDisplay.FormatOilHud(96f, 92f),
            grade: GradeDisplay.FormatPercent(0.4f),
            load: LoadDisplay.FormatHud(43f),
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            handbrakes: HandbrakeDisplay.FormatTotal(1));

        var line = sb.ToString();
        Assert.Contains("Fuel 96 %", line);
        Assert.Contains("Oil 92 %", line);
        Assert.Contains("Load 43 %", line);
        Assert.Contains("Motors OK", line);
        Assert.Contains("Mass 74 t", line);
        Assert.Contains("Grade +0.4 %", line);
        Assert.DoesNotContain("MU idle", line);
        Assert.DoesNotContain("MU desync", line);
    }

    [Fact]
    public void Smoke_two_de2s_synced_omits_mu_chip()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 0 km/h",
            limitLabel: "Limit 40",
            carCount: 2,
            massTonnes: 76f,
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            handbrakes: HandbrakeDisplay.FormatTotal(0),
            freeMotion: ConsistFreeMotion.FormatHud(FreeMotionSeverity.None));

        var line = sb.ToString();
        Assert.Contains("Motors OK", line);
        Assert.DoesNotContain("MU idle", line);
        Assert.DoesNotContain("MU desync", line);
    }

    [Fact]
    public void Smoke_trailing_neutral_shows_mu_idle()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 0 km/h",
            limitLabel: "Limit 40",
            carCount: 2,
            massTonnes: 76f,
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            handbrakes: HandbrakeDisplay.FormatTotal(0),
            freeMotion: ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));

        var line = sb.ToString();
        Assert.Contains("MU idle", line);
        Assert.Contains(ConsistFreeMotion.YellowColor, line);
        Assert.True(line.IndexOf("Motors OK", StringComparison.Ordinal) < line.IndexOf("MU idle", StringComparison.Ordinal));
        Assert.True(line.IndexOf("MU idle", StringComparison.Ordinal) < line.IndexOf("Handbrakes", StringComparison.Ordinal));
    }

    [Fact]
    public void Smoke_unplugged_throttle_mismatch_shows_mu_desync()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 0 km/h",
            limitLabel: "Limit 40",
            carCount: 2,
            massTonnes: 76f,
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            handbrakes: HandbrakeDisplay.FormatTotal(0),
            freeMotion: ConsistFreeMotion.FormatHud(FreeMotionSeverity.Red));

        var line = sb.ToString();
        Assert.Contains("MU desync", line);
        Assert.Contains(ConsistFreeMotion.RedColor, line);
        Assert.True(line.IndexOf("Motors OK", StringComparison.Ordinal) < line.IndexOf("MU desync", StringComparison.Ordinal));
        Assert.True(line.IndexOf("MU desync", StringComparison.Ordinal) < line.IndexOf("Handbrakes", StringComparison.Ordinal));
    }

    [Fact]
    public void Cab_shows_levers_speed_limit_cars()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 30f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 36 km/h",
            limitLabel: "Limit 40",
            carCount: 2,
            massTonnes: 80f);

        var line = sb.ToString();
        Assert.Contains("Speed 36 km/h", line);
        Assert.Contains("Limit 40", line);
        Assert.Contains(MonitorHudLine.Separator, line);
    }

    [Fact]
    public void Top_bar_same_values_reuse_cached_string()
    {
        var cache = new GuiContentCache(slotCount: 1);
        var sb = new StringBuilder();
        HudShell.AppendTopBar(
            sb,
            hasUsable: true,
            cars: 1,
            tonnes: 38f,
            hasCab: false,
            reverser01: null,
            throttlePct: null,
            indyPct: null,
            trainBrakePct: null);
        cache.TryCommit(0, sb, out var first);

        sb.Clear();
        HudShell.AppendTopBar(
            sb,
            hasUsable: true,
            cars: 1,
            tonnes: 38f,
            hasCab: false,
            reverser01: null,
            throttlePct: null,
            indyPct: null,
            trainBrakePct: null);
        var changed = cache.TryCommit(0, sb, out var second);

        Assert.False(changed);
        Assert.Same(first, second);
    }
}
