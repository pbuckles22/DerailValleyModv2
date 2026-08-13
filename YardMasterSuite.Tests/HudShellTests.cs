using System.Text;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: in-world top bar + compass; launcher hides HUD (story 3.1).
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
        HudShell.AppendCompass(sb, pointIndex: 2);

        Assert.Equal("Heading NE", sb.ToString());
    }

    [Fact]
    public void Top_bar_shows_consist_cars_and_tonnes()
    {
        var sb = new StringBuilder();
        HudShell.AppendTopBar(
            sb,
            hasConsist: true, cars: 6, tonnes: 128,
            hasCab: false, thr: 0, indy: 0, train: 0, engPresent: false, eng: 0, rev: 0);

        Assert.Equal("cars=6 t=128", sb.ToString());
        Assert.True(HudShell.ShouldDrawTopBar(hasConsist: true, hasCab: false));
    }

    [Fact]
    public void Top_bar_hides_when_no_consist_and_not_boarded()
    {
        var sb = new StringBuilder();
        HudShell.AppendTopBar(
            sb,
            hasConsist: false, cars: 0, tonnes: 0,
            hasCab: false, thr: 0, indy: 0, train: 0, engPresent: false, eng: 0, rev: 0);

        Assert.Equal(0, sb.Length);
        Assert.False(HudShell.ShouldDrawTopBar(hasConsist: false, hasCab: false));
    }

    [Fact]
    public void Unboard_keeps_consist_and_drops_cab_chips()
    {
        var sb = new StringBuilder();
        HudShell.AppendTopBar(
            sb,
            hasConsist: true, cars: 6, tonnes: 128,
            hasCab: true, thr: 40, indy: 0, train: 50, engPresent: false, eng: 0, rev: 50);
        Assert.Equal("cars=6 t=128 | thr=40 indy=0 train=50 eng=na rev=50", sb.ToString());

        sb.Clear();
        HudShell.AppendTopBar(
            sb,
            hasConsist: true, cars: 6, tonnes: 128,
            hasCab: false, thr: 40, indy: 0, train: 50, engPresent: false, eng: 0, rev: 50);

        Assert.Equal("cars=6 t=128", sb.ToString());
    }

    [Fact]
    public void Top_bar_same_values_reuse_cached_string()
    {
        var cache = new GuiContentCache(slotCount: 1);
        var sb = new StringBuilder();
        HudShell.AppendTopBar(
            sb,
            hasConsist: true, cars: 1, tonnes: 38,
            hasCab: false, thr: 0, indy: 0, train: 0, engPresent: false, eng: 0, rev: 0);
        cache.TryCommit(0, sb, out var first);

        sb.Clear();
        HudShell.AppendTopBar(
            sb,
            hasConsist: true, cars: 1, tonnes: 38,
            hasCab: false, thr: 0, indy: 0, train: 0, engPresent: false, eng: 0, rev: 0);
        var changed = cache.TryCommit(0, sb, out var second);

        Assert.False(changed);
        Assert.Same(first, second);
    }
}
