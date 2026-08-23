using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (6.16): radar caption is type, then meters, no track.
/// </summary>
public class LocoRadarDisplayTests
{
    /// <summary>
    /// H104: cab windows ran `feature=15-19` because the caption string was rebuilt every
    /// LateUpdate. Standing still must reuse one metre key; walking must move it.
    /// </summary>
    [Fact]
    public void Smoke_cab_idle_reuses_caption_metre_key()
    {
        Assert.Equal(63, LocoRadarDisplay.CaptionMeters(63f));
        Assert.Equal(63, LocoRadarDisplay.CaptionMeters(63.4f));
        Assert.Equal(64, LocoRadarDisplay.CaptionMeters(63.6f));
        Assert.Equal(0, LocoRadarDisplay.CaptionMeters(-5f));
    }

    [Fact]
    public void Caption_metre_key_matches_rendered_caption()
    {
        Assert.Equal(
            "S060\n" + LocoRadarDisplay.CaptionMeters(152.7f) + "m",
            LocoRadarDisplay.FormatCaption("S060", 152.7f));
    }

    [Fact]
    public void FormatCaption_distance_only_when_no_type()
    {
        Assert.Equal("120m", LocoRadarDisplay.FormatCaption(null, 120.4f));
        Assert.Equal("0m", LocoRadarDisplay.FormatCaption("  ", -5f));
    }

    [Fact]
    public void FormatCaption_type_newline_distance()
    {
        Assert.Equal("DE2\n145m", LocoRadarDisplay.FormatCaption("LocoDE2", 145.2f));
        Assert.Equal("DE6\n10m", LocoRadarDisplay.FormatCaption("DE6", 9.6f));
    }

    [Fact]
    public void Smoke_caption_is_name_then_meters_no_track()
    {
        Assert.Equal("DE2\n145m", LocoRadarDisplay.FormatCaption("LocoDE2", 145f, "SM-O6I"));
        Assert.Equal("120m", LocoRadarDisplay.FormatCaption(null, 120f, "C-06S"));
        Assert.Equal("S060\n63m", LocoRadarDisplay.FormatCaption("S060", 63f, "SW-A1P"));
        Assert.DoesNotContain("SW-A1P", LocoRadarDisplay.FormatCaption("S060", 63f, "SW-A1P"));
        Assert.DoesNotContain("SM-O6I", LocoRadarDisplay.FormatCaption("LocoDE2", 145f, "SM-O6I"));
    }

    [Fact]
    public void TrackIncludesCity_detects_SM_style()
    {
        Assert.True(LocoRadarDisplay.TrackIncludesCity("SM-T12P"));
        Assert.True(LocoRadarDisplay.TrackIncludesCity("FF-A1"));
        Assert.True(LocoRadarDisplay.TrackIncludesCity("HB-O6I"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity("#Y"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity("Y"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity("T12P"));
        Assert.False(LocoRadarDisplay.TrackIncludesCity(null));
    }

    [Fact]
    public void Smoke_place_is_track_id_or_city_spur()
    {
        Assert.Equal("SM-T12P", LocoRadarDisplay.FormatPlace("SM-T12P", "SM"));
        Assert.Equal("FF #Y", LocoRadarDisplay.FormatPlace("#Y", "FF"));
        Assert.Equal("FF", LocoRadarDisplay.FormatPlace(null, "FF"));
        Assert.Equal("#Y", LocoRadarDisplay.FormatPlace("#Y", null));
        Assert.Equal("HB", LocoRadarDisplay.FormatPlace("", "HB"));
    }

    [Fact]
    public void FormatPlace_rejects_junk_yardId_matching_spur_track()
    {
        Assert.Equal("#Y", LocoRadarDisplay.FormatPlace("#Y", "#Y"));
        Assert.Equal("#Y", LocoRadarDisplay.FormatPlace("#Y", "Y"));
        Assert.False(LocoRadarDisplay.IsUsableCityYardId("#Y"));
        Assert.False(LocoRadarDisplay.IsUsableCityYardId("Y"));
        Assert.True(LocoRadarDisplay.IsUsableCityYardId("FF"));
        Assert.True(LocoRadarDisplay.IsUsableCityYardId("SM"));
    }

    [Fact]
    public void FormatPlace_keeps_spur_token_with_real_city()
    {
        Assert.Equal("FF #Y", LocoRadarDisplay.FormatPlace("#Y", "FF"));
        Assert.Equal("FF", LocoRadarDisplay.FormatPlace(null, "FF"));
        Assert.NotEqual("FF", LocoRadarDisplay.FormatPlace("#Y", "FF"));
    }

    [Fact]
    public void ShortTypeId_strips_Loco_prefix()
    {
        Assert.Equal("DE2", LocoRadarDisplay.ShortTypeId("LocoDE2"));
        Assert.Equal("DE6", LocoRadarDisplay.ShortTypeId("Loco DE6"));
        Assert.Null(LocoRadarDisplay.ShortTypeId(null));
        Assert.Null(LocoRadarDisplay.ShortTypeId("   "));
    }
}
