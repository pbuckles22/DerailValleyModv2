using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class LocoRerailPolicyTests
{
    [Fact]
    public void EvaluateTurn_ok_solo_stationary_on_rails()
    {
        var abort = LocoRerailPolicy.EvaluateTurn(
            hasLoco: true,
            consistCarCount: 1,
            maxAbsSpeedKmh: 0f,
            isTeleporting: false,
            isDerailed: false);
        Assert.Equal(LocoRerailAbort.None, abort);
        Assert.True(LocoRerailPolicy.CanApply(abort));
    }

    [Fact]
    public void Smoke_Turn_refuses_coupled_consist()
    {
        Assert.Equal(
            LocoRerailAbort.Coupled,
            LocoRerailPolicy.EvaluateTurn(true, 3, 0f, false, false));
    }

    [Fact]
    public void EvaluateTurn_blocks_coupled_moving_derailed()
    {
        Assert.Equal(
            LocoRerailAbort.NoLoco,
            LocoRerailPolicy.EvaluateTurn(false, 1, 0f, false, false));
        Assert.Equal(
            LocoRerailAbort.Coupled,
            LocoRerailPolicy.EvaluateTurn(true, 3, 0f, false, false));
        Assert.Equal(
            LocoRerailAbort.Derailed,
            LocoRerailPolicy.EvaluateTurn(true, 1, 0f, false, true));
        Assert.Equal(
            LocoRerailAbort.Moving,
            LocoRerailPolicy.EvaluateTurn(true, 1, 5f, false, false));
        Assert.Equal(
            LocoRerailAbort.BusyTeleporting,
            LocoRerailPolicy.EvaluateTurn(true, 1, 0f, true, false));
    }

    [Fact]
    public void EvaluatePlace_ok_and_blocks_derailed()
    {
        Assert.Equal(
            LocoRerailAbort.None,
            LocoRerailPolicy.EvaluatePlace(true, 1, 0f, false, true, false, false));
        Assert.Equal(
            LocoRerailAbort.Derailed,
            LocoRerailPolicy.EvaluatePlace(true, 1, 0f, false, true, false, true));
        Assert.Equal(
            LocoRerailAbort.NoMatch,
            LocoRerailPolicy.EvaluatePlace(true, 0, 0f, false, true, false, false));
        Assert.Equal(
            LocoRerailAbort.NoTarget,
            LocoRerailPolicy.EvaluatePlace(true, 1, 0f, false, false, false, false));
    }

    [Fact]
    public void FormatPlaceChip_smoke_shape()
    {
        Assert.Equal(
            "PLACE OK · DH4 · SW-C1O",
            LocoRerailPolicy.FormatPlaceChip(true, "DH4", "SW-C1O", LocoRerailAbort.None));
        Assert.Equal(
            "PLACE BLOCKED · look at a track",
            LocoRerailPolicy.FormatPlaceChip(true, "DH4", null, LocoRerailAbort.NoTarget));
        Assert.Equal("", LocoRerailPolicy.FormatPlaceChip(false, "DH4", "X", LocoRerailAbort.None));
    }

    [Fact]
    public void SelectSource_prefers_on_rails_outside_then_farthest()
    {
        // 0 derailed outside far, 1 on-rails same-yard, 2 on-rails outside far, 3 coupled
        var idx = LocoRerailPolicy.SelectSourceIndex(
            4,
            i => true,
            i => i == 3,
            i => i == 0,
            i => i == 1,
            i => i switch { 0 => 900f, 1 => 10f, 2 => 800f, _ => 1f });
        Assert.Equal(2, idx);
    }

    [Fact]
    public void SelectSource_returns_minus_one_when_only_derailed()
    {
        var idx = LocoRerailPolicy.SelectSourceIndex(
            2,
            _ => true,
            _ => false,
            _ => true,
            _ => false,
            i => 100f + i);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void Smoke_game_teleport_rejects_derailed_maps_to_Derailed_abort()
    {
        // Harvest: Player.log "carsToTeleport … one of the cars is derailed! Aborting fast travel"
        Assert.Equal(
            "need on-rails loco",
            LocoRerailPolicy.FormatAbort(LocoRerailAbort.Derailed));
    }
}

public class LocoTypeIdTests
{
    [Theory]
    [InlineData("LocoDH4", "DH4")]
    [InlineData("DH4", "DH4")]
    [InlineData("Loco DE6", "DE6")]
    [InlineData("locoS060", "S060")]
    public void Normalize_strips_loco_prefix(string raw, string expected)
    {
        Assert.Equal(expected, LocoTypeId.Normalize(raw));
    }

    [Fact]
    public void IsDe2_accepts_loco_prefix()
    {
        Assert.True(LocoTypeId.IsDe2("DE2"));
        Assert.True(LocoTypeId.IsDe2("LocoDE2"));
        Assert.True(LocoTypeId.IsDe2("Loco DE2"));
        Assert.False(LocoTypeId.IsDe2("DH4"));
        Assert.False(LocoTypeId.IsDe2(null));
    }

    [Fact]
    public void Matches_across_prefixes()
    {
        Assert.True(LocoTypeId.Matches("LocoDH4", "DH4"));
        Assert.True(LocoTypeId.Matches("DH4", "LocoDH4"));
        Assert.False(LocoTypeId.Matches("DE6", "DH4"));
    }
}

[Collection("StaticSessions")]
public class LocoRerailSessionTests
{
    public LocoRerailSessionTests()
    {
        LocoRerailSession.Clear();
    }

    [Fact]
    public void Begin_SetTarget_Toggle_Clear()
    {
        LocoRerailSession.Begin("LocoDH4");
        Assert.True(LocoRerailSession.IsActive);
        Assert.Equal("DH4", LocoRerailSession.TypeId);
        Assert.True(LocoRerailSession.ForceRegularDirection);

        LocoRerailSession.SetTarget("SW-C1O", 1f, 2f, 3f);
        Assert.Equal("SW-C1O", LocoRerailSession.TargetTrackId);
        Assert.True(LocoRerailSession.TryGetAimPoint(out var x, out var y, out var z));
        Assert.Equal(1f, x);
        Assert.Equal(2f, y);
        Assert.Equal(3f, z);

        LocoRerailSession.ToggleFacing();
        Assert.False(LocoRerailSession.ForceRegularDirection);

        LocoRerailSession.Clear();
        Assert.False(LocoRerailSession.IsActive);
        Assert.Equal(string.Empty, LocoRerailSession.TypeId);
    }

    [Fact]
    public void Smoke_poll_miss_keeps_last_aim_lock_freezes()
    {
        LocoRerailSession.Begin("DH4");
        LocoRerailSession.SetTarget("SW-C1O", 1f, 2f, 3f);
        Assert.True(LocoRerailSession.HasLatchedTarget);

        LocoRerailSession.ClearTargetIfUnlocked();
        Assert.Equal("SW-C1O", LocoRerailSession.TargetTrackId);

        LocoRerailSession.LockTarget();
        Assert.True(LocoRerailSession.IsTargetLocked);
        LocoRerailSession.SetTarget("SM-A1", 9f, 9f, 9f);
        Assert.Equal("SW-C1O", LocoRerailSession.TargetTrackId);

        LocoRerailSession.Begin("DE6");
        Assert.Equal("DE6", LocoRerailSession.TypeId);
        Assert.True(LocoRerailSession.HasLatchedTarget);
    }
}
