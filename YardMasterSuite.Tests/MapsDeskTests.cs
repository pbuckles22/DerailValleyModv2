using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class DestinationCatalogTests
{
    private static readonly (string, string)[] Entries =
    {
        ("SM", "SM-O6I"),
        ("SM", "SM-L1"),
        ("FF", "FF-A2P"),
        ("", "bad"),
        ("HB", "#Y-01"),
    };

    [Fact]
    public void ListYards_sorted_unique()
    {
        Assert.Equal(new[] { "FF", "SM" }, DestinationCatalog.ListYards(Entries));
    }

    [Fact]
    public void ListTracksInYard_filtered_skips_hash_ids()
    {
        Assert.Equal(new[] { "SM-L1", "SM-O6I" }, DestinationCatalog.ListTracksInYard(Entries, "SM"));
        Assert.Empty(DestinationCatalog.ListTracksInYard(Entries, "HB"));
    }

    [Fact]
    public void CycleIndex_wraps()
    {
        Assert.Equal(0, DestinationCatalog.CycleIndex(2, 3, 1));
        Assert.Equal(2, DestinationCatalog.CycleIndex(0, 3, -1));
    }

    [Fact]
    public void YardIdFromTrackKey_uses_city_prefix()
    {
        Assert.Equal("SM", DestinationCatalog.YardIdFromTrackKey("SM-O6I"));
        Assert.Null(DestinationCatalog.YardIdFromTrackKey("#Y-01"));
        Assert.Null(DestinationCatalog.YardIdFromTrackKey("A1"));
    }

    [Fact]
    public void TryAdd_rejects_hash_and_junk_yard()
    {
        var catalog = new List<(string, string)>();
        Assert.True(DestinationCatalog.TryAdd(catalog, "SM", "SM-O6I"));
        Assert.False(DestinationCatalog.TryAdd(catalog, "Y", "SM-O6I"));
        Assert.False(DestinationCatalog.TryAdd(catalog, "SM", "#Y-01"));
        Assert.Single(catalog);
    }
}

public class RouteAlignAccessTests
{
    [Fact]
    public void CanAlign_requires_dispatcher()
    {
        Assert.True(RouteAlignAccess.CanAlign(true));
        Assert.False(RouteAlignAccess.CanAlign(false));
        Assert.Equal("Need Dispatcher", RouteAlignAccess.DeniedChip(false));
        Assert.Null(RouteAlignAccess.DeniedChip(true));
    }
}

[Collection("StaticSessions")]
public class RouteDestSessionTests
{
    public RouteDestSessionTests()
    {
        RouteDestSession.Clear();
        PathCheckSession.Clear();
    }

    [Fact]
    public void Set_yard_and_track_does_not_arm_end_path_check()
    {
        RouteDestSession.Set("SM", "SM-O6I");
        Assert.True(RouteDestSession.HasDestination);
        Assert.Equal("SM", RouteDestSession.YardId);
        Assert.Equal("SM-O6I", RouteDestSession.TrackId);
        Assert.False(PathCheckSession.HasDestination);
    }

    [Fact]
    public void SetTrackOnly_clears_yard()
    {
        RouteDestSession.Set("SM", "SM-O6I");
        RouteDestSession.SetTrackOnly("FF-A2P");
        Assert.Null(RouteDestSession.YardId);
        Assert.Equal("FF-A2P", RouteDestSession.TrackId);
        Assert.False(PathCheckSession.HasDestination);
    }
}

[Collection("StaticSessions")]
public class MapsDestApplyTests
{
    public MapsDestApplyTests()
    {
        RouteDestSession.Clear();
        PathCheckSession.Clear();
    }

    [Fact]
    public void Smoke_SetDest_binds_city_and_track_without_pathfind()
    {
        var kind = MapsDestApply.SetDest(" SM ", " SM-O6I ");
        Assert.Equal(MapsDestKind.Set, kind);
        Assert.Equal("SM", RouteDestSession.YardId);
        Assert.Equal("SM-O6I", RouteDestSession.TrackId);
        Assert.False(PathCheckSession.HasDestination);
    }

    [Fact]
    public void Smoke_SetDest_empty_rejects()
    {
        Assert.Equal(MapsDestKind.RejectEmpty, MapsDestApply.SetDest(null, "SM-O6I"));
        Assert.Equal(MapsDestKind.RejectEmpty, MapsDestApply.SetDest("SM", "  "));
        Assert.False(RouteDestSession.HasDestination);
    }

    [Fact]
    public void Smoke_Recheck_without_dest_binds_then_rechecks()
    {
        var kind = MapsDestApply.Recheck("FF", "FF-A2P");
        Assert.Equal(MapsDestKind.Recheck, kind);
        Assert.Equal("FF", RouteDestSession.YardId);
        Assert.Equal("FF-A2P", RouteDestSession.TrackId);
    }

    [Fact]
    public void Smoke_Recheck_with_dest_keeps_session()
    {
        MapsDestApply.SetDest("SM", "SM-O6I");
        var kind = MapsDestApply.Recheck("FF", "FF-A2P");
        Assert.Equal(MapsDestKind.Recheck, kind);
        Assert.Equal("SM", RouteDestSession.YardId);
        Assert.Equal("SM-O6I", RouteDestSession.TrackId);
    }

    [Fact]
    public void Smoke_Recheck_empty_rejects()
    {
        Assert.Equal(MapsDestKind.RejectEmpty, MapsDestApply.Recheck(null, null));
        Assert.False(RouteDestSession.HasDestination);
    }

    [Fact]
    public void Smoke_maps_dest_does_not_replace_end_path_check()
    {
        PathCheckSession.SetDestination("SM-A");
        MapsDestApply.SetDest("SM", "SM-B3I");
        Assert.Equal("SM-A", PathCheckSession.DestinationTrackId);
        MapsDestApply.Clear();
        Assert.Equal("SM-A", PathCheckSession.DestinationTrackId);
        Assert.False(RouteDestSession.HasDestination);
    }

    [Fact]
    public void Smoke_Clear_drops_dest()
    {
        MapsDestApply.SetDest("SM", "SM-O6I");
        Assert.Equal(MapsDestKind.Clear, MapsDestApply.Clear());
        Assert.False(RouteDestSession.HasDestination);
        Assert.False(PathCheckSession.HasDestination);
    }
}

public class MapsDestTelemetryTests
{
    [Fact]
    public void Smoke_set_and_recheck_include_city_track()
    {
        Assert.Equal(
            "T2 maps: dest set city=SM track=SM-O6I",
            MapsDestTelemetry.Format(MapsDestKind.Set, "SM", "SM-O6I"));
        Assert.Equal(
            "T2 maps: recheck city=SM track=SM-O6I",
            MapsDestTelemetry.Format(MapsDestKind.Recheck, "SM", "SM-O6I"));
        Assert.Equal("T2 maps: dest clear", MapsDestTelemetry.Format(MapsDestKind.Clear, "SM", "SM-O6I"));
        Assert.Equal("T2 maps: reject empty", MapsDestTelemetry.Format(MapsDestKind.RejectEmpty, null, null));
    }

    [Fact]
    public void Catalog_and_desk_lines_are_discrete()
    {
        Assert.Equal("T2 maps-desk: open", MapsDestTelemetry.DeskOpen);
        Assert.Equal("T2 maps-desk: close", MapsDestTelemetry.DeskClose);
        Assert.Equal("T2 maps-desk: catalog cities=12 tracks=40", MapsDestTelemetry.FormatCatalog(12, 40));
    }
}
