using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

[Collection("StaticSessions")]
public class YmsRouteSessionsTests
{
    public YmsRouteSessionsTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_9_1_world_leave_clears_dest_so_pid_stays_unarmed()
    {
        RouteDestSession.Set("SW", "#Y-#S1774#T");
        SwitchListSession.Bind(
            "route:SW",
            new[]
            {
                new SwitchListStep(0, SwitchListStepKind.Transit, "SW", "#Y-#S1774#T", "Past switch"),
            });
        Assert.True(RouteDestSession.HasDestination);
        Assert.True(SwitchListSession.HasActive);
        Assert.True(PidSpeedArm.IsArmed(
            RouteDestSession.HasDestination,
            SwitchListSession.HasActive && !SwitchListSession.IsComplete,
            facingReady: true));

        YmsRouteSessions.ClearAll();

        Assert.False(RouteDestSession.HasDestination);
        Assert.False(SwitchListSession.HasActive);
        Assert.False(PidSpeedArm.IsArmed(
            RouteDestSession.HasDestination,
            switchListActiveIncomplete: false,
            facingReady: true));
    }
}

[Collection("StaticSessions")]
public class MapsDeskDefaultsTests
{
    [Fact]
    public void Smoke_desk_defaults_to_SW_Turntable_when_no_session()
    {
        var yards = new[] { "CME", "SW", "HB" };
        Assert.Equal(1, MapsDeskDefaults.ResolveYardIndex(yards, sessionYardId: null, currentIndex: 0));
        var tracks = MapsTurntableDest.WithTokenFirst(new[] { "SW-B3I", "SW-B4L" });
        Assert.Equal(0, MapsDeskDefaults.ResolveTrackIndex(tracks, sessionTrackId: null, currentIndex: 2));
        Assert.True(MapsTurntableDest.IsToken(tracks[0]));
    }

    [Fact]
    public void Smoke_desk_keeps_session_city_over_SW_default()
    {
        var yards = new[] { "CME", "SW", "HB" };
        Assert.Equal(0, MapsDeskDefaults.ResolveYardIndex(yards, sessionYardId: "CME", currentIndex: 1));
    }
}
