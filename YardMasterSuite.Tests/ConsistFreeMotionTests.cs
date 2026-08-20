using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: two-loco MU idle / desync (**6.7**). Pure 1.15 rules.
/// </summary>
public class ConsistFreeMotionTests
{
    private static LocoControlSnapshot LeadOnForward() =>
        new(engineOn: true, reverser: 1f, throttle: 0.4f, brake: 0f);

    [Fact]
    public void Compare_matching_unit_is_none()
    {
        var lead = LeadOnForward();
        var other = lead;
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_matched_partial_brake_is_none()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.5f);
        var other = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.5f);
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Smoke_two_de2s_synced_is_quiet()
    {
        var lead = LeadOnForward();
        var other = LeadOnForward();
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
        Assert.Equal(string.Empty, ConsistFreeMotion.FormatHud(FreeMotionSeverity.None));
    }

    [Fact]
    public void Compare_off_unit_is_yellow()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: false, reverser: 0.5f, throttle: 0f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Smoke_trailing_neutral_is_mu_idle()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 0.5f, throttle: 0f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
        Assert.Equal("MU idle", ConsistFreeMotion.Format(FreeMotionSeverity.Yellow));
        Assert.Contains(ConsistFreeMotion.YellowColor, ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));
    }

    [Fact]
    public void Compare_on_but_neutral_is_yellow()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 0.5f, throttle: 0f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_from_idle_cab_looking_at_powered_other_is_yellow()
    {
        var lead = new LocoControlSnapshot(engineOn: true, reverser: 0.5f, throttle: 0f, brake: 0f);
        var other = LeadOnForward();
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_brake_mismatch_is_red_even_when_other_off()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0f, independentBrake: 0f);
        var other = new LocoControlSnapshot(false, 0.5f, 0f, brake: 0.5f, independentBrake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_independent_brake_mismatch_is_red()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0f, independentBrake: 0f);
        var other = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0f, independentBrake: 0.6f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_matched_independent_brake_is_none()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.3f, independentBrake: 0.4f);
        var other = new LocoControlSnapshot(true, 1f, 0.2f, brake: 0.3f, independentBrake: 0.4f);
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Smoke_one_handbrake_on_is_not_mu_desync()
    {
        // 6.7 smoke 2026-08-19: parking wheel is a different chip. MU red is
        // train/indy fight or both on+in-gear throttle/reverser mismatch.
        var lead = new LocoControlSnapshot(true, 1f, 0f, brake: 1f, independentBrake: 1f);
        var other = new LocoControlSnapshot(true, 1f, 0f, brake: 1f, independentBrake: 1f);
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
        Assert.Equal(string.Empty, ConsistFreeMotion.FormatHud(FreeMotionSeverity.None));
    }

    [Fact]
    public void Smoke_unplugged_indy_mismatch_is_mu_desync()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0f, brake: 1f, independentBrake: 1f);
        var other = new LocoControlSnapshot(true, 1f, 0f, brake: 1f, independentBrake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
        Assert.Equal("MU desync", ConsistFreeMotion.Format(FreeMotionSeverity.Red));
    }

    [Fact]
    public void Smoke_unplugged_throttle_mismatch_is_mu_desync()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 1f, throttle: 0.9f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
        Assert.Equal("MU desync", ConsistFreeMotion.Format(FreeMotionSeverity.Red));
        Assert.Contains(ConsistFreeMotion.RedColor, ConsistFreeMotion.FormatHud(FreeMotionSeverity.Red));
    }

    [Fact]
    public void Compare_on_in_gear_wrong_reverser_is_red()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 0f, throttle: 0.4f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_on_in_gear_throttle_mismatch_is_red()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 1f, throttle: 0.9f, brake: 0f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Compare_on_in_gear_brake_mismatch_is_red()
    {
        var lead = LeadOnForward();
        var other = new LocoControlSnapshot(engineOn: true, reverser: 1f, throttle: 0.4f, brake: 0.5f);
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.CompareUnit(lead, other));
    }

    [Fact]
    public void Aggregate_worst_wins_red_over_yellow()
    {
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.Aggregate(FreeMotionSeverity.None, FreeMotionSeverity.None));
        Assert.Equal(FreeMotionSeverity.Yellow, ConsistFreeMotion.Aggregate(FreeMotionSeverity.None, FreeMotionSeverity.Yellow));
        Assert.Equal(FreeMotionSeverity.Red, ConsistFreeMotion.Aggregate(FreeMotionSeverity.Yellow, FreeMotionSeverity.Red));
    }

    [Fact]
    public void Format_empty_when_none()
    {
        Assert.Equal(string.Empty, ConsistFreeMotion.Format(FreeMotionSeverity.None));
        Assert.Equal(string.Empty, ConsistFreeMotion.FormatHud(FreeMotionSeverity.None));
    }

    [Fact]
    public void Format_yellow_and_red_labels()
    {
        Assert.Equal("MU idle", ConsistFreeMotion.Format(FreeMotionSeverity.Yellow));
        Assert.Equal("MU desync", ConsistFreeMotion.Format(FreeMotionSeverity.Red));
        Assert.Contains(ConsistFreeMotion.YellowColor, ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));
        Assert.Contains(ConsistFreeMotion.RedColor, ConsistFreeMotion.FormatHud(FreeMotionSeverity.Red));
        Assert.Contains("MU idle", ConsistFreeMotion.FormatHud(FreeMotionSeverity.Yellow));
        Assert.Contains("MU desync", ConsistFreeMotion.FormatHud(FreeMotionSeverity.Red));
    }

    [Fact]
    public void FormatToken_idle_desync_or_dash()
    {
        Assert.Equal("—", ConsistFreeMotion.FormatToken(FreeMotionSeverity.None));
        Assert.Equal("idle", ConsistFreeMotion.FormatToken(FreeMotionSeverity.Yellow));
        Assert.Equal("desync", ConsistFreeMotion.FormatToken(FreeMotionSeverity.Red));
    }

    [Fact]
    public void ControlsMatch_allows_small_epsilon()
    {
        var lead = new LocoControlSnapshot(true, 1f, 0.40f, 0.50f);
        var other = new LocoControlSnapshot(true, 1f, 0.42f, 0.48f);
        Assert.True(ConsistFreeMotion.ControlsMatch(lead, other));
        Assert.Equal(FreeMotionSeverity.None, ConsistFreeMotion.CompareUnit(lead, other));
    }
}
