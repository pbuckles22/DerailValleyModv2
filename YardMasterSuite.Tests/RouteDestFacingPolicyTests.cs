using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Smoke 8.7 leftover: at B4L Set dest, dest is crow-flies behind but after
/// reverse through the pin it is ahead (Set Forward into TT).
/// </summary>
public class RouteDestFacingPolicyTests
{
    [Fact]
    public void Smoke_8_7_B4L_pin_reverse_dest_crow_flies_behind_is_Set_Forward()
    {
        Assert.False(RouteDestFacingPolicy.DestNeedsReverse(
            pinNeedsReverse: true,
            destCrowFliesBehind: true));
        Assert.Equal(
            SwitchListDriveFacing.Forward,
            SwitchListDriveFacing.SetWord(
                RouteDestFacingPolicy.DestNeedsReverse(true, true)));
    }

    [Fact]
    public void Pin_ahead_dest_behind_is_still_Set_Reverse()
    {
        Assert.True(RouteDestFacingPolicy.DestNeedsReverse(
            pinNeedsReverse: false,
            destCrowFliesBehind: true));
    }

    [Fact]
    public void Pin_ahead_dest_ahead_is_Set_Forward()
    {
        Assert.False(RouteDestFacingPolicy.DestNeedsReverse(
            pinNeedsReverse: false,
            destCrowFliesBehind: false));
    }
}
