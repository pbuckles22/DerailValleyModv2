using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest: always-on Marked chip (**6.11** / v1 1.14).
/// </summary>
public class ParkMarkDisplayTests
{
    [Fact]
    public void Smoke_unmarked_omits_marked_chip()
    {
        Assert.Null(ParkMarkDisplay.FormatReturn(null, null, 10f, 20f));
    }

    [Fact]
    public void Unknown_player_shows_dash_marked()
    {
        Assert.Equal("— Marked", ParkMarkDisplay.FormatReturn(10f, 20f, null, null));
    }

    [Fact]
    public void Smoke_walk_away_shows_bearing_and_meters()
    {
        Assert.Equal("Marked W 100m", ParkMarkDisplay.FormatReturn(0f, 0f, 100f, 0f));
        Assert.Equal("Marked S 50m", ParkMarkDisplay.FormatReturn(0f, 0f, 0f, 50f));
    }

    [Fact]
    public void Smoke_home_mark_shows_marked_here()
    {
        Assert.Equal("Marked here", ParkMarkDisplay.FormatReturn(10.2f, 20.4f, 10.4f, 20.1f));
    }

    [Fact]
    public void Return_point_is_compass_or_here()
    {
        Assert.Equal("W", ParkMarkDisplay.TryGetReturnPoint(0f, 0f, 100f, 0f));
        Assert.Equal("here", ParkMarkDisplay.TryGetReturnPoint(10f, 20f, 10.2f, 20.1f));
    }
}

[Collection("StaticSessions")]
public class ParkMarkSessionTests
{
    public ParkMarkSessionTests()
    {
        ParkMarkSession.Clear();
    }

    [Fact]
    public void Set_stores_xz()
    {
        ParkMarkSession.Set(12.5f, -40f);
        Assert.True(ParkMarkSession.HasMark);
        Assert.True(ParkMarkSession.TryGet(out var x, out var z));
        Assert.Equal(12.5f, x);
        Assert.Equal(-40f, z);
    }

    [Fact]
    public void Clear_drops_the_mark()
    {
        ParkMarkSession.Set(1f, 2f);
        ParkMarkSession.Clear();
        Assert.False(ParkMarkSession.HasMark);
        Assert.False(ParkMarkSession.TryGet(out _, out _));
    }

    [Fact]
    public void Set_xyz_keeps_y_for_ar_pin()
    {
        ParkMarkSession.Set(12.5f, 3.2f, -40f);
        Assert.True(ParkMarkSession.TryGet(out var x, out var y, out var z));
        Assert.Equal(12.5f, x);
        Assert.Equal(3.2f, y);
        Assert.Equal(-40f, z);
    }

    [Fact]
    public void Set_updates_existing_mark()
    {
        ParkMarkSession.Set(1f, 2f);
        ParkMarkSession.Set(9f, 8f);
        Assert.True(ParkMarkSession.TryGet(out var x, out var z));
        Assert.Equal(9f, x);
        Assert.Equal(8f, z);
    }
}
