using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class MapsTurntableDestTests
{
    [Fact]
    public void IsToken_matches_Turntable()
    {
        Assert.True(MapsTurntableDest.IsToken("Turntable"));
        Assert.True(MapsTurntableDest.IsToken(" turntable "));
        Assert.False(MapsTurntableDest.IsToken("SW-B1S"));
        Assert.False(MapsTurntableDest.IsToken(null));
    }

    [Fact]
    public void WithTokenFirst_prepends_and_dedupes()
    {
        var listed = MapsTurntableDest.WithTokenFirst(new[] { "SW-B1S", "Turntable", "SW-A1L" });
        Assert.Equal(new[] { "Turntable", "SW-B1S", "SW-A1L" }, listed);
    }

    [Fact]
    public void WithTokenFirst_empty_still_offers_token()
    {
        Assert.Equal(new[] { "Turntable" }, MapsTurntableDest.WithTokenFirst(null));
        Assert.Equal(new[] { "Turntable" }, MapsTurntableDest.WithTokenFirst(Array.Empty<string>()));
    }

    [Fact]
    public void TryResolve_named_track_passthrough()
    {
        Assert.True(MapsTurntableDest.TryResolveTrackId(
            "SW", " SW-B1S ", null, out var id, out var err));
        Assert.Equal("SW-B1S", id);
        Assert.Null(err);
    }

    [Fact]
    public void Smoke_TryResolve_Turntable_uses_resolver_callback()
    {
        Assert.True(MapsTurntableDest.TryResolveTrackId(
            "SW",
            MapsTurntableDest.Token,
            yard => yard == "SW" ? "#Y-#S1774#T" : null,
            out var id,
            out var err));
        Assert.Equal("#Y-#S1774#T", id);
        Assert.Null(err);
    }

    [Fact]
    public void TryResolve_Turntable_fail_closed_without_tt()
    {
        Assert.False(MapsTurntableDest.TryResolveTrackId(
            "CME",
            MapsTurntableDest.Token,
            _ => null,
            out _,
            out var err));
        Assert.Equal("no turntable in CME", err);
    }

    [Fact]
    public void TryResolve_empty_rejects()
    {
        Assert.False(MapsTurntableDest.TryResolveTrackId(null, "SW-B1S", null, out _, out var err));
        Assert.Equal("pick city + track", err);
    }
}

[Collection("StaticSessions")]
public class MapsTurntableDestApplyTests
{
    public MapsTurntableDestApplyTests()
    {
        RouteDestSession.Clear();
        PathCheckSession.Clear();
    }

    /// <summary>
    /// Smoke **8.4**: desk picks Turntable → resolve → Set dest binds session yard + anonymous TT.
    /// </summary>
    [Fact]
    public void Smoke_SetDest_Turntable_binds_session_yard_and_anonymous_track()
    {
        Assert.True(MapsTurntableDest.TryResolveTrackId(
            "SW",
            MapsTurntableDest.Token,
            _ => "#Y-#S1774#T",
            out var trackId,
            out _));
        var kind = MapsDestApply.SetDest("SW", trackId);
        Assert.Equal(MapsDestKind.Set, kind);
        Assert.Equal("SW", RouteDestSession.YardId);
        Assert.Equal("#Y-#S1774#T", RouteDestSession.TrackId);
        Assert.False(PathCheckSession.HasDestination);
    }
}
