using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (planned 3.2): behind-camera office is an edge cue, not screen center.
/// </summary>
public class ArMarkerProjectionTests
{
    private const float W = 800f;
    private const float H = 600f;
    private const float Margin = 28f;

    [Fact]
    public void Look_behind_office_uses_horizontal_edge_not_screen_center()
    {
        float x = W * 0.5f;
        float y = H * 0.5f;
        ArMarkerProjection.ApplyBehindCameraHorizontalEdge(
            behindCamera: true,
            ArHorizontalEdge.Right,
            W,
            H,
            Margin,
            ref x,
            ref y);

        Assert.Equal(W - Margin, x);
        Assert.Equal(H * 0.5f, y);
        Assert.True(Math.Abs(x - W * 0.5f) > 50f);
    }

    [Fact]
    public void Ahead_on_screen_office_places_on_object()
    {
        Assert.True(ArMarkerProjection.ShouldPlaceOnObject(
            behindCamera: false, screenZ: 1f, screenX: 400f, screenY: 300f, W, H));
        Assert.False(ArMarkerProjection.ShouldPlaceOnObject(
            behindCamera: true, screenZ: 1f, screenX: 400f, screenY: 300f, W, H));
        Assert.False(ArMarkerProjection.ShouldPlaceOnObject(
            behindCamera: false, screenZ: -1f, screenX: 400f, screenY: 300f, W, H));
        Assert.False(ArMarkerProjection.ShouldPlaceOnObject(
            behindCamera: false, screenZ: 1f, screenX: -50f, screenY: 300f, W, H));
    }

    [Fact]
    public void IsBehindCamera_uses_forward_dot()
    {
        Assert.True(ArMarkerProjection.IsBehindCamera(0.05f));
        Assert.True(ArMarkerProjection.IsBehindCamera(-1f));
        Assert.False(ArMarkerProjection.IsBehindCamera(0.06f));
        Assert.False(ArMarkerProjection.IsBehindCamera(2f));
    }

    [Fact]
    public void Behind_hysteresis_holds_until_clearly_ahead()
    {
        Assert.True(ArMarkerProjection.IsBehindCameraHysteresis(0.2f, wasBehind: true));
        Assert.False(ArMarkerProjection.IsBehindCameraHysteresis(0.4f, wasBehind: true));
        Assert.False(ArMarkerProjection.IsBehindCameraHysteresis(0.2f, wasBehind: false));
        Assert.True(ArMarkerProjection.IsBehindCameraHysteresis(0.05f, wasBehind: false));
    }

    [Fact]
    public void ClampToScreen_keeps_insets()
    {
        Assert.True(ArMarkerProjection.ClampToScreen(-10f, 500f, W, H, 20f, out var x, out var y));
        Assert.Equal(20f, x);
        Assert.Equal(500f, y);

        Assert.False(ArMarkerProjection.ClampToScreen(400f, 300f, W, H, 20f, out x, out y));
        Assert.Equal(400f, x);
        Assert.Equal(300f, y);
    }

    [Fact]
    public void ToGuiY_flips_origin()
    {
        Assert.Equal(100f, ArMarkerProjection.ToGuiY(500f, 600f));
    }

    [Fact]
    public void ApplyBehindCameraEdge_right_lateral_hits_right_edge_not_center()
    {
        float x = W * 0.5f;
        float y = H * 0.5f;
        ArMarkerProjection.ApplyBehindCameraEdge(
            behindCamera: true,
            viewRight: 10f,
            viewUp: 0.1f,
            W,
            H,
            Margin,
            ref x,
            ref y);

        Assert.InRange(x, W - Margin - 1f, W - Margin + 1f);
        Assert.True(Math.Abs(x - W * 0.5f) > 50f);
    }

    [Fact]
    public void ApplyBehindCameraHorizontalEdge_noop_when_ahead()
    {
        float x = 123f;
        float y = 456f;
        ArMarkerProjection.ApplyBehindCameraHorizontalEdge(
            false, ArHorizontalEdge.Right, W, H, Margin, ref x, ref y);
        Assert.Equal(123f, x);
        Assert.Equal(456f, y);
    }
}

/// <summary>
/// Smoke harvest (planned 3.2): looking almost away must not L/R stutter.
/// </summary>
public class ArEdgeHysteresisTests
{
    [Fact]
    public void Look_away_holds_edge_through_small_yaw_wobble()
    {
        var side = ArHorizontalEdge.Left;
        side = ArEdgeHysteresis.Resolve(viewRight: 0.02f, viewForward: -1f, side);
        Assert.Equal(ArHorizontalEdge.Left, side);
        side = ArEdgeHysteresis.Resolve(viewRight: -0.02f, viewForward: -1f, side);
        Assert.Equal(ArHorizontalEdge.Left, side);

        side = ArEdgeHysteresis.Resolve(viewRight: 0.08f, viewForward: -1f, side);
        Assert.Equal(ArHorizontalEdge.Right, side);
    }

    [Fact]
    public void Edge_bearing_is_distance_independent()
    {
        var near = ArEdgeHysteresis.Resolve(0.087f, -5f, ArHorizontalEdge.Left);
        var far = ArEdgeHysteresis.Resolve(3.49f, -200f, ArHorizontalEdge.Left);
        Assert.Equal(near, far);
        Assert.Equal(ArHorizontalEdge.Left, near);
    }
}
