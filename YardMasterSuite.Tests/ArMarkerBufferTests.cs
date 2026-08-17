using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (planned 3.2): hide = off-screen slot, never drop capacity.
/// </summary>
public class ArMarkerBufferTests
{
    [Fact]
    public void Hidden_office_slot_is_off_screen_capacity_unchanged()
    {
        var slots = ArMarkerBuffer.Create();
        Assert.Equal(ArMarkerBuffer.Capacity, slots.Length);

        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            guiX: 400f,
            guiY: 200f,
            ArMarkerPlace.OnObject,
            distanceMeters: 40);

        ArMarkerBuffer.Hide(ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)]);

        Assert.Equal(ArMarkerBuffer.Capacity, slots.Length);
        var office = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)];
        Assert.False(office.Occupied);
        Assert.Equal(ArMarkerPlace.Hidden, office.Place);
        Assert.Equal(ArMarkerBuffer.OffScreenX, office.GuiX);
        Assert.Equal(ArMarkerBuffer.OffScreenY, office.GuiY);
        Assert.False(ArMarkerBuffer.ShouldDrawSlot(in office));
    }

    [Fact]
    public void Launcher_hides_ar_when_player_transform_missing()
    {
        Assert.False(ArOverlay.ShouldDraw(playerTransformPresent: false));
        Assert.True(ArOverlay.ShouldDraw(playerTransformPresent: true));
    }

    [Fact]
    public void Look_behind_office_resolve_is_edge_not_object()
    {
        ArMarkerPlacement.Resolve(
            viewForward: -1f,
            viewRight: 4f,
            screenX: 400f,
            screenY: 300f,
            screenZ: -2f,
            screenWidth: 800f,
            screenHeight: 600f,
            wasBehind: false,
            previousEdge: ArHorizontalEdge.None,
            previousPlace: ArMarkerPlace.Hidden,
            out var behind,
            out var place,
            out var guiX,
            out var guiY,
            out var edge);

        Assert.True(behind);
        Assert.Equal(ArMarkerPlace.Edge, place);
        Assert.Equal(ArHorizontalEdge.Right, edge);
        Assert.Equal(800f - ArMarkerProjection.DefaultEdgeMarginPixels, guiX);
        Assert.Equal(ArMarkerProjection.ToGuiY(600f * 0.5f, 600f), guiY);
    }

    [Fact]
    public void Ahead_on_screen_office_resolve_is_on_object()
    {
        ArMarkerPlacement.Resolve(
            viewForward: 2f,
            viewRight: 0.1f,
            screenX: 400f,
            screenY: 300f,
            screenZ: 12f,
            screenWidth: 800f,
            screenHeight: 600f,
            wasBehind: false,
            previousEdge: ArHorizontalEdge.None,
            previousPlace: ArMarkerPlace.Hidden,
            out var behind,
            out var place,
            out var guiX,
            out var guiY,
            out var edge);

        Assert.False(behind);
        Assert.Equal(ArMarkerPlace.OnObject, place);
        Assert.Equal(ArHorizontalEdge.None, edge);
        Assert.Equal(400f, guiX);
        Assert.Equal(ArMarkerProjection.ToGuiY(300f, 600f), guiY);
    }

    [Fact]
    public void Loco_off_top_of_view_is_mid_edge_not_hud()
    {
        ArMarkerPlacement.Resolve(
            viewForward: 2f,
            viewRight: -3f,
            screenX: 20f,
            screenY: 700f,
            screenZ: 8f,
            screenWidth: 800f,
            screenHeight: 600f,
            wasBehind: false,
            previousEdge: ArHorizontalEdge.None,
            previousPlace: ArMarkerPlace.Hidden,
            out var behind,
            out var place,
            out var guiX,
            out var guiY,
            out var edge);

        Assert.False(behind);
        Assert.Equal(ArMarkerPlace.Edge, place);
        Assert.Equal(ArHorizontalEdge.Left, edge);
        Assert.Equal(ArMarkerProjection.DefaultEdgeMarginPixels, guiX);
        Assert.Equal(ArMarkerProjection.ToGuiY(600f * 0.5f, 600f), guiY);
        Assert.Equal(ArEdgeBand.Mid, ArEdgeBanding.ClassifyGuiY(guiY, 600f));
    }

    [Fact]
    public void Classify_guiY_in_hud_stack_is_top()
    {
        Assert.Equal(ArEdgeBand.Top, ArEdgeBanding.ClassifyGuiY(28f, 600f));
        Assert.Equal(ArEdgeBand.Mid, ArEdgeBanding.ClassifyGuiY(300f, 600f));
    }

    [Fact]
    public void Screen_edge_hysteresis_holds_object_when_barely_off()
    {
        ArMarkerPlacement.Resolve(
            viewForward: 2f,
            viewRight: 0.1f,
            screenX: -20f,
            screenY: 300f,
            screenZ: 12f,
            screenWidth: 800f,
            screenHeight: 600f,
            wasBehind: false,
            previousEdge: ArHorizontalEdge.None,
            previousPlace: ArMarkerPlace.OnObject,
            out _,
            out var place,
            out _,
            out _,
            out _);

        Assert.Equal(ArMarkerPlace.OnObject, place);
    }

    [Fact]
    public void Screen_edge_hysteresis_holds_edge_when_barely_on()
    {
        ArMarkerPlacement.Resolve(
            viewForward: 2f,
            viewRight: -3f,
            screenX: 20f,
            screenY: 300f,
            screenZ: 12f,
            screenWidth: 800f,
            screenHeight: 600f,
            wasBehind: false,
            previousEdge: ArHorizontalEdge.Left,
            previousPlace: ArMarkerPlace.Edge,
            out _,
            out var place,
            out _,
            out _,
            out var edge);

        Assert.Equal(ArMarkerPlace.Edge, place);
        Assert.Equal(ArHorizontalEdge.Left, edge);
    }
}

/// <summary>
/// Smoke harvest (planned 3.2): T2 ar logs occupancy+place on change only.
/// </summary>
public class ArTelemetryTests
{
    [Fact]
    public void World_load_emits_T2_ar_init()
    {
        var empty = default(ArOverlaySnapshot);
        var line = ArTelemetry.NextLog(previous: null, in empty);

        Assert.Equal("T2 ar init: loco=— office=— pin=—", line);
    }

    [Fact]
    public void Office_appearing_as_edge_emits_T2_ar_change()
    {
        var none = default(ArOverlaySnapshot);
        var officeEdge = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.Edge, ArMarkerPlace.Hidden);

        Assert.Equal(
            "T2 ar change: loco=— office=edge pin=—",
            ArTelemetry.NextLog(none, in officeEdge));
    }

    [Fact]
    public void Same_ar_set_is_silent()
    {
        var office = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.OnObject, ArMarkerPlace.Hidden);

        Assert.Null(ArTelemetry.NextLog(office, in office));
    }

    [Fact]
    public void Rapid_look_throttles_T2_ar_change()
    {
        var lastLogAt = -999f;
        var hidden = default(ArOverlaySnapshot);
        var edge = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.Edge, ArMarkerPlace.Hidden);
        var onObject = new ArOverlaySnapshot(
            ArMarkerPlace.Hidden, ArMarkerPlace.OnObject, ArMarkerPlace.Hidden);

        Assert.Equal(
            "T2 ar init: loco=— office=— pin=—",
            ArTelemetry.NextLog(previous: null, in hidden, nowSeconds: 10f, ref lastLogAt));

        Assert.Null(
            ArTelemetry.NextLog(hidden, in edge, nowSeconds: 11f, ref lastLogAt));
        Assert.Equal(10f, lastLogAt);

        Assert.Equal(
            "T2 ar change: loco=— office=object pin=—",
            ArTelemetry.NextLog(edge, in onObject, nowSeconds: 13f, ref lastLogAt));
    }

    [Fact]
    public void Buffer_snapshot_feeds_T2_fields()
    {
        var slots = ArMarkerBuffer.Create();
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            100f,
            80f,
            ArMarkerPlace.OnObject,
            12);

        var snap = ArMarkerBuffer.Snapshot(slots);
        Assert.Equal(ArMarkerPlace.OnObject, snap.Loco);
        Assert.Equal(ArMarkerPlace.Hidden, snap.Station);
        Assert.Equal(ArMarkerPlace.Hidden, snap.Pin);
        Assert.Equal(
            "T2 ar init: loco=object office=— pin=—",
            ArTelemetry.NextLog(previous: null, in snap));
    }

    [Fact]
    public void In_yard_office_object_uses_ascii_stn_glyph()
    {
        Assert.Equal("STN", ArMarkerDisplay.Glyph(ArWaypointKind.Station));
        Assert.True(ArMarkerDisplay.IsImguiFontSafe("STN"));
    }

    [Fact]
    public void Glyphs_are_imgui_font_safe_ascii()
    {
        Assert.Equal("LOCO", ArMarkerDisplay.Glyph(ArWaypointKind.Loco));
        Assert.Equal("STN", ArMarkerDisplay.Glyph(ArWaypointKind.Station));
        Assert.Equal("PIN", ArMarkerDisplay.Glyph(ArWaypointKind.Pin));
        Assert.True(ArMarkerDisplay.IsImguiFontSafe(ArMarkerDisplay.Glyph(ArWaypointKind.Loco)));
        Assert.True(ArMarkerDisplay.IsImguiFontSafe(ArMarkerDisplay.Glyph(ArWaypointKind.Station)));
        Assert.True(ArMarkerDisplay.IsImguiFontSafe(ArMarkerDisplay.Glyph(ArWaypointKind.Pin)));
        Assert.False(ArMarkerDisplay.IsImguiFontSafe("⌂"));
    }

    [Fact]
    public void Ar_summary_counts_hud_top_separately()
    {
        var hist = default(ArPlacementHistogram);
        var slots = ArMarkerBuffer.Create();
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            28f,
            28f,
            ArMarkerPlace.Edge,
            10,
            ArHorizontalEdge.Left,
            0f);
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            28f,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Left,
            0f);

        ArPlacementStats.Record(slots, screenHeight: 600f, now: 10f, ref hist);

        Assert.Equal(1, hist.EdgeTop);
        Assert.Equal(1, hist.EdgeMid);
        Assert.Equal(1, hist.Hidden);
        Assert.Null(ArPlacementStats.MaybeSummary(now: 39.9f, force: false, ref hist));

        var line = ArPlacementStats.MaybeSummary(now: 40f, force: false, ref hist);
        Assert.Equal("T2 ar-summary: n=3 object=0 edgeMid=1 edgeTop=1 hidden=1", line);
        Assert.Equal(0, hist.EdgeTop);
    }
}
