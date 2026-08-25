using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 6.17 smoke harvest: 48px named PNGs + dark plate (v1 4.9).
/// </summary>
public class ArPngIconTests
{
    [Fact]
    public void Smoke_yard_markers_are_48px_named_pngs_with_dark_plate()
    {
        Assert.Equal(48f, ArMarkerDisplay.IconPixels);
        Assert.Equal("loco.png", ArPngIcons.FileName(ArWaypointKind.Loco));
        Assert.Equal("station.png", ArPngIcons.FileName(ArWaypointKind.Station));
        Assert.Equal("pin.png", ArPngIcons.FileName(ArWaypointKind.Pin));
        Assert.Equal("radar.png", ArPngIcons.FileName(ArWaypointKind.OtherLoco));
        Assert.Equal(string.Empty, ArPngIcons.FileName(ArWaypointKind.JobCar));
        Assert.Equal("Icons", ArPngIcons.FolderName);

        var inner = ArEdgeStackLayout.InnerOccupancyWidthPixels(48f, 64f);
        Assert.Equal(64f, inner);
        Assert.Equal(
            inner + ArMarkerPlate.HorizontalChromePixels,
            ArEdgeStackLayout.OccupancyWidthPixels(48f, 64f));
        Assert.True(ArEdgeStackLayout.OccupancyWidthPixels(48f, 40f) > 48f);
    }

    [Fact]
    public void Png_file_names_are_distinct()
    {
        var names = new[]
        {
            ArPngIcons.FileName(ArWaypointKind.Loco),
            ArPngIcons.FileName(ArWaypointKind.Station),
            ArPngIcons.FileName(ArWaypointKind.Pin),
            ArPngIcons.FileName(ArWaypointKind.OtherLoco),
        };
        Assert.Equal(4, names.Distinct().Count());
    }

    [Fact]
    public void Plate_height_covers_icon_and_caption()
    {
        Assert.Equal(
            48f + 22f + ArMarkerPlate.LabelGapY + (ArMarkerPlate.ExpandY * 2f),
            ArMarkerPlate.OuterHeightPixels(48f, 22f));
    }
}
