using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class NextStationDisplayTests
{
    [Fact]
    public void Format_omits_when_fluids_ok_or_unknown()
    {
        Assert.Null(NextStationDisplay.Format(fluidsLow: false, "SW", 1500f));
        Assert.Null(NextStationDisplay.Format(fluidsLow: true, "SW", null));
        Assert.Null(NextStationDisplay.Format(fluidsLow: true, "  ", 1500f));
        Assert.Null(NextStationDisplay.Format(fluidsLow: true, null, 1500f));
    }

    [Fact]
    public void Format_km_chip_when_fluids_low()
    {
        Assert.Equal("Next: SW [1.5 km]", NextStationDisplay.Format(true, " SW ", 1500f));
    }

    [Fact]
    public void FluidsLow_follows_fluid_display_warn_band()
    {
        Assert.False(NextStationDisplay.FluidsLow(50f, 50f));
        Assert.True(NextStationDisplay.FluidsLow(FluidDisplay.WarningThresholdPercent - 1f, 50f));
        Assert.True(NextStationDisplay.FluidsLow(50f, FluidDisplay.WarningThresholdPercent - 1f));
        Assert.False(NextStationDisplay.FluidsLow(FluidDisplay.WarningThresholdPercent, 50f));
    }
}

public class RouteExitDisplayTests
{
    [Fact]
    public void Format_null_on_zero_delta()
    {
        Assert.Null(RouteExitDisplay.Format(0f, 0f, 0f, 0f));
    }

    [Fact]
    public void Format_cardinal_and_intercardinal()
    {
        Assert.Equal("Exit N", RouteExitDisplay.Format(0f, 0f, 0f, 10f));
        Assert.Equal("Exit E", RouteExitDisplay.Format(0f, 0f, 10f, 0f));
        Assert.Equal("Exit S", RouteExitDisplay.Format(0f, 0f, 0f, -10f));
        Assert.Equal("Exit W", RouteExitDisplay.Format(0f, 0f, -10f, 0f));
        Assert.Equal("Exit NE", RouteExitDisplay.Format(0f, 0f, 10f, 10f));
    }
}

public class TargetCarSelectionTests
{
    [Theory]
    [InlineData(false, false, TargetCarSource.None)]
    [InlineData(true, false, TargetCarSource.Standing)]
    [InlineData(false, true, TargetCarSource.LookAt)]
    [InlineData(true, true, TargetCarSource.LookAt)]
    public void Resolve_look_at_wins(bool standing, bool lookAt, TargetCarSource want) =>
        Assert.Equal(want, TargetCarSelection.Resolve(standing, lookAt));
}

public class HandbrakeDisplayTests
{
    [Fact]
    public void Applied_threshold_and_count()
    {
        Assert.False(HandbrakeDisplay.IsApplied(0.01f));
        Assert.True(HandbrakeDisplay.IsApplied(0.011f));
        Assert.Equal(2, HandbrakeDisplay.CountApplied(new[] { 0f, 0.02f, 1f }));
        Assert.Equal("— Handbrake", HandbrakeDisplay.FormatCount(null));
        Assert.Equal("Handbrake 1", HandbrakeDisplay.FormatCount(1));
        Assert.Equal("— Handbrakes", HandbrakeDisplay.FormatTotal(null));
        Assert.Equal("Handbrakes 3", HandbrakeDisplay.FormatTotal(3));
    }
}

public class BrakePipeDisplayTests
{
    [Fact]
    public void FormatBar_null_and_rounding()
    {
        Assert.Equal("— Pipe", BrakePipeDisplay.FormatBar(null));
        Assert.Equal("Pipe 5.0 bar", BrakePipeDisplay.FormatBar(5.04f));
        Assert.Equal("Pipe 5.1 bar", BrakePipeDisplay.FormatBar(5.06f));
    }
}

public class TonnageDisplayCoverageTests
{
    [Fact]
    public void Kilograms_and_car_consist_chips()
    {
        Assert.Equal(74f, TonnageDisplay.KilogramsToTonnes(74000f));
        Assert.Equal("— Mass", TonnageDisplay.FormatTonnes(null));
        Assert.Equal("Mass 74 t", TonnageDisplay.FormatFromKilograms(74000f));
        Assert.Equal("— Car", TonnageDisplay.FormatCarAndConsistFromKilograms(null, 74000f));
        Assert.Equal("Car 18 t", TonnageDisplay.FormatCarAndConsistFromKilograms(18000f, null));
        Assert.Equal("Car 18 t", TonnageDisplay.FormatCarAndConsistFromKilograms(18000f, 18000f));
        Assert.Equal(
            "Car 18 t  |  all cars 74 t",
            TonnageDisplay.FormatCarAndConsistFromKilograms(18000f, 74000f));
        Assert.Equal(int.MinValue, TonnageDisplay.BucketTonnes(null));
        Assert.Equal(74, TonnageDisplay.BucketTonnes(74.4f));
    }
}

public class HudStackLayoutTests
{
    [Fact]
    public void Publish_and_reset()
    {
        HudStackLayout.PublishLastBottomGuiY(120f);
        Assert.Equal(120f, HudStackLayout.LastBottomGuiY);
        HudStackLayout.PublishLastBottomGuiY(90f);
        Assert.Equal(90f, HudStackLayout.LastBottomGuiY);
        HudStackLayout.Reset();
        Assert.Equal(0f, HudStackLayout.LastBottomGuiY);
    }
}

public class RoutePlanReadyTests
{
    [Fact]
    public void Ctor_roundtrips_fields()
    {
        var plan = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "A", "B" },
            Array.Empty<PathJunctionEval>(),
            0,
            0,
            false,
            1f);
        var snap = new Dictionary<string, int> { ["j"] = 1 };
        var ready = new RoutePlanReady(3, plan, "A", "Exit N", 12f, snap, "T2 route", "set-dest");
        Assert.Equal(3, ready.Generation);
        Assert.Same(plan, ready.Plan);
        Assert.Equal("A", ready.OriginTrackId);
        Assert.Equal("Exit N", ready.ExitCue);
        Assert.Equal(12f, ready.TravelEtaSeconds);
        Assert.Same(snap, ready.JunctionSnapshot);
        Assert.Equal("T2 route", ready.LogLine);
        Assert.Equal("set-dest", ready.ComputeReason);
        var empty = new RoutePlanReady(0, null, null, null, null, null, null);
        Assert.Null(empty.Plan);
        Assert.Null(empty.ComputeReason);
    }
}
