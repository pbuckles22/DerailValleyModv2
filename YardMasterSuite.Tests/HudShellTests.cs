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
