using System;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

/// <summary>
/// 9.1.3 Win 0 — sit still at SW, switch thrown, 2.5 km local graph dump.
/// Named after the gather smoke, not walker routing (Win 3).
/// </summary>
public class TrackGraphHarvestCodecTests
{
    [Fact]
    public void Sit_still_sw_leave_roundtrips_loco_tracks_junctions_and_boards()
    {
        var tracks = new[]
        {
            new HarvestedTrack(101, 546.7f, 591.4f, 600f, 500f, 120f),
            new HarvestedTrack(102, 600f, 500f, 700f, 400f, 80f),
        };
        var junctions = new[]
        {
            new HarvestedJunction(id: 9, stemId: 101, leftId: 102, rightId: 103, selectedBranch: 1),
        };
        var boards = new[]
        {
            new HarvestedGraphBoard(1398156, 768.8f, 285.4f, 40f, 40f, -0.98f, 0.18f, isDual: false, junctionNearby: false),
            new HarvestedGraphBoard(1398162, 758.1f, 292.2f, 50f, 50f, 0.95f, -0.30f, isDual: true, junctionNearby: true),
            new HarvestedGraphBoard(1402212, 2168.9f, 1011.7f, 60f, 40f, -0.48f, -0.87f, isDual: true, junctionNearby: true),
        };

        var text = TrackGraphHarvestCodec.Format(
            origin: "SW",
            locoX: 546.7f,
            locoY: 147.4f,
            locoZ: 591.4f,
            forwardX: 0.492f,
            forwardZ: -0.871f,
            tracks,
            tracks.Length,
            junctions,
            junctions.Length,
            boards,
            boards.Length);

        Assert.StartsWith(TrackGraphHarvestCodec.Header, text);
        Assert.Contains("origin SW", text, StringComparison.Ordinal);
        Assert.Contains("radiusM 2500", text, StringComparison.Ordinal);
        Assert.Contains("trackN 2", text, StringComparison.Ordinal);
        Assert.Contains("juncN 1", text, StringComparison.Ordinal);
        Assert.Contains("boardN 3", text, StringComparison.Ordinal);
        Assert.Contains("junc 9 101 102 103 1", text, StringComparison.Ordinal);

        Assert.True(TrackGraphHarvestCodec.TryParse(text, out var snap));
        Assert.Equal("SW", snap.Origin);
        Assert.Equal(2500f, snap.RadiusMeters);
        Assert.Equal(2, snap.TrackN);
        Assert.Equal(1, snap.JuncN);
        Assert.Equal(3, snap.BoardN);
        Assert.Equal(546.7f, snap.LocoX, precision: 1);
        Assert.Equal(0.492f, snap.ForwardX, precision: 3);
        Assert.Equal(101, snap.Tracks[0].Id);
        Assert.Equal(120f, snap.Tracks[0].LengthMeters, precision: 1);
        Assert.Equal(1, snap.Junctions[0].SelectedBranch);
        Assert.Equal(103, snap.Junctions[0].RightId);
        Assert.Equal(1398156, snap.Boards[0].Id);
        Assert.False(snap.Boards[0].IsDual);
        Assert.Equal(1398162, snap.Boards[1].Id);
        Assert.True(snap.Boards[1].IsDual);
        Assert.True(snap.Boards[1].JunctionNearby);
        Assert.True(
            PostedPathAheadGate.ShouldSkipSymmetricDualThrough(
                TrackGraphCore.ToPostedBoard(snap.Boards[1]),
                diverging: false));
        Assert.Equal(1402212, snap.Boards[2].Id);
        Assert.Equal(60f, snap.Boards[2].ThroughKmh);
        Assert.Equal(40f, snap.Boards[2].DivergeKmh);
        Assert.True(snap.Boards[2].IsDual);
        Assert.False(
            PostedPathAheadGate.ShouldSkipSymmetricDualThrough(
                TrackGraphCore.ToPostedBoard(snap.Boards[2]),
                diverging: false));
    }

    [Fact]
    public void SW_leave_2_5km_radius_includes_harvest_sixty_board_1402212()
    {
        var snap = HtpFixtures.LoadBoardsSw20260831();
        var sixty = HtpFixtures.RequireBoard(in snap, 1402212);
        Assert.True(
            TrackGraphHarvestPolicy.IsWithinRadius(snap.NoseX, snap.NoseZ, sixty.X, sixty.Z),
            "harvest 60 must sit inside the 2.5 km dump circle from SW nose");
        Assert.True(
            TrackGraphHarvestPolicy.IsWithinRadius(snap.NoseX, snap.NoseZ, 768.756958f, 285.408936f),
            "harvest 40 must sit inside the 2.5 km dump circle");
    }

    [Fact]
    public void Excludes_point_beyond_2_5km()
    {
        Assert.True(TrackGraphHarvestPolicy.IsWithinRadius(0f, 0f, 2500f, 0f));
        Assert.False(TrackGraphHarvestPolicy.IsWithinRadius(0f, 0f, 2500.1f, 0f));
        Assert.False(TrackGraphHarvestPolicy.IncludeTrack(
            locoX: 0f,
            locoZ: 0f,
            inX: 2600f,
            inZ: 0f,
            outX: 2700f,
            outZ: 0f));
        Assert.True(TrackGraphHarvestPolicy.IncludeTrack(
            locoX: 0f,
            locoZ: 0f,
            inX: 100f,
            inZ: 0f,
            outX: 2600f,
            outZ: 0f));
    }

    [Fact]
    public void ShouldWrite_once_when_still_maps_and_local_graph()
    {
        Assert.True(TrackGraphHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            still: true,
            trackCount: 8,
            junctionCount: 3));
        Assert.False(TrackGraphHarvestPolicy.ShouldWrite(
            alreadyWritten: true,
            mapsLeg: true,
            still: true,
            trackCount: 8,
            junctionCount: 3));
        Assert.False(TrackGraphHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: false,
            still: true,
            trackCount: 8,
            junctionCount: 3));
        Assert.False(TrackGraphHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            still: false,
            trackCount: 8,
            junctionCount: 3));
        Assert.False(TrackGraphHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            still: true,
            trackCount: 0,
            junctionCount: 3));
        Assert.False(TrackGraphHarvestPolicy.ShouldWrite(
            alreadyWritten: false,
            mapsLeg: true,
            still: true,
            trackCount: 8,
            junctionCount: 0));
        Assert.True(TrackGraphHarvestPolicy.IsStill(PostedStickyLimit.StandstillMaxSpeedKmh));
        Assert.False(TrackGraphHarvestPolicy.IsStill(PostedStickyLimit.StandstillMaxSpeedKmh + 0.1f));
    }

    [Fact]
    public void TryParse_rejects_garbage()
    {
        Assert.False(TrackGraphHarvestCodec.TryParse(null, out _));
        Assert.False(TrackGraphHarvestCodec.TryParse("YMS-BOARDS 1\n", out _));
    }

    [Fact]
    public void Sit_still_sw_graph_dump_contains_harvest_sixty_1402212()
    {
        var snap = HtpFixtures.LoadGraphSw20260901();
        Assert.Equal("SW", snap.Origin);
        Assert.Equal(75, snap.TrackN);
        Assert.Equal(20, snap.JuncN);
        Assert.Equal(20, snap.BoardN);
        Assert.Equal(546.7f, snap.LocoX, precision: 1);
        Assert.Equal(591.4f, snap.LocoZ, precision: 1);

        var foundForty = false;
        var foundSixty = false;
        for (var i = 0; i < snap.Boards.Count; i++)
        {
            var b = snap.Boards[i];
            if (b.Id == 1398156)
            {
                foundForty = true;
                Assert.Equal(40f, b.ThroughKmh);
            }

            if (b.Id == 1402212)
            {
                foundSixty = true;
                Assert.Equal(60f, b.ThroughKmh);
                Assert.Equal(40f, b.DivergeKmh);
            }
        }

        Assert.True(foundForty, "harvest 40 1398156 missing from sit-still dump");
        Assert.True(foundSixty, "harvest 60 1402212 missing from sit-still dump");
    }

    [Fact]
    public void Sit_still_sw_graph_dump_marks_throat_50_1398162_as_symmetric_dual()
    {
        var snap = HtpFixtures.LoadGraphSw20260901();
        HarvestedGraphBoard? throat = null;
        HarvestedGraphBoard? sixty = null;
        HarvestedGraphBoard? forty = null;
        for (var i = 0; i < snap.Boards.Count; i++)
        {
            var b = snap.Boards[i];
            if (b.Id == 1398156)
            {
                forty = b;
            }

            if (b.Id == 1398162)
            {
                throat = b;
            }

            if (b.Id == 1402212)
            {
                sixty = b;
            }
        }

        Assert.True(forty.HasValue);
        Assert.False(forty.Value.IsDual);
        Assert.True(throat.HasValue);
        Assert.True(throat.Value.IsDual);
        Assert.True(throat.Value.JunctionNearby);
        Assert.True(
            PostedPathAheadGate.ShouldSkipSymmetricDualThrough(
                TrackGraphCore.ToPostedBoard(throat.Value),
                diverging: false),
            "50/50 throat dual must skip through governance");
        Assert.True(sixty.HasValue);
        Assert.True(sixty.Value.IsDual);
        Assert.False(
            PostedPathAheadGate.ShouldSkipSymmetricDualThrough(
                TrackGraphCore.ToPostedBoard(sixty.Value),
                diverging: false),
            "60/40 is dual but not symmetric — must still govern");
    }

    [Fact]
    public void Sit_still_sw_exit_junction_1003218_is_thrown_for_40_60_leave()
    {
        var snap = HtpFixtures.LoadGraphSw20260901();
        CoreJunction? exit = null;
        for (var i = 0; i < snap.Junctions.Count; i++)
        {
            var row = snap.Junctions[i];
            if (row.Id != 1003218)
            {
                continue;
            }

            exit = TrackGraphCore.Junction(row);
            break;
        }

        Assert.True(exit.HasValue, "SW exit junction 1003218 missing from sit-still dump");
        Assert.Equal(1, exit.Value.SelectedBranch);
        Assert.Equal(1003218, exit.Value.Id);
        Assert.Equal(75, TrackGraphCore.Tracks(snap).Length);
        Assert.Equal(20, TrackGraphCore.Junctions(snap).Length);
    }
}
