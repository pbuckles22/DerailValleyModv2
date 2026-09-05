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

    [Fact]
    public void DetectEdge_left_right_and_none()
    {
        var margin = ArMarkerProjection.DefaultEdgeMarginPixels;
        const float width = 800f;
        var rightX = Math.Max(margin, width - margin);

        Assert.Equal(
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.DetectEdge(margin, width, margin));
        Assert.Equal(
            ArHorizontalEdge.Left,
            ArEdgeStackLayout.DetectEdge(
                margin + ArEdgeStackLayout.EdgeDetectTolerancePixels,
                width,
                margin));
        Assert.Equal(
            ArHorizontalEdge.Right,
            ArEdgeStackLayout.DetectEdge(rightX, width, margin));
        Assert.Equal(
            ArHorizontalEdge.Right,
            ArEdgeStackLayout.DetectEdge(
                rightX - ArEdgeStackLayout.EdgeDetectTolerancePixels,
                width,
                margin));
        Assert.Equal(
            ArHorizontalEdge.None,
            ArEdgeStackLayout.DetectEdge(width * 0.5f, width, margin));
        Assert.Equal(
            ArHorizontalEdge.None,
            ArEdgeStackLayout.DetectEdge(
                margin + ArEdgeStackLayout.EdgeDetectTolerancePixels + 1f,
                width,
                margin));
    }

    [Fact]
    public void CaptionSeparationPixels_glyph_and_radar_combinations()
    {
        var icon = ArMarkerDisplay.IconPixels;
        var stn = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Station));
        var loco = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Loco));
        var pin = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Pin));
        var radar = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.OtherLoco));
        var job = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.JobCar));

        Assert.Equal(
            ArEdgeStackLayout.CenterSeparationPixels(stn, loco),
            ArEdgeStackLayout.CaptionSeparationPixels(
                ArWaypointKind.Station, ArWaypointKind.Loco));
        Assert.Equal(
            ArEdgeStackLayout.CenterSeparationPixels(pin, pin),
            ArEdgeStackLayout.CaptionSeparationPixels(
                ArWaypointKind.Pin, ArWaypointKind.Pin));
        Assert.Equal(
            ArEdgeStackLayout.CenterSeparationPixels(radar, stn),
            ArEdgeStackLayout.CaptionSeparationPixels(
                ArWaypointKind.OtherLoco, ArWaypointKind.Station));
        Assert.Equal(
            ArEdgeStackLayout.CenterSeparationPixels(job, radar),
            ArEdgeStackLayout.CaptionSeparationPixels(
                ArWaypointKind.JobCar, ArWaypointKind.OtherLoco));
        Assert.True(
            ArEdgeStackLayout.CaptionSeparationPixels(
                ArWaypointKind.OtherLoco, ArWaypointKind.Loco)
            > ArEdgeStackLayout.CaptionSeparationPixels(
                ArWaypointKind.Station, ArWaypointKind.Loco));
    }

    [Fact]
    public void Apply_detects_edge_when_slot_edge_is_none()
    {
        var slots = ArMarkerBuffer.Create();
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)],
            ArWaypointKind.Station,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            40,
            ArHorizontalEdge.None,
            0f);
        ArMarkerBuffer.Show(
            ref slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)],
            ArWaypointKind.Loco,
            leftX,
            300f,
            ArMarkerPlace.Edge,
            10,
            ArHorizontalEdge.None,
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.2f));

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f);

        Assert.NotEqual(
            slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Station)].GuiX,
            slots[ArMarkerBuffer.SlotOf(ArWaypointKind.Loco)].GuiX);
    }

    [Fact]
    public void OutwardSortKey_none_is_zero_and_AssignStackedXs_none_copies()
    {
        Assert.Equal(0f, ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.None, 0.4f));

        var keys = new[] { 1f, 2f };
        var xs = new[] { 9f, 9f };
        ArEdgeStackLayout.AssignStackedXs(
            ArHorizontalEdge.None,
            outermostX: 42f,
            ArEdgeStackLayout.DefaultSeparationPixels,
            keys,
            xs);
        Assert.Equal(42f, xs[0]);
        Assert.Equal(42f, xs[1]);

        var emptyKeys = Array.Empty<float>();
        var emptyXs = Array.Empty<float>();
        ArEdgeStackLayout.AssignStackedXs(
            ArHorizontalEdge.Left,
            outermostX: 28f,
            ArEdgeStackLayout.DefaultSeparationPixels,
            emptyKeys,
            emptyXs);
    }

    [Fact]
    public void Apply_null_or_empty_slots_is_noop()
    {
        ArEdgeStackLayout.Apply(null!, screenWidth: 800f);
        ArEdgeStackLayout.Apply(Array.Empty<ArMarkerSlot>(), screenWidth: 800f);
    }

    [Fact]
    public void AssignStackedXs_rejects_null_or_short_outXs()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ArEdgeStackLayout.AssignStackedXs(
                ArHorizontalEdge.Left, 28f, 72f, null!, new float[1]));
        Assert.Throws<ArgumentNullException>(() =>
            ArEdgeStackLayout.AssignStackedXs(
                ArHorizontalEdge.Left, 28f, 72f, new float[1], null!));
        Assert.Throws<ArgumentException>(() =>
            ArEdgeStackLayout.AssignStackedXs(
                ArHorizontalEdge.Left, 28f, 72f, new float[2], new float[1]));
    }

    [Fact]
    public void AssignStackedXs_equal_keys_prefer_lower_index_outward()
    {
        var keys = new[] { 1f, 1f };
        var xs = new[] { 0f, 0f };
        ArEdgeStackLayout.AssignStackedXs(
            ArHorizontalEdge.Left,
            outermostX: 28f,
            separationPixels: 10f,
            keys,
            xs);
        Assert.Equal(28f, xs[0]);
        Assert.Equal(38f, xs[1]);
    }

    [Fact]
    public void Sixteen_edge_markers_full_fan_capacity_with_mixed_kinds()
    {
        const int n = 16;
        var slots = new ArMarkerSlot[n];
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        var kinds = new[]
        {
            ArWaypointKind.Station,
            ArWaypointKind.Loco,
            ArWaypointKind.Pin,
            ArWaypointKind.OtherLoco,
            ArWaypointKind.JobCar,
        };
        for (var i = 0; i < n; i++)
        {
            ArMarkerBuffer.Show(
                ref slots[i],
                kinds[i % kinds.Length],
                leftX,
                300f,
                ArMarkerPlace.Edge,
                10 + i,
                ArHorizontalEdge.Left,
                edgeSortKey: n - i);
        }

        var widths = new float[n];
        for (var i = 0; i < n; i++)
        {
            widths[i] = 30f + (i % 5);
        }

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: widths);

        var icon = ArMarkerDisplay.IconPixels;
        var prevOcc = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[0]);
        var x = ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, leftX, 800f, prevOcc);
        Assert.Equal(x, slots[0].GuiX);
        for (var i = 1; i < n; i++)
        {
            var occ = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[i]);
            x += ArEdgeStackLayout.CenterSeparationPixels(prevOcc, occ);
            Assert.Equal(x, slots[i].GuiX);
            Assert.False(
                ArEdgeStackLayout.CaptionsOverlap(
                    slots[i - 1].GuiX, widths[i - 1], slots[i].GuiX, widths[i]));
            prevOcc = occ;
        }
    }

    [Fact]
    public void Seventeen_edge_markers_caps_fan_at_sixteen_leaves_overflow_unmoved()
    {
        const int n = 17;
        var slots = new ArMarkerSlot[n];
        var leftX = ArMarkerProjection.DefaultEdgeMarginPixels;
        for (var i = 0; i < n; i++)
        {
            ArMarkerBuffer.Show(
                ref slots[i],
                ArWaypointKind.OtherLoco,
                leftX,
                300f,
                ArMarkerPlace.Edge,
                10,
                ArHorizontalEdge.Left,
                edgeSortKey: n - i);
        }

        var overflowX = leftX;
        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: null);

        Assert.Equal(overflowX, slots[16].GuiX);
        Assert.NotEqual(slots[0].GuiX, slots[1].GuiX);
        Assert.NotEqual(slots[14].GuiX, slots[15].GuiX);
    }

    [Fact]
    public void Right_edge_multi_kind_fan_steps_inward_with_overlap_free_captions()
    {
        var slots = new ArMarkerSlot[5];
        var margin = ArMarkerProjection.DefaultEdgeMarginPixels;
        const float width = 800f;
        var rightX = Math.Max(margin, width - margin);
        var kinds = new[]
        {
            ArWaypointKind.Station,
            ArWaypointKind.Loco,
            ArWaypointKind.Pin,
            ArWaypointKind.OtherLoco,
            ArWaypointKind.JobCar,
        };
        for (var i = 0; i < slots.Length; i++)
        {
            ArMarkerBuffer.Show(
                ref slots[i],
                kinds[i],
                rightX,
                300f,
                ArMarkerPlace.Edge,
                20 + i,
                ArHorizontalEdge.Right,
                ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Right, 0.5f - (i * 0.1f)));
        }

        var widths = new[] { 36f, 40f, 36f, 48f, 44f };
        ArEdgeStackLayout.Apply(slots, screenWidth: width, captionWidths: widths);

        var icon = ArMarkerDisplay.IconPixels;
        var occ0 = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[0]);
        var x = ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Right, margin, width, occ0);
        Assert.Equal(x, slots[0].GuiX);
        var prev = occ0;
        for (var i = 1; i < slots.Length; i++)
        {
            var occ = ArEdgeStackLayout.OccupancyWidthPixels(icon, widths[i]);
            x -= ArEdgeStackLayout.CenterSeparationPixels(prev, occ);
            Assert.Equal(x, slots[i].GuiX);
            Assert.False(
                ArEdgeStackLayout.CaptionsOverlap(
                    slots[i - 1].GuiX, widths[i - 1], slots[i].GuiX, widths[i]));
            prev = occ;
        }
    }

    [Fact]
    public void Apply_zero_caption_width_falls_back_to_kind_label()
    {
        var slots = new ArMarkerSlot[2];
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
            ArEdgeStackLayout.OutwardSortKey(ArHorizontalEdge.Left, -0.1f));

        ArEdgeStackLayout.Apply(slots, screenWidth: 800f, captionWidths: new[] { 0f, -1f });

        var icon = ArMarkerDisplay.IconPixels;
        var stn = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Station));
        var loco = ArEdgeStackLayout.OccupancyWidthPixels(
            icon, ArMarkerDisplay.LabelWidthPixels(ArWaypointKind.Loco));
        var x0 = ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, leftX, 800f, stn);
        Assert.Equal(x0, slots[0].GuiX);
        Assert.Equal(x0 + ArEdgeStackLayout.CenterSeparationPixels(stn, loco), slots[1].GuiX);
    }

    [Fact]
    public void Outermost_and_estimate_helpers_clamp_negatives()
    {
        Assert.Equal(
            10f,
            ArEdgeStackLayout.CenterSeparationPixels(10f, 10f, padPixels: -4f));
        Assert.Equal(
            0f,
            ArEdgeStackLayout.EstimateCaptionWidthPixels("ABC", pixelsPerChar: 0f));
        Assert.Equal(
            0f,
            ArEdgeStackLayout.EstimateCaptionWidthPixels("ABC", pixelsPerChar: -2f));
        var wide = ArEdgeStackLayout.OutermostCenterX(
            ArHorizontalEdge.Right, 28f, 100f, occupancy: 200f);
        Assert.True(wide <= 100f);
        Assert.Equal(
            28f,
            ArEdgeStackLayout.OutermostCenterX(ArHorizontalEdge.Left, 28f, 800f, occupancy: -8f));
    }
}
