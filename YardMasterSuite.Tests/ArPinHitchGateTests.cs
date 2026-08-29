using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke harvest (8.7 / 2.8.7.29): desk-off frog/CLEARED still scored
/// <c>feature=11–17</c> from pin WorldToScreen + OnGUI Layout/CalcSize.
/// </summary>
public class ArPinHitchGateTests
{
    [Fact]
    public void Smoke_8_7_frog_cleared_throttles_pin_project_in_cab()
    {
        Assert.True(ArPinHitchGate.ShouldThrottleProject(
            boardedLoco: true,
            routePinOccupied: true));
        Assert.False(ArPinHitchGate.ShouldThrottleProject(
            boardedLoco: false,
            routePinOccupied: true));
        Assert.False(ArPinHitchGate.ShouldThrottleProject(
            boardedLoco: true,
            routePinOccupied: false));
    }

    [Fact]
    public void Smoke_8_7_throttled_pin_skips_project_inside_interval()
    {
        Assert.False(ArPinHitchGate.ShouldProject(
            throttleCabRoutePin: true,
            identityChanged: false,
            secondsSinceProject: 0.04f));
        Assert.True(ArPinHitchGate.ShouldProject(
            throttleCabRoutePin: true,
            identityChanged: false,
            secondsSinceProject: ArPinHitchGate.ProjectIntervalSeconds));
    }

    [Fact]
    public void Smoke_8_7_cleared_caption_projects_immediately()
    {
        Assert.True(ArPinHitchGate.ShouldProject(
            throttleCabRoutePin: true,
            identityChanged: true,
            secondsSinceProject: 0f));
    }

    [Fact]
    public void Smoke_8_7_on_foot_park_pin_projects_every_frame()
    {
        Assert.True(ArPinHitchGate.ShouldProject(
            throttleCabRoutePin: false,
            identityChanged: false,
            secondsSinceProject: 0f));
    }

    [Fact]
    public void Smoke_8_7_pin_ongui_skips_layout_pass()
    {
        Assert.False(ArPinHitchGate.ShouldRunOnGuiPass(eventIsRepaint: false));
        Assert.True(ArPinHitchGate.ShouldRunOnGuiPass(eventIsRepaint: true));
    }

    [Fact]
    public void Smoke_8_7_pin_ongui_measures_only_on_caption_or_screen_change()
    {
        Assert.False(ArPinHitchGate.ShouldRemeasureCaptions(
            captionDirty: false,
            screenSizeChanged: false));
        Assert.True(ArPinHitchGate.ShouldRemeasureCaptions(
            captionDirty: true,
            screenSizeChanged: false));
        Assert.True(ArPinHitchGate.ShouldRemeasureCaptions(
            captionDirty: false,
            screenSizeChanged: true));
    }

    [Fact]
    public void ObserveThrottle_emits_on_edge_only()
    {
        var was = false;
        Assert.Equal(
            "T2 ar-pin: hitch throttle",
            ArPinHitchGate.ObserveThrottle(throttle: true, ref was));
        Assert.True(was);
        Assert.Null(ArPinHitchGate.ObserveThrottle(throttle: true, ref was));
        Assert.Equal(
            "T2 ar-pin: hitch full",
            ArPinHitchGate.ObserveThrottle(throttle: false, ref was));
        Assert.False(was);
    }

    [Fact]
    public void PinWorldMoved_uses_five_cm_epsilon()
    {
        Assert.False(ArPinHitchGate.PinWorldMoved(0f, 0f, 0f, 0.04f, 0f, 0f));
        Assert.True(ArPinHitchGate.PinWorldMoved(0f, 0f, 0f, 0.06f, 0f, 0f));
    }

    [Fact]
    public void Hitch_gate_does_not_allocate()
    {
        var was = false;
        ArPinHitchGate.ShouldProject(true, false, 0.1f);
        ArPinHitchGate.ObserveThrottle(true, ref was);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++)
        {
            ArPinHitchGate.ShouldThrottleProject(true, true);
            ArPinHitchGate.ShouldProject(true, false, 0.04f);
            ArPinHitchGate.ShouldProject(true, true, 0f);
            ArPinHitchGate.ShouldRunOnGuiPass(eventIsRepaint: i % 2 == 0);
            ArPinHitchGate.ShouldRemeasureCaptions(false, false);
            ArPinHitchGate.PinWorldMoved(0f, 0f, 0f, 0.01f, 0f, 0f);
            ArPinHitchGate.ObserveThrottle(true, ref was);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
