using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SpeedLimitDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_and_whole_kmh()
    {
        Assert.Equal("— Limit", SpeedLimitDisplay.Format(null));
        Assert.Equal("Limit 60", SpeedLimitDisplay.Format(60f));
        Assert.Equal("Limit 10", SpeedLimitDisplay.Format(9.6f));
    }

    [Fact]
    public void FormatHud_yellow_from_ten_below_through_five_over()
    {
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHud(49f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.WarningColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(50f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.CriticalColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHud(66f, 60f));
    }

    [Fact]
    public void FormatHud_placeholder_when_limit_unknown()
    {
        Assert.Equal("— Limit", SpeedLimitDisplay.FormatHud(40f, null));
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHud(null, 60f));
    }

    [Fact]
    public void Smoke_usable_loco_without_posted_is_limit_120_auth_default()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: true, postedKmh: null);
        Assert.Equal(SpeedLimitState.UnrestrictedKmh, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Default, snap.Authority);
        Assert.Equal("Limit 120", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.Equal("T2 limit init: 120 auth=default", SpeedLimitTelemetry.FormatInit(in snap));
    }

    [Fact]
    public void Smoke_curve_cannot_move_hud_limit_without_posted_board()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: true, postedKmh: null);
        Assert.Equal(120f, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Default, snap.Authority);
        Assert.DoesNotContain("Next", SpeedLimitDisplay.FormatHudOrEmpty(10f, snap.LimitKmh));
    }

    [Fact]
    public void Smoke_posted_board_behind_is_limit_60_auth_posted()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: true, postedKmh: 60f);
        Assert.Equal(60f, snap.LimitKmh);
        Assert.Equal(LimitAuthority.Posted, snap.Authority);
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.DoesNotContain("Next", SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
        Assert.DoesNotContain("30", SpeedLimitTelemetry.FormatInit(in snap));
        Assert.Equal("T2 limit init: 60 auth=posted", SpeedLimitTelemetry.FormatInit(in snap));
    }

    [Fact]
    public void Smoke_unboarded_omits_limit_even_if_posted_sticky()
    {
        var snap = SpeedLimitState.Resolve(hasUsableLoco: false, postedKmh: 60f);
        Assert.Null(snap.LimitKmh);
        Assert.Equal(LimitAuthority.None, snap.Authority);
        Assert.Equal(string.Empty, SpeedLimitDisplay.FormatHudOrEmpty(0f, snap.LimitKmh));
    }

    [Fact]
    public void Smoke_cab_limit_omits_next_distance()
    {
        var label = SpeedLimitDisplay.FormatHudOrEmpty(20f, 40f);
        Assert.Equal("Limit 40", label);
        Assert.DoesNotContain("Next", label);
        Assert.DoesNotContain("— Limit", label);
    }

    [Fact]
    public void Smoke_after_6_next_8_omits_meters_until_close()
    {
        var far = SpeedLimitState.Resolve(
            hasUsableLoco: true,
            postedKmh: 60f,
            nextKmh: 80f,
            nextAlongMeters: 800f);
        Assert.Equal(
            "Limit 60 | Next 80",
            SpeedLimitDisplay.FormatHudOrEmpty(40f, far.LimitKmh, far.NextKmh, far.NextAlongMeters, 38f));
        Assert.Equal("T2 limit init: 60 auth=posted next=80", SpeedLimitTelemetry.FormatInit(in far));

        var close = SpeedLimitState.Resolve(
            hasUsableLoco: true,
            postedKmh: 60f,
            nextKmh: 80f,
            nextAlongMeters: 115f);
        Assert.Equal(
            "Limit 60 | Next 80 (115m)",
            SpeedLimitDisplay.FormatHudOrEmpty(40f, close.LimitKmh, close.NextKmh, close.NextAlongMeters, 38f));
    }

    [Fact]
    public void Smoke_take_8_shows_next_5_meters_when_drop_is_inside_reveal()
    {
        var snap = SpeedLimitState.Resolve(
            hasUsableLoco: true,
            postedKmh: 80f,
            nextKmh: 50f,
            nextAlongMeters: 579f);
        Assert.Equal(
            "Limit 80 | Next 50 (579m)",
            SpeedLimitDisplay.FormatHudOrEmpty(
                50f,
                snap.LimitKmh,
                snap.NextKmh,
                snap.NextAlongMeters,
                38f));
        Assert.Equal(
            "T2 limit init: 80 auth=posted next=50 579m",
            SpeedLimitTelemetry.FormatInit(in snap, massTonnes: 38f));
    }

    [Fact]
    public void Smoke_cab_limit_shows_next_without_meters_when_far()
    {
        var snap = SpeedLimitState.Resolve(
            hasUsableLoco: true,
            postedKmh: 80f,
            nextKmh: 50f,
            nextAlongMeters: 800f);
        Assert.Equal(50f, snap.NextKmh);
        Assert.Equal(800f, snap.NextAlongMeters);
        Assert.Equal(
            "Limit 80 | Next 50",
            SpeedLimitDisplay.FormatHudOrEmpty(40f, snap.LimitKmh, snap.NextKmh, snap.NextAlongMeters, 38f));
        Assert.Equal(
            "T2 limit init: 80 auth=posted next=50",
            SpeedLimitTelemetry.FormatInit(in snap));
    }

    [Fact]
    public void Smoke_cab_limit_shows_next_meters_when_close()
    {
        var snap = SpeedLimitState.Resolve(
            hasUsableLoco: true,
            postedKmh: 80f,
            nextKmh: 50f,
            nextAlongMeters: 50f);
        Assert.Equal(
            "Limit 80 | Next 50 (50m)",
            SpeedLimitDisplay.FormatHudOrEmpty(40f, snap.LimitKmh, snap.NextKmh, snap.NextAlongMeters, 38f));
        Assert.Equal(
            "T2 limit init: 80 auth=posted next=50 50m",
            SpeedLimitTelemetry.FormatInit(in snap, massTonnes: 38f));
    }

    [Fact]
    public void FormatHud_colors_limit_chip_not_next()
    {
        var hud = SpeedLimitDisplay.FormatHud(
            speedKmh: 86f,
            limitKmh: 80f,
            nextKmh: 50f,
            nextDistanceMeters: 50f,
            massTonnes: 38f);
        Assert.StartsWith($"<color={SpeedLimitDisplay.CriticalColor}>Limit 80</color>", hud);
        Assert.Contains("Next 50 (50m)", hud);
    }

    [Fact]
    public void Observe_does_not_allocate_when_posted_limit_holds()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(60f, LimitAuthority.Posted);
        SpeedLimitTelemetry.Observe(snap, ref cache, out _);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            SpeedLimitTelemetry.Observe(snap, ref cache, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void FormatHudOrEmpty_yellow_and_red_bands()
    {
        Assert.Equal("Limit 60", SpeedLimitDisplay.FormatHudOrEmpty(49f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.WarningColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHudOrEmpty(50f, 60f));
        Assert.Equal(
            $"<color={SpeedLimitDisplay.CriticalColor}>Limit 60</color>",
            SpeedLimitDisplay.FormatHudOrEmpty(66f, 60f));
    }
}

public class SpeedLimitTelemetryTests
{
    [Fact]
    public void Observe_dedupes_same_limit()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(60f, LimitAuthority.Posted);
        Assert.True(SpeedLimitTelemetry.Observe(snap, ref cache, out _));
        Assert.False(SpeedLimitTelemetry.Observe(snap, ref cache, out _));
    }

    [Fact]
    public void Observe_publishes_when_next_number_changes()
    {
        var cache = default(SpeedLimitCache);
        var limit = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 50f, nextAlongMeters: 800f);
        Assert.True(SpeedLimitTelemetry.Observe(limit, ref cache, out _));
        Assert.False(SpeedLimitTelemetry.Observe(limit, ref cache, out _));

        var closer = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 40f, nextAlongMeters: 800f);
        Assert.True(SpeedLimitTelemetry.Observe(closer, ref cache, out _));
    }

    [Fact]
    public void Observe_does_not_chatter_next_meters_at_reveal_edge()
    {
        var cache = default(SpeedLimitCache);
        var a = new SpeedLimitSnapshot(120f, LimitAuthority.Default, nextKmh: 40f, nextAlongMeters: 599f);
        Assert.True(SpeedLimitTelemetry.Observe(a, ref cache, out _, massTonnes: 38f));
        Assert.True(cache.EmitLog);

        var b = new SpeedLimitSnapshot(120f, LimitAuthority.Default, nextKmh: 40f, nextAlongMeters: 601f);
        Assert.True(SpeedLimitTelemetry.Observe(b, ref cache, out _, massTonnes: 38f));
        Assert.False(cache.EmitLog);
        Assert.True(cache.NextBucket >= 0);
    }

    [Fact]
    public void Observe_does_not_chatter_far_next_every_ten_meters()
    {
        var cache = default(SpeedLimitCache);
        var a = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 50f, nextAlongMeters: 800f);
        var b = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 50f, nextAlongMeters: 790f);
        Assert.True(SpeedLimitTelemetry.Observe(a, ref cache, out _));
        Assert.False(SpeedLimitTelemetry.Observe(b, ref cache, out _, massTonnes: 38f));
    }

    [Fact]
    public void Observe_close_meters_hud_without_t2_every_ten()
    {
        var cache = default(SpeedLimitCache);
        var a = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 50f, nextAlongMeters: 50f);
        Assert.True(SpeedLimitTelemetry.Observe(a, ref cache, out _, massTonnes: 38f));
        Assert.True(cache.EmitLog);

        var b = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 50f, nextAlongMeters: 40f);
        Assert.True(SpeedLimitTelemetry.Observe(b, ref cache, out _, massTonnes: 38f));
        Assert.False(cache.EmitLog);
    }

    [Fact]
    public void Observe_does_not_allocate_when_far_next_holds()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(80f, LimitAuthority.Posted, nextKmh: 50f, nextAlongMeters: 800f);
        SpeedLimitTelemetry.Observe(snap, ref cache, out _);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            SpeedLimitTelemetry.Observe(snap, ref cache, out _, massTonnes: 38f);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Observe_does_not_allocate_when_limit_holds()
    {
        var cache = default(SpeedLimitCache);
        var snap = new SpeedLimitSnapshot(120f, LimitAuthority.Default);
        SpeedLimitTelemetry.Observe(snap, ref cache, out _);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            SpeedLimitTelemetry.Observe(snap, ref cache, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
