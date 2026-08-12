using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class YmsEventBusTests
{
    [Fact]
    public void ClearAllSubscriptions_is_safe_to_call_with_no_subscribers()
    {
        YmsEventBus.ClearAllSubscriptions();
    }
}
