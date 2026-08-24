using System;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class BackupProximityTelemetryTests
{
    [Fact]
    public void Smoke_reverse_free_tip_caption_is_rear_not_front()
    {
        var reverse = BackupProximityTelemetry.CaptionKey(
            showChip: true,
            ProximityTravelDirection.Reverse,
            clearanceMeters: 12.4f,
            inCoupleRange: false,
            tipActive: true);
        var forward = BackupProximityTelemetry.CaptionKey(
            showChip: true,
            ProximityTravelDirection.Forward,
            clearanceMeters: 12.4f,
            inCoupleRange: false,
            tipActive: true);
        Assert.NotEqual(BackupProximityTelemetry.KeyOmit, reverse);
        Assert.NotEqual(reverse, forward);
        Assert.Contains(
            "Rear 12.4m",
            BackupProximityDisplay.FormatHud(12.4f, inCoupleRange: false, tipActive: true, label: "Rear"));
        Assert.DoesNotContain(
            "Front",
            BackupProximityDisplay.Format(12.4f, inCoupleRange: false, tipActive: true, label: "Rear"));
    }

    [Fact]
    public void Smoke_neutral_omits_chip()
    {
        Assert.Equal(
            BackupProximityTelemetry.KeyOmit,
            BackupProximityTelemetry.CaptionKey(
                showChip: true,
                ProximityTravelDirection.Neutral,
                clearanceMeters: 2f,
                inCoupleRange: false,
                tipActive: true));
        Assert.Equal(
            BackupProximityTelemetry.KeyOmit,
            BackupProximityTelemetry.CaptionKey(
                showChip: false,
                ProximityTravelDirection.Reverse,
                clearanceMeters: 2f,
                inCoupleRange: false,
                tipActive: true));
    }

    [Fact]
    public void Smoke_coupled_tip_omits_chip()
    {
        Assert.Equal(
            BackupProximityTelemetry.KeyOmit,
            BackupProximityTelemetry.CaptionKey(
                showChip: true,
                ProximityTravelDirection.Reverse,
                clearanceMeters: 0.4f,
                inCoupleRange: true,
                tipActive: false));
    }

    [Fact]
    public void Observe_fires_once_per_tenth_then_holds()
    {
        var cache = default(BackupProximityCache);
        var key = BackupProximityTelemetry.CaptionKey(
            true,
            ProximityTravelDirection.Reverse,
            2.0f,
            false,
            true);
        Assert.True(BackupProximityTelemetry.Observe(key, ref cache));
        Assert.False(BackupProximityTelemetry.Observe(key, ref cache));
        var next = BackupProximityTelemetry.CaptionKey(
            true,
            ProximityTravelDirection.Reverse,
            1.9f,
            false,
            true);
        Assert.True(BackupProximityTelemetry.Observe(next, ref cache));
    }

    [Fact]
    public void Observe_hide_after_show_fires()
    {
        var cache = default(BackupProximityCache);
        var shown = BackupProximityTelemetry.CaptionKey(
            true,
            ProximityTravelDirection.Reverse,
            8f,
            false,
            true);
        Assert.True(BackupProximityTelemetry.Observe(shown, ref cache));
        Assert.True(BackupProximityTelemetry.Observe(BackupProximityTelemetry.KeyOmit, ref cache));
        Assert.False(BackupProximityTelemetry.Observe(BackupProximityTelemetry.KeyOmit, ref cache));
    }

    [Fact]
    public void NextLog_init_then_throttled_change_then_hide()
    {
        var lastAt = -1f;
        var init = BackupProximityTelemetry.NextLog(
            BackupProximityTelemetry.CaptionKey(
                true,
                ProximityTravelDirection.Reverse,
                0.4f,
                true,
                true),
            ProximityTravelDirection.Reverse,
            0.4f,
            inCoupleRange: true,
            tipActive: true,
            nowSeconds: 0f,
            ref lastAt);
        Assert.Equal("T2 proximity init: end=Rear tenths=4 couple=1", init);

        var suppressed = BackupProximityTelemetry.NextLog(
            BackupProximityTelemetry.CaptionKey(
                true,
                ProximityTravelDirection.Reverse,
                0.3f,
                true,
                true),
            ProximityTravelDirection.Reverse,
            0.3f,
            inCoupleRange: true,
            tipActive: true,
            nowSeconds: 1f,
            ref lastAt);
        Assert.Null(suppressed);

        var hide = BackupProximityTelemetry.NextLog(
            BackupProximityTelemetry.KeyOmit,
            ProximityTravelDirection.Neutral,
            null,
            inCoupleRange: false,
            tipActive: false,
            nowSeconds: 10f,
            ref lastAt);
        Assert.Equal("T2 proximity hide", hide);
    }

    [Fact]
    public void Observe_does_not_allocate_when_key_holds()
    {
        var cache = default(BackupProximityCache);
        var key = BackupProximityTelemetry.CaptionKey(
            true,
            ProximityTravelDirection.Forward,
            9.0f,
            false,
            true);
        BackupProximityTelemetry.Observe(key, ref cache);

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10_000; i++)
        {
            BackupProximityTelemetry.Observe(key, ref cache);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
