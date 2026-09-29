using Xunit;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.16.36.7: Front showed 74 m then the laser dropped on the curve (rem=?).
/// The car was still on the next hop of the plan.
/// </summary>
public class RouteCarAlongTrackTests
{
    [Fact]
    public void Smoke_16_run_a_car_around_the_curve_is_meters_along_the_plan()
    {
        var hops = new[] { 100f, 80f, 60f };

        Assert.True(RouteCarAlongTrack.TryMetersAhead(
            hops,
            hops.Length,
            leadHop: 0,
            leadInto: 90f,
            carHop: 1,
            carInto: 10f,
            towardPlanEnd: true,
            out var around));
        Assert.Equal(20f, around);

        Assert.False(RouteCarAlongTrack.TryMetersAhead(
            hops,
            hops.Length,
            leadHop: 0,
            leadInto: 90f,
            carHop: 0,
            carInto: 40f,
            towardPlanEnd: true,
            out _));

        Assert.True(RouteCarAlongTrack.TryMetersAhead(
            hops,
            hops.Length,
            leadHop: 1,
            leadInto: 10f,
            carHop: 0,
            carInto: 40f,
            towardPlanEnd: false,
            out var back));
        Assert.Equal(70f, back);
    }
}
