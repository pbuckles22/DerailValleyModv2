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

        var occ = ArEdgeStackLayout.OccupancyWidthPixels(
            ArMarkerDisplay.IconPixels,
            ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Station));
        Assert.Equal(
            ArEdgeStackLayout.OutermostCenterX(
                ArHorizontalEdge.Left,
                leftX,
                800f,
                occ),
            slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
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

        var locoOcc = ArEdgeStackLayout.OccupancyWidthPixels(
            ArMarkerDisplay.IconPixels,
            ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Loco));
        var stnOcc = ArEdgeStackLayout.OccupancyWidthPixels(
            ArMarkerDisplay.IconPixels,
            ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Station));
        Assert.Equal(
            ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, 28f, 800f, locoOcc),
            slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)].GuiX);
        Assert.Equal(
            ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Right, 28f, 800f, stnOcc),
            slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
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

        var icon = ArMarkerDisplay.IconPixels;
        var stn = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Station));
        var loco = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Loco));
        var pin = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Pin));
        var x0 = ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, leftX, 800f, stn);
        Assert.Equal(x0, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX);
        var x1 = x0 + ArEdgeStackLayout.CenterSeparationPixels(stn, loco);
        Assert.Equal(x1, slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)].GuiX);
        Assert.Equal(
            x1 + ArEdgeStackLayout.CenterSeparationPixels(loco, pin),
            slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Pin)].GuiX);
    }

    [Fact]
    public void Four_edge_markers_fan_inward_with_radar()
    {
        var slots = new ArMarkerSlot[4];
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[0],
            ArWaypointKind.Station,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.5f));
        ArMarkerBuffer.Show(
            ref slots[1],
            ArWaypointKind.Loco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            10,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.2f));
        ArMarkerBuffer.Show(
            ref slots[2],
            ArWaypointKind.Pin,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            5,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.05f));
        ArMarkerBuffer.Show(
            ref slots[3],
            ArWaypointKind.OtherLoco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            80,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, 0.1f));

        var widths = new[] { 36f, 40f, 36f, 48f };
        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: widths);

        var icon = ArMarkerDisplay.IconPixels;
        var occ0 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[0]);
        var occ1 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[1]);
        var occ2 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[2]);
        var occ3 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[3]);
        var x = ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, leftX, 800f, occ0);
        Assert.Equal(x, slots[0].GuiX);
        x += ArEdgeStackLayout.CenterSeparationPixels(occ0, occ1);
        Assert.Equal(x, slots[1].GuiX);
        x += ArEdgeStackLayout.CenterSeparationPixels(occ1, occ2);
        Assert.Equal(x, slots[2].GuiX);
        x += ArEdgeStackLayout.CenterSeparationPixels(occ2, occ3);
        Assert.Equal(x, slots[3].GuiX);
        Assert.False(
            ArEdgeStackLayout.CaptionsOverlap(
                slots[2].GuiX,
                widths[2],
                slots[3].GuiX,
                widths[3]));
    }

    [Fact]
    public void Smoke_edge_radar_captions_do_not_overlap()
    {
        var slots = new ArMarkerSlot[3];
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[0],
            ArWaypointKind.Station,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.5f));
        ArMarkerBuffer.Show(
            ref slots[1],
            ArWaypointKind.OtherLoco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            93,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.2f));
        ArMarkerBuffer.Show(
            ref slots[2],
            ArWaypointKind.OtherLoco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            56,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, 0.1f));

        var widths = new[] { 36f, 48f, 52f };
        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: widths);

        Assert.False(
            ArEdgeStackLayout.CaptionsOverlap(
                slots[0].GuiX, widths[0], slots[1].GuiX, widths[1]));
        Assert.False(
            ArEdgeStackLayout.CaptionsOverlap(
                slots[1].GuiX, widths[1], slots[2].GuiX, widths[2]));
        var icon = ArMarkerDisplay.IconPixels;
        var occ0 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[0]);
        var occ1 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[1]);
        Assert.Equal(
            ArEdgeStackLayout.CenterSeparationPixels(occ0, occ1),
            Math.Abs(slots[0].GuiX - slots[1].GuiX),
            1);
        Assert.True(
            Math.Abs(slots[0].GuiX - slots[1].GuiX) < 100f,
            "measured two-line captions must pack tighter than the old 200 px radar width");
        var left = slots[0].GuiX - (occ0 * 0.5f);
        Assert.True(left >= -0.5f, "leftmost caption must stay on screen");
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
        var stnOcc = ArEdgeStackLayout.OccupancyWidthPixels(
            ArMarkerDisplay.IconPixels,
            ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Station));
        Assert.Equal(
            ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, leftX, 800f, stnOcc),
            office.GuiX);
        Assert.Equal(ArEdgeBand.Top, ArEdgeBanding.ClassifyGuiY(loco.GuiY, 600f));
        Assert.Equal(ArEdgeBand.Mid, ArEdgeBanding.ClassifyGuiY(office.GuiY, 600f));
    }

    [Fact]
    public void Smoke_heading_only_sticky_row_edge_pair_still_fans()
    {
        var slots = ArMarkerBuffer.Create();
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        var stackBottom = MonitorHudStackLayout.StackBottomGuiY(false, false, false);
        var stickyGuiY = ArStickyRowPlacement.ResolveSlotGuiY(
            ArMarkerPlace.Edge, 300f, stackBottom, iconPixels: 28f);
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            leftX,
            stickyGuiY,
            ArMarkerPlace.Edge,
            distanceMeters: 20,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.1f));
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            leftX,
            stickyGuiY,
            ArMarkerPlace.Edge,
            distanceMeters: 40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.4f));

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, screenHeight: 600f, hudBottomGuiY: stackBottom);

        var loco = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)];
        var office = slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)];
        Assert.NotEqual(loco.GuiX, office.GuiX);
        Assert.Equal(stickyGuiY, loco.GuiY);
        Assert.Equal(stickyGuiY, office.GuiY);
        Assert.True(Math.Abs(loco.GuiX - office.GuiX) >= ArEdgeStackLayout.DefaultSeparationPixels - 0.5f);
        Assert.Equal(ArEdgeBand.Mid, ArEdgeBanding.ClassifyGuiY(stickyGuiY, 600f, stackBottom));
    }

    [Fact]
    public void Occupancy_uses_wider_of_icon_and_caption()
    {
        Assert.Equal(28f, ArEdgeStackLayout.InnerOccupancyWidthPixels(28f, 20f));
        Assert.Equal(48f, ArEdgeStackLayout.InnerOccupancyWidthPixels(28f, 48f));
        Assert.Equal(
            28f + ArMarkerPlate.HorizontalChromePixels,
            ArEdgeStackLayout.OccupancyWidthPixels(28f, 20f));
        Assert.Equal(
            48f + ArMarkerPlate.HorizontalChromePixels,
            ArEdgeStackLayout.OccupancyWidthPixels(28f, 48f));
        Assert.Equal(0f, ArEdgeStackLayout.OccupancyWidthPixels(-4f, -1f));
    }

    [Fact]
    public void Center_separation_is_end_plus_pad_plus_start()
    {
        Assert.Equal(36f, ArEdgeStackLayout.CenterSeparationPixels(40f, 24f, padPixels: 4f));
        Assert.Equal(
            44f,
            ArEdgeStackLayout.CenterSeparationPixels(36f, 36f));
    }

    [Fact]
    public void EstimateCaptionWidth_uses_longest_line()
    {
        Assert.Equal(27f, ArEdgeStackLayout.EstimateCaptionWidthPixels("DE2\n93m", pixelsPerChar: 9f));
        Assert.Equal(36f, ArEdgeStackLayout.EstimateCaptionWidthPixels("S060\n156m", pixelsPerChar: 9f));
        Assert.Equal(0f, ArEdgeStackLayout.EstimateCaptionWidthPixels(null));
    }

    [Fact]
    public void Smoke_left_edge_caption_does_not_run_off_screen()
    {
        var slots = new ArMarkerSlot[1];
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[0],
            ArWaypointKind.OtherLoco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            56,
            ArHorizontalEdge.Left,
            0f);

        var widths = new[] { 200f };
        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: widths);

        var occ = ArEdgeStackLayout.OccupancyWidthPixels(ArMarkerDisplay.IconPixels, widths[0]);
        Assert.Equal(
            ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, leftX, 800f, occ),
            slots[0].GuiX);
        Assert.True(slots[0].GuiX - (occ * 0.5f) >= -0.5f);
    }

    [Fact]
    public void Occupancy_layout_does_not_allocate()
    {
        var slots = new ArMarkerSlot[2];
        var widths = new[] { 36f, 48f };
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[0],
            ArWaypointKind.Station,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.5f));
        ArMarkerBuffer.Show(
            ref slots[1],
            ArWaypointKind.OtherLoco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            93,
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.2f));
        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: widths);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 200; i++)
        {
            ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: widths);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
