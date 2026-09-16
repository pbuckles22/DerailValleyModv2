using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class SwitchListJobPickPolicyTests
{
    [Fact]
    public void Smoke_desk_refresh_lists_available_even_when_auto_hold_is_off()
    {
        Assert.False(SmokeJobHoldGate.Enabled);
        Assert.True(SwitchListJobPickPolicy.ShouldAddAvailableBoardJobs(autoHoldEnabled: false));
        Assert.True(SwitchListJobPickPolicy.ShouldAddAvailableBoardJobs(autoHoldEnabled: true));
    }
}
