using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class ScreenOverlayHandlePolicyTests
{
    [Fact]
    public void Smoke_cab_drive_does_not_retry_overlay_fot_every_two_seconds()
    {
        Assert.False(
            ScreenOverlayHandlePolicy.ShouldLookup(
                havePopup: false,
                haveNotification: false,
                attempts: ScreenOverlayHandlePolicy.MaxLookupsPerWorld,
                now: 40f,
                nextRetryAt: 2f));
    }

    [Fact]
    public void First_world_frame_looks_up_handles()
    {
        Assert.True(
            ScreenOverlayHandlePolicy.ShouldLookup(
                havePopup: false,
                haveNotification: false,
                attempts: 0,
                now: 0f,
                nextRetryAt: 0f));
    }

    [Fact]
    public void Both_handles_skip_lookup()
    {
        Assert.False(
            ScreenOverlayHandlePolicy.ShouldLookup(
                havePopup: true,
                haveNotification: true,
                attempts: 0,
                now: 0f,
                nextRetryAt: 0f));
    }

    [Fact]
    public void One_retry_after_delay_then_stop()
    {
        Assert.False(
            ScreenOverlayHandlePolicy.ShouldLookup(
                havePopup: true,
                haveNotification: false,
                attempts: 1,
                now: 1f,
                nextRetryAt: 2f));
        Assert.True(
            ScreenOverlayHandlePolicy.ShouldLookup(
                havePopup: true,
                haveNotification: false,
                attempts: 1,
                now: 2f,
                nextRetryAt: 2f));
        Assert.False(
            ScreenOverlayHandlePolicy.ShouldLookup(
                havePopup: true,
                haveNotification: false,
                attempts: 2,
                now: 99f,
                nextRetryAt: 4f));
    }
}
