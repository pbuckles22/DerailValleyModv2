using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Cab 2.16.35: after frog CLEARED, <c>step 4/5 Drive F &gt;SW-B4L req=25</c>
/// held until <c>T2 route: dest-yard behind</c>. Fully in means the tail is
/// 5 m past the dest entry, not the bumper and not the lookahead end.
/// </summary>
public class RouteFinalLegStopTests
{
    [Fact]
    public void Smoke_16_36_b4l_final_drive_is_fully_in_when_the_tail_clears_the_entry()
    {
        const int approach = 1;
        const int dest = 200;
        const int leftover = 300;
        var segs = new[]
        {
            new PathSegmentAlong(0f, 0f, 0f, 0f, 0f, 1f, 100f, trackId: approach),
            new PathSegmentAlong(100f, 0f, 0f, 100f, 0f, 1f, 80f, trackId: dest),
            new PathSegmentAlong(180f, 0f, 0f, 180f, 0f, 1f, 1500f, trackId: leftover),
        };

        const float consist = 40f;
        const float inside = 130f;
        var signed = PostedPathAheadGate.SignedMetersToEntry(inside, dest, segs, 3);
        var stall = RouteCommandExecutor.StallRemainingMeters(signed, consist);
        var pathEnd = PostedPathAheadGate.RemainingToEnd(inside, segs, 3);

        Assert.Equal(-30f, signed);
        Assert.Equal(15f, stall);
        Assert.Null(RouteCommandExecutor.StallRemainingMeters(signed, 0f));
        Assert.True(pathEnd > YardApproachKinematics.CruiseBeyondM + RouteCommandExecutor.DestEndPadMeters);

        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-B4L") };
        var cruise = RouteCommandExecutor.Begin(cmds);
        var held = RouteCommandExecutor.Tick(cmds, ref cruise, Input(pathEnd, 25f, consist, aimIsStall: false));
        Assert.Equal(RouteExecAction.Drive, held.Action);
        Assert.Equal(YardApproachKinematics.CruiseSpeedKmh, held.RequestKmh);

        var taper = RouteCommandExecutor.Begin(cmds);
        var onSpur = RouteCommandExecutor.Tick(cmds, ref taper, Input(stall, 25f, consist, aimIsStall: true));
        Assert.Equal(RouteExecAction.Drive, onSpur.Action);
        Assert.True(onSpur.RequestKmh < YardApproachKinematics.CruiseSpeedKmh);

        var nosePast = consist + RouteCommandExecutor.StallMarginMeters;
        var inStall = RouteCommandExecutor.StallRemainingMeters(-nosePast, consist);
        Assert.Equal(0f, inStall);
        var stopping = RouteCommandExecutor.Begin(cmds);
        var atClear = RouteCommandExecutor.Tick(cmds, ref stopping, Input(inStall, 20f, consist, aimIsStall: true));
        Assert.Equal(RouteExecAction.Brake, atClear.Action);
        Assert.Equal(RouteCommandExecutor.ReasonArriving, atClear.Reason);
        var done = RouteCommandExecutor.Tick(cmds, ref stopping, Input(inStall, 0f, consist, aimIsStall: true));
        Assert.Equal(RouteExecAction.Done, done.Action);

        var waiting = RouteCommandExecutor.Begin(cmds);
        var noLength = RouteCommandExecutor.Tick(cmds, ref waiting, Input(null, 25f, 0f, aimIsStall: false));
        Assert.Equal(RouteExecAction.Brake, noLength.Action);
        Assert.Equal(RouteCommandExecutor.ReasonWaitRemaining, noLength.Reason);

        var cut = RouteCommandExecutor.Begin(cmds);
        var atCar = RouteCommandExecutor.Tick(
            cmds,
            ref cut,
            Input(80f, 20f, consist, aimIsStall: true, car: 10f));
        Assert.Equal(RouteExecAction.Drive, atCar.Action);
        Assert.Equal(YardApproachKinematics.TouchdownSpeedKmh, atCar.RequestKmh);
    }

    [Fact]
    public void Smoke_16_36_1_far_dest_cruises_on_path_end_until_the_entry_is_known()
    {
        const float pathEnd = 3900f;
        const float consist = 7f;
        Assert.Null(RouteCommandExecutor.StallRemainingMeters(null, consist));
        Assert.Equal(pathEnd, RouteCommandExecutor.DriveRemainingMeters(null, pathEnd));

        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-C4S") };
        var far = RouteCommandExecutor.Begin(cmds);
        var cruise = RouteCommandExecutor.Tick(cmds, ref far, Input(pathEnd, 0f, consist, aimIsStall: false));
        Assert.Equal(RouteExecAction.Drive, cruise.Action);
        Assert.Equal(YardApproachKinematics.CruiseSpeedKmh, cruise.RequestKmh);
        Assert.NotEqual(RouteCommandExecutor.ReasonWaitRemaining, cruise.Reason);

        var stall = RouteCommandExecutor.StallRemainingMeters(1500f, consist);
        Assert.Equal(stall, RouteCommandExecutor.DriveRemainingMeters(stall, pathEnd));
        Assert.Null(RouteCommandExecutor.DriveRemainingMeters(null, null));
    }

    private static RouteExecInput Input(
        float? remaining,
        float speed,
        float consist,
        bool aimIsStall,
        float? car = null) =>
        new(
            hasPlan: true,
            hasDispatcher: true,
            phase: RouteClearancePhase.Cleared,
            pinJunctionId: null,
            remToClearedMeters: null,
            remainingMeters: remaining,
            speedKmh: speed,
            consistLengthMeters: consist,
            carClearanceMeters: car,
            aimIsStall: aimIsStall);
}
