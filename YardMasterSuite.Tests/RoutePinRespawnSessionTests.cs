using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class RoutePinRespawnSessionTests
{
    public RoutePinRespawnSessionTests()
    {
        YmsRouteSessions.ClearAll();
        RoutePinRespawnSession.Clear();
    }

    [Fact]
    public void Smoke_cleared_1_plus_4_respawns_caption_4_at_cached_xyz()
    {
        RoutePinRespawnSession.Arm("990152", "4", 10f, 2f, 30f, hasWorld: true);
        Assert.True(RoutePinRespawnSession.Active);
        Assert.Equal("990152", RoutePinRespawnSession.PinId);
        Assert.Equal("4", RoutePinRespawnSession.Caption);
        Assert.Equal(10f, RoutePinRespawnSession.X);
        Assert.Equal(30f, RoutePinRespawnSession.Z);
        RoutePinRespawnSession.Clear();
        Assert.False(RoutePinRespawnSession.Active);
    }
}
