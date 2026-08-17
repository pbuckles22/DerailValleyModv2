using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (3.2): loco=edge and office=edge shared one pixel.
/// Fan inward on that side so STN and LOCO stay readable. Mid-height; not a top bar.
/// </summary>
public class ArEdgeStackLayoutTests
{
    [Fact]
    public void Both_edge_markers_on_same_side_do_not_share_gui_x()
    {
        var slots = ArMarkerBuffer.Create();
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        var midGuiY = ArMarkerProjection.ToGuiY(600f * 0.5f, 600f);
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            leftX,
            midGuiY,
            ArMarkerPlace.Edge,
            distanceMeters: 20,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.1f));
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            leftX,
            midGuiY,
            ArMarkerPlace.Edge,
            distanceMeters: 40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.4f));

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f);

        var loco = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)];
        var office = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)];
        Assert.Equal(ArMarkerPlace.Edge, loco.Place);
        Assert.Equal(ArMarkerPlace.Edge, office.Place);
        Assert.NotEqual(loco.GuiX, office.GuiX);
        Assert.Equal(midGuiY, loco.GuiY);
        Assert.Equal(midGuiY, office.GuiY);
        Assert.True(Math.Abs(loco.GuiX - office.GuiX) >= ArEdgeStackLayout.DefaultSeparationPixels - 0.5f);
    }

    [Fact]
    public void Single_edge_marker_stays_at_outermost()
    {
        var slots = ArMarkerBuffer.Create();
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            12,
            ArHorizontalEdge.Left,
            0f);

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f);

        Assert.Equal(leftX, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
    }

    [Fact]
    public void On_object_markers_are_not_moved()
    {
        var slots = ArMarkerBuffer.Create();
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            400f,
            200f,
            ArMarkerPlace.OnObject,
            8);
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            410f,
            210f,
            ArMarkerPlace.OnObject,
            30);

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f);

        Assert.Equal(400f, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)].GuiX);
        Assert.Equal(410f, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
    }

    [Fact]
    public void AssignStackedXs_left_edge_extreme_stays_outer()
    {
        var keys = new[]
        {
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.1f),
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.4f),
        };
        var xs = new[] { 0f, 0f };
        ArEdgeStackLayout.AssignStackedXs(
            ArHorizontalEdge.Left,
            outermostX: 28f,
            ArEdgeStackLayout.DefaultSeparationPixels,
            keys,
            xs);

        Assert.Equal(28f + ArEdgeStackLayout.DefaultSeparationPixels, xs[0]);
        Assert.Equal(28f, xs[1]);
    }

    [Fact]
    public void AssignStackedXs_right_edge_extreme_stays_outer()
    {
        var keys = new[]
        {
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Right, 0.5f),
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Right, 0.1f),
        };
        var xs = new[] { 0f, 0f };
        ArEdgeStackLayout.AssignStackedXs(
            ArHorizontalEdge.Right,
            outermostX: 772f,
            ArEdgeStackLayout.DefaultSeparationPixels,
            keys,
            xs);

        Assert.Equal(772f, xs[0]);
        Assert.Equal(772f - ArEdgeStackLayout.DefaultSeparationPixels, xs[1]);
    }

    [Fact]
    public void Opposite_edges_are_not_stacked_together()
    {
        var slots = ArMarkerBuffer.Create();
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            28f,
            300f,
            ArMarkerPlace.Edge,
            10,
            ArHorizontalEdge.Left,
            0f);
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            772f,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Right,
            0f);

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f);

        Assert.Equal(28f, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)].GuiX);
        Assert.Equal(772f, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
    }

    [Fact]
    public void Three_edge_markers_on_left_fan_inward()
    {
        var slots = ArMarkerBuffer.Create();
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            10,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.2f));
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.5f));
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Pin)],
            ArWaypointKind.Pin,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            5,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.05f));

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f);

        var sep = ArEdgeStackLayout.DefaultSeparationPixels;
        Assert.Equal(leftX + sep, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)].GuiX);
        Assert.Equal(leftX, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
        Assert.Equal(leftX + 2f * sep, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Pin)].GuiX);
    }

    [Fact]
    public void Stack_does_not_pull_mid_edge_into_hud_band()
    {
        var slots = ArMarkerBuffer.Create();
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        var midGuiY = 300f;
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            leftX,
            28f,
            ArMarkerPlace.Edge,
            10,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.4f));
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            leftX,
            midGuiY,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.1f));

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, screenHeight: 600f);

        var loco = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)];
        var office = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)];
        Assert.Equal(28f, loco.GuiY);
        Assert.Equal(midGuiY, office.GuiY);
        Assert.Equal(leftX, office.GuiX);
        Assert.Equal(ArEdgeBand.Top, ArEdgeBanding.ClassifyGuiY(loco.GuiY, 600f));
        Assert.Equal(ArEdgeBand.Mid, ArEdgeBanding.ClassifyGuiY(office.GuiY, 600f));
    }
}
