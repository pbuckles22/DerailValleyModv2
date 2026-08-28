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
    public void Smoke_loading_screen_hides_hud_before_world_stream_complete()
    {
        Assert.False(HudWorldSession.IsActive(playerTransformPresent: true, worldReady: false));
        Assert.False(HudShell.ShouldDraw(playerTransformPresent: true, worldReady: false));
        Assert.True(HudWorldSession.IsActive(playerTransformPresent: true, worldReady: true));
    }

    [Fact]
    public void Smoke_pause_menu_hides_on_screen_hud_while_world_session_active()
    {
        Assert.True(HudShell.ShouldDrawOnScreen(
            playerTransformPresent: true,
            worldReady: true,
            hideForOverlay: false));
        Assert.False(HudShell.ShouldDrawOnScreen(
            playerTransformPresent: true,
            worldReady: true,
            hideForOverlay: true));
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
    public void Smoke_home_mark_shows_marked_here_on_always_on()
    {
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 2,
            marked: ParkMarkDisplay.FormatReturn(10.2f, 20.4f, 10.4f, 20.1f),
            clock: ClockDisplay.Format(14, 30));

        Assert.Equal(
            "Heading NE"
            + MonitorHudLine.Separator
            + "Marked here"
            + MonitorHudLine.Separator
            + "Clock 14:30",
            sb.ToString());
        Assert.DoesNotContain("Station", sb.ToString());
    }

    [Fact]
    public void Smoke_unmarked_omits_marked_from_always_on()
    {
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 0,
            marked: ParkMarkDisplay.FormatReturn(null, null, 0f, 0f),
            clock: ClockDisplay.Format(9, 5));

        Assert.Equal("Heading N" + MonitorHudLine.Separator + "Clock 09:05", sb.ToString());
        Assert.DoesNotContain("Marked", sb.ToString());
    }

    [Fact]
    public void Smoke_end_dest_same_track_shows_path_ok()
    {
        var path = PathCheckDisplay.Format(PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            "SM-O6I",
            "SM-O6I"));
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 4,
            path: path,
            clock: ClockDisplay.Format(11, 57));

        Assert.Equal(
            "Heading E"
            + MonitorHudLine.Separator
            + "Path OK"
            + MonitorHudLine.Separator
            + "Clock 11:57",
            sb.ToString());
    }

    [Fact]
    public void Smoke_no_dest_omits_path_from_always_on()
    {
        var path = PathCheckDisplay.Format(PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            "SM-O6I",
            null));
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(sb, headingIndex: 0, path: path, clock: ClockDisplay.Format(9, 5));

        Assert.Equal("Heading N" + MonitorHudLine.Separator + "Clock 09:05", sb.ToString());
        Assert.DoesNotContain("Path", sb.ToString());
    }

    [Fact]
    public void Smoke_in_zone_shows_station_bearing_on_always_on()
    {
        var station = StationWaypointDisplay.Format(
            inZone: true,
            yardId: "SM",
            stationX: 10f,
            stationZ: 20f,
            playerX: 110f,
            playerZ: 20f,
            atOffice: false);
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 2,
            station: station,
            clock: ClockDisplay.Format(14, 30));

        Assert.Equal(
            "Heading NE"
            + MonitorHudLine.Separator
            + "Station SM W 100m"
            + MonitorHudLine.Separator
            + "Clock 14:30",
            sb.ToString());
    }

    [Fact]
    public void Smoke_office_apron_shows_station_here()
    {
        var station = StationWaypointDisplay.Format(
            inZone: true,
            yardId: "HB",
            stationX: 50f,
            stationZ: 60f,
            playerX: 58f,
            playerZ: 60f,
            atOffice: true);
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 0,
            station: station,
            clock: ClockDisplay.Format(11, 57));

        Assert.Equal(
            "Heading N"
            + MonitorHudLine.Separator
            + "Station HB here"
            + MonitorHudLine.Separator
            + "Clock 11:57",
            sb.ToString());
    }

    [Fact]
    public void Smoke_outside_zone_omits_station_from_always_on()
    {
        var station = StationWaypointDisplay.Format(
            inZone: false,
            yardId: "SM",
            stationX: 10f,
            stationZ: 20f,
            playerX: 0f,
            playerZ: 0f,
            atOffice: false);
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 0,
            station: station,
            clock: ClockDisplay.Format(9, 5));

        Assert.Equal("Heading N" + MonitorHudLine.Separator + "Clock 09:05", sb.ToString());
        Assert.DoesNotContain("Station", sb.ToString());
    }

    [Fact]
    public void Smoke_cab_drive_shows_station_cp_ssw_640m_on_always_on()
    {
        OffsetStation(640f, 202.5, out var stationX, out var stationZ);
        var station = StationWaypointDisplay.Format(
            inZone: true,
            yardId: "CP",
            stationX: stationX,
            stationZ: stationZ,
            playerX: 0f,
            playerZ: 0f,
            atOffice: false);
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 8,
            station: station,
            clock: ClockDisplay.Format(22, 54));

        Assert.Equal(
            "Heading S"
            + MonitorHudLine.Separator
            + "Station CP SSW 640m"
            + MonitorHudLine.Separator
            + "Clock 22:54",
            sb.ToString());
    }

    [Fact]
    public void Smoke_look_away_keeps_station_and_path_on_always_on()
    {
        OffsetStation(41f, 337.5, out var stationX, out var stationZ);
        OffsetStation(28f, 337.5, out var markX, out var markZ);
        var marked = ParkMarkDisplay.FormatReturn(markX, markZ, 0f, 0f);
        var station = StationWaypointDisplay.Format(
            inZone: true,
            yardId: "CP",
            stationX: stationX,
            stationZ: stationZ,
            playerX: 0f,
            playerZ: 0f,
            atOffice: false);
        var path = PathCheckDisplay.Format(PathCheck.Evaluate(
            Array.Empty<PathEdge>(),
            new Dictionary<string, int>(),
            "CP-A1",
            "CP-A1"));
        var sb = new StringBuilder();
        HudShell.AppendAlwaysOn(
            sb,
            headingIndex: 6,
            marked: marked,
            station: station,
            path: path,
            clock: ClockDisplay.Format(0, 7));

        Assert.Equal(
            "Heading SE"
            + MonitorHudLine.Separator
            + "Marked NNW 28m"
            + MonitorHudLine.Separator
            + "Station CP NNW 41m"
            + MonitorHudLine.Separator
            + "Path OK"
            + MonitorHudLine.Separator
            + "Clock 00:07",
            sb.ToString());
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
    public void Smoke_limit_gov_flash_wraps_throttle_chip_red()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 54f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 40 km/h",
            limitLabel: "Limit 40",
            carCount: 1,
            massTonnes: 38f,
            flashThrottle: true,
            flashIndy: true,
            flashTrain: false,
            flashLit: true);

        var line = sb.ToString();
        Assert.Contains($"<color={DerailRiskDisplay.CriticalColor}>Throttle 54 %</color>", line);
        Assert.Contains($"<color={DerailRiskDisplay.CriticalColor}>Indy 0 %</color>", line);
        Assert.Contains("TrainBrake 0 %", line);
        Assert.DoesNotContain($"<color={DerailRiskDisplay.CriticalColor}>TrainBrake", line);
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
        Assert.DoesNotContain("Derail Risk", line);
        Assert.DoesNotContain("Stress", line);
    }

    [Fact]
    public void Smoke_cab_shows_green_derail_risk_when_safe()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 20 km/h",
            limitLabel: "Limit 40",
            carCount: 1,
            massTonnes: 38f,
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            derailRisk: DerailRiskDisplay.FormatHud(0f));

        var line = sb.ToString();
        Assert.Contains("Motors OK", line);
        Assert.Contains("Derail Risk 0 %", line);
        Assert.Contains(DerailRiskDisplay.OkColor, line);
        Assert.True(line.IndexOf("Motors OK", StringComparison.Ordinal) < line.IndexOf("Derail Risk 0 %", StringComparison.Ordinal));
        Assert.DoesNotContain("Stress", line);
    }

    [Fact]
    public void Smoke_cab_shows_derail_risk_after_motors()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: "Speed 40 km/h",
            limitLabel: "Limit 40",
            carCount: 1,
            massTonnes: 38f,
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            derailRisk: DerailRiskDisplay.FormatHud(22f));

        var line = sb.ToString();
        Assert.Contains("Motors OK", line);
        Assert.Contains("Derail Risk 22 %", line);
        Assert.Contains(DerailRiskDisplay.WarningColor, line);
        Assert.True(line.IndexOf("Motors OK", StringComparison.Ordinal) < line.IndexOf("Derail Risk 22 %", StringComparison.Ordinal));
        Assert.DoesNotContain("Stress", line);
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
    public void Smoke_mf_t13p_cab_held_speed_0_limit_120()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 0.5f,
            throttlePct: 0f,
            indyPct: 0f,
            trainBrakePct: 100f,
            speedLabel: SpeedDisplay.FormatOrEmpty(0),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(0f, 120f),
            carCount: 2,
            massTonnes: 76f,
            fuel: FluidDisplay.FormatFuelHud(74f, 0f),
            oil: FluidDisplay.FormatOilHud(74f, 0f),
            grade: GradeDisplay.FormatPercent(-0.2f),
            load: LoadDisplay.FormatHud(0f),
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            handbrakes: HandbrakeDisplay.FormatTotal(0));

        var line = sb.ToString();
        Assert.Contains("Speed 0 km/h", line);
        Assert.Contains("Limit 120", line);
        Assert.Contains("TrainBrake 100 %", line);
        Assert.Contains("Throttle 0 %", line);
        Assert.DoesNotContain("— Speed", line);
        Assert.DoesNotContain("— Limit", line);
        Assert.DoesNotContain("Next", line);
        Assert.True(line.IndexOf("TrainBrake", StringComparison.Ordinal) < line.IndexOf("Speed 0", StringComparison.Ordinal));
        Assert.True(line.IndexOf("Speed 0", StringComparison.Ordinal) < line.IndexOf("Limit 120", StringComparison.Ordinal));
    }

    [Fact]
    public void Smoke_cab_roll_speed_5_limit_120_load_35()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 18f,
            indyPct: 43f,
            trainBrakePct: 36f,
            speedLabel: SpeedDisplay.FormatOrEmpty(5),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(5f, 120f),
            carCount: 2,
            massTonnes: 76f,
            load: LoadDisplay.FormatHud(35f),
            motors: MotorDisplay.FormatHud(MotorStatus.Ok),
            freeMotion: ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));

        var line = sb.ToString();
        Assert.Contains("Throttle 18 %", line);
        Assert.Contains("Indy 43 %", line);
        Assert.Contains("TrainBrake 36 %", line);
        Assert.Contains("Speed 5 km/h", line);
        Assert.Contains("Limit 120", line);
        Assert.Contains("Load 35 %", line);
        Assert.Contains("MU idle", line);
        Assert.DoesNotContain("Next", line);
        Assert.True(line.IndexOf("TrainBrake", StringComparison.Ordinal) < line.IndexOf("Speed 5", StringComparison.Ordinal));
        Assert.True(line.IndexOf("Speed 5", StringComparison.Ordinal) < line.IndexOf("Limit 120", StringComparison.Ordinal));
    }

    [Fact]
    public void Smoke_cab_drive_speed_20_limit_40_between_levers_and_motors()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: SpeedDisplay.FormatOrEmpty(20),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(20f, 40f),
            carCount: 1,
            massTonnes: 38f,
            load: LoadDisplay.FormatHud(25f),
            motors: MotorDisplay.FormatHud(MotorStatus.Ok));

        var line = sb.ToString();
        Assert.Contains("Throttle 40 %", line);
        Assert.Contains("TrainBrake 0 %", line);
        Assert.Contains("Speed 20 km/h", line);
        Assert.Contains("Limit 40", line);
        Assert.Contains("Motors OK", line);
        Assert.DoesNotContain("— Speed", line);
        Assert.DoesNotContain("— Limit", line);
        Assert.DoesNotContain("Next", line);
        Assert.True(line.IndexOf("TrainBrake", StringComparison.Ordinal) < line.IndexOf("Speed 20", StringComparison.Ordinal));
        Assert.True(line.IndexOf("Speed 20", StringComparison.Ordinal) < line.IndexOf("Limit 40", StringComparison.Ordinal));
        Assert.True(line.IndexOf("Limit 40", StringComparison.Ordinal) < line.IndexOf("Motors OK", StringComparison.Ordinal));
    }

    [Fact]
    public void Smoke_cab_drive_limit_80_shows_next_50()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 40f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: SpeedDisplay.FormatOrEmpty(40),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(40f, 80f, 50f, 800f, 38f),
            carCount: 1,
            massTonnes: 38f,
            load: LoadDisplay.FormatHud(25f),
            motors: MotorDisplay.FormatHud(MotorStatus.Ok));

        var line = sb.ToString();
        Assert.Contains("Limit 80 | Next 50", line);
        Assert.DoesNotContain("(800m)", line);
        Assert.DoesNotContain("— Limit", line);
    }

    [Fact]
    public void Smoke_look_at_usable_loco_shows_levers_speed_and_limit()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 1f,
            throttlePct: 0f,
            indyPct: 100f,
            trainBrakePct: 100f,
            speedLabel: SpeedDisplay.FormatOrEmpty(0),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(0f, 60f),
            carCount: 2,
            massTonnes: 76f);

        var line = sb.ToString();
        Assert.Contains("Throttle 0 %", line);
        Assert.Contains("Indy 100 %", line);
        Assert.Contains("TrainBrake 100 %", line);
        Assert.Contains("Speed 0 km/h", line);
        Assert.Contains("Limit 60", line);
    }

    [Fact]
    public void Smoke_reverse_shunt_shows_rear_chip_after_cars()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 0f,
            throttlePct: 0f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: SpeedDisplay.FormatOrEmpty(4),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(4f, 40f),
            carCount: 8,
            massTonnes: 320f,
            backup: BackupProximityDisplay.FormatHud(0.4f, inCoupleRange: true, tipActive: true, label: "Rear"));

        var line = sb.ToString();
        Assert.Contains("Rear 0.4m", line);
        Assert.Contains(BackupProximityDisplay.NearColor, line);
        Assert.DoesNotContain("Couple ready", line);
        Assert.DoesNotContain("Front", line);
        Assert.True(line.IndexOf("Cars", StringComparison.Ordinal) < line.IndexOf("Rear 0.4m", StringComparison.Ordinal));
    }

    [Fact]
    public void Smoke_neutral_omits_proximity_chip()
    {
        var sb = new StringBuilder();
        HudShell.AppendLocoStopState(
            sb,
            reverser01: 0.5f,
            throttlePct: 0f,
            indyPct: 0f,
            trainBrakePct: 0f,
            speedLabel: SpeedDisplay.FormatOrEmpty(0),
            limitLabel: SpeedLimitDisplay.FormatHudOrEmpty(0f, 40f),
            carCount: 1,
            massTonnes: 38f,
            backup: string.Empty);

        var line = sb.ToString();
        Assert.DoesNotContain("Rear", line);
        Assert.DoesNotContain("Front", line);
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

    private static void OffsetStation(float meters, double degrees, out float x, out float z)
    {
        var rad = degrees * Math.PI / 180.0;
        x = (float)(meters * Math.Sin(rad));
        z = (float)(meters * Math.Cos(rad));
    }
}
