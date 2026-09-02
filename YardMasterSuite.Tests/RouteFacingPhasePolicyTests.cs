using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteFacingPhasePolicyTests
{
    [Fact]
    public void Smoke_C1O_after_cleared_uses_dest_reverse_not_pin_forward()
    {
        // Pin approached forward (reverse=0); dest spur behind ⇒ Set Reverse into C1O.
        var needsReverse = RouteFacingPhasePolicy.FacingNeedsReverse(
            RouteClearancePhase.Cleared,
            pinArmedForClearance: true,
            pinLatched: true,
            pinTravelReverse: false,
            pinBehindLive: false,
            destBehindLive: true);
        Assert.True(needsReverse);
    }

    [Fact]
    public void Smoke_before_cleared_faces_toward_pin()
    {
        var needsReverse = RouteFacingPhasePolicy.FacingNeedsReverse(
            RouteClearancePhase.Approaching,
            pinArmedForClearance: true,
            pinLatched: true,
            pinTravelReverse: false,
            pinBehindLive: false,
            destBehindLive: true);
        Assert.False(needsReverse);
    }

    [Fact]
    public void Smoke_pin_reverse_latch_makes_dest_forward_after_frog()
    {
        var needsReverse = RouteFacingPhasePolicy.FacingNeedsReverse(
            RouteClearancePhase.Cleared,
            pinArmedForClearance: true,
            pinLatched: true,
            pinTravelReverse: true,
            pinBehindLive: true,
            destBehindLive: true);
        Assert.False(needsReverse);
    }
}
