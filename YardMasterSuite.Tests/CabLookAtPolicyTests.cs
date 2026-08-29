using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CabLookAtPolicyTests
{
    [Fact]
    public void Smoke_8_7_cab_drive_skips_look_at_cast_when_boarded()
    {
        Assert.True(CabLookAtPolicy.SkipLookAtCast(boardedLoco: true));
        Assert.False(CabLookAtPolicy.SkipLookAtCast(boardedLoco: false));
    }

    [Fact]
    public void Smoke_8_7_cab_drive_hides_look_at_bar_when_boarded()
    {
        Assert.True(CabLookAtPolicy.HideLookAtBar(boardedLoco: true));
        Assert.False(CabLookAtPolicy.HideLookAtBar(boardedLoco: false));
    }
}
