using System.Collections.Generic;
using System.Linq;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// 16.2 ship 2 — Route-tab GO walks the <see cref="RouteCommandParser"/> queue:
/// drive to CLEARED, stop at rest, throw the pivot frog to the outbound branch,
/// flip direction, drive to the dest end, stop.
/// </summary>
[Collection("StaticSessions")]
public class RouteCommandExecutorTests
{
    private const string Pin = "J-pivot";

    private static readonly LocoCommand[] Sawtooth =
    {
        new(LocoCommandAction.Drive, "Stem", travelReverse: true),
        new(LocoCommandAction.Stop),
        new(LocoCommandAction.ThrowSwitch, Pin, targetIsJunction: true, requiredBranch: 1),
        new(LocoCommandAction.ChangeDirection, travelReverse: false),
        new(LocoCommandAction.Drive, "Dest", travelReverse: false),
    };

    private static RouteExecInput In(
        RouteClearancePhase phase = RouteClearancePhase.Approaching,
        string? pin = Pin,
        float? remToCleared = 40f,
        float? remaining = 300f,
        float speed = 0f,
        bool hasPlan = true,
        bool dispatcher = true,
        float length = 0f,
        float? car = null,
        bool tipCoupled = false) =>
        new(hasPlan, dispatcher, phase, pin, remToCleared, remaining, speed, length, car, tipCoupled);

    private static RouteExecDecision Run(IReadOnlyList<LocoCommand> cmds, ref RouteExecState s, in RouteExecInput input) =>
        RouteCommandExecutor.Tick(cmds, ref s, in input);

    [Theory]
    [InlineData(HtpSetDestAuditTests.Sl55SecondPickup)]
    [InlineData(HtpSetDestAuditTests.Sl55FirstPickup)]
    public void Smoke_16_2_ship2_b4l_go_stops_cleared_throws_pivot_outbound_then_drives_to_dest(string dest)
    {
        var snap = HtpFixtures.LoadGraph();
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55ViaSpur,
            dest,
            destYardId: "SW",
            yardFor: PathRouteConstraints.YardIdOf,
            mode: PathPlanMode.Yard);
        var cmds = RouteCommandParser.Generate(plan, snap.Edges, startReverse: true);
        var pin = cmds[2].TargetId;
        var s = RouteCommandExecutor.Begin(cmds);

        var leg1 = Run(cmds, ref s, In(pin: pin, remToCleared: 80f));
        Assert.Equal(RouteExecAction.Drive, leg1.Action);
        Assert.True(leg1.TravelReverse);
        Assert.Equal(pin, leg1.LegPinId);
        Assert.Equal(YardApproachKinematics.CruiseSpeedKmh, leg1.RequestKmh);

        var braking = Run(cmds, ref s, In(RouteClearancePhase.Cleared, pin, remToCleared: 0f, speed: 3f));
        Assert.Equal(RouteExecAction.Brake, braking.Action);

        var thrown = Run(cmds, ref s, In(RouteClearancePhase.Cleared, pin, remToCleared: 0f, speed: 0f));
        Assert.Equal(RouteExecAction.Throw, thrown.Action);
        Assert.Equal(pin, thrown.TargetId);
        var tracks = plan.TrackIds.ToList();
        var stemIndex = tracks.LastIndexOf(cmds[0].TargetId);
        Assert.Contains(
            snap.Edges,
            e => e.JunctionId == pin
                && e.RequiredBranch == thrown.Branch
                && tracks.IndexOf(e.ToTrackId) > stemIndex);

        RouteCommandExecutor.ReportThrow(ref s, ok: true);
        var leg2 = Run(cmds, ref s, In(RouteClearancePhase.Cleared, pin, remaining: 200f));
        Assert.Equal(RouteExecAction.Drive, leg2.Action);
        Assert.False(leg2.TravelReverse);
        Assert.Null(leg2.LegPinId);
        Assert.Equal(dest, leg2.TargetId);

        var arriving = Run(cmds, ref s, In(remaining: RouteCommandExecutor.DestEndPadMeters, speed: 5f));
        Assert.Equal(RouteExecAction.Brake, arriving.Action);
        Assert.Equal(RouteExecAction.Done, Run(cmds, ref s, In(remaining: 10f, speed: 0f)).Action);
    }

    [Theory]
    [InlineData(HtpSetDestAuditTests.Sl55SecondPickup, "1002848")]
    [InlineData(HtpSetDestAuditTests.Sl55FirstPickup, "1002868")]
    public void Smoke_16_2_ship2_live_graph_throw_carries_the_outbound_branch(string dest, string junction)
    {
        var snap = HtpFixtures.LoadGraph();
        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            HtpSetDestAuditTests.Sl55ViaSpur,
            dest,
            destYardId: "SW",
            yardFor: PathRouteConstraints.YardIdOf,
            mode: PathPlanMode.Yard);

        var cmds = RouteCommandParser.Generate(plan, snap.Edges, startReverse: true);

        Assert.Equal(junction, cmds[2].TargetId);
        Assert.True(cmds[2].RequiredBranch >= 0, RouteCommandTelemetry.FormatQueue(cmds));
    }

    [Fact]
    public void Smoke_16_2_ship2_at_switch_creeps_and_never_throws_before_cleared()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);

        var d = Run(Sawtooth, ref s, In(RouteClearancePhase.AtSwitch, remToCleared: 1f, speed: 0f));

        Assert.Equal(RouteExecAction.Drive, d.Action);
        Assert.Equal(YardApproachKinematics.TouchdownSpeedKmh, d.RequestKmh);
        Assert.Equal(0, s.Index);
    }

    [Fact]
    public void Smoke_16_2_ship2_cleared_but_rolling_brakes_before_the_throw()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);

        var d = Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared, speed: 2f));

        Assert.Equal(RouteExecAction.Brake, d.Action);
        Assert.Equal(RouteCommandExecutor.ReasonStopping, d.Reason);
        Assert.False(s.AwaitingThrow);
    }

    [Fact]
    public void Smoke_16_2_ship2_leftover_pin_cleared_does_not_trip_this_legs_stop()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);

        var d = Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared, pin: "J-old"));

        Assert.Equal(RouteExecAction.Drive, d.Action);
        Assert.Equal(YardApproachKinematics.ApproachSpeedKmh, d.RequestKmh);
        Assert.Equal(RouteCommandExecutor.ReasonWaitPin, d.Reason);
        Assert.Equal(Pin, d.LegPinId);
        Assert.Equal(0, s.Index);
    }

    [Fact]
    public void Smoke_16_2_ship2_failed_throw_holds_the_train()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);
        Assert.Equal(RouteExecAction.Throw, Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared)).Action);

        RouteCommandExecutor.ReportThrow(ref s, ok: false);

        var d = Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared));
        Assert.Equal(RouteExecAction.Hold, d.Action);
        Assert.Equal(RouteCommandExecutor.ReasonThrowFailed, d.Reason);
    }

    [Fact]
    public void Smoke_16_2_ship2_throw_is_issued_once_until_reported()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);
        Assert.Equal(RouteExecAction.Throw, Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared)).Action);

        var again = Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared));

        Assert.Equal(RouteExecAction.Brake, again.Action);
        Assert.Equal(RouteCommandExecutor.ReasonAwaitThrow, again.Reason);
    }

    [Fact]
    public void Smoke_16_2_ship2_no_dispatcher_holds_at_the_throw()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);

        var d = Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared, dispatcher: false));

        Assert.Equal(RouteExecAction.Hold, d.Action);
        Assert.Equal(RouteCommandExecutor.ReasonNeedDispatcher, d.Reason);
    }

    [Fact]
    public void Smoke_16_2_ship2_stale_plan_holds()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);

        var d = Run(Sawtooth, ref s, In(hasPlan: false));

        Assert.Equal(RouteExecAction.Hold, d.Action);
        Assert.Equal(RouteCommandExecutor.ReasonPlanStale, d.Reason);
    }

    [Fact]
    public void Smoke_16_2_ship2_pivot_track_without_junction_runs_full_align()
    {
        var cmds = new[]
        {
            new LocoCommand(LocoCommandAction.Drive, "Stem", travelReverse: false),
            new LocoCommand(LocoCommandAction.Stop),
            new LocoCommand(LocoCommandAction.ThrowSwitch, "Stem"),
            new LocoCommand(LocoCommandAction.ChangeDirection, travelReverse: true),
            new LocoCommand(LocoCommandAction.Drive, "Dest", travelReverse: true),
        };
        var s = RouteCommandExecutor.Begin(cmds);

        var d = Run(cmds, ref s, In(RouteClearancePhase.Cleared, pin: "J-any"));

        Assert.Equal(RouteExecAction.Align, d.Action);
        Assert.Equal("Stem", d.TargetId);
    }

    [Fact]
    public void Smoke_16_2_ship2_junction_with_unknown_branch_falls_back_to_align()
    {
        var cmds = Sawtooth.ToArray();
        cmds[2] = new LocoCommand(LocoCommandAction.ThrowSwitch, Pin, targetIsJunction: true);
        var s = RouteCommandExecutor.Begin(cmds);

        Assert.Equal(RouteExecAction.Align, Run(cmds, ref s, In(RouteClearancePhase.Cleared)).Action);
    }

    [Fact]
    public void Smoke_16_2_ship2_final_leg_without_corridor_rem_fails_closed()
    {
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "Dest") };
        var s = RouteCommandExecutor.Begin(cmds);

        var d = Run(cmds, ref s, In(remaining: null));

        Assert.Equal(RouteExecAction.Brake, d.Action);
        Assert.Equal(RouteCommandExecutor.ReasonWaitRemaining, d.Reason);
    }

    [Fact]
    public void Smoke_16_2_ship2_final_leg_stops_a_consist_length_short_of_the_end_pad()
    {
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "Dest") };
        var s = RouteCommandExecutor.Begin(cmds);

        var stillDriving = Run(cmds, ref s, In(remaining: 60f, length: 40f, speed: 10f));
        var stop = Run(cmds, ref s, In(remaining: 55f, length: 40f, speed: 10f));

        Assert.Equal(RouteExecAction.Drive, stillDriving.Action);
        Assert.Equal(RouteExecAction.Brake, stop.Action);
        Assert.Equal(RouteCommandExecutor.ReasonArriving, stop.Reason);
    }

    [Fact]
    public void Smoke_16_2_ship2_cursor_survives_align_refreezing_the_plan_queue()
    {
        RoutePlanSession.Clear();
        RouteExecSession.Stop();
        try
        {
            var edges = HtpSawtoothTddStepsTests.MockShortTailYardEdges();
            var plan = PathPlan.Find(
                edges,
                new Dictionary<string, int>(),
                HtpSawtoothTddStepsTests.ShortA,
                HtpSawtoothTddStepsTests.ShortC,
                mode: PathPlanMode.Yard,
                consistLengthMeters: 20f);
            RoutePlanSession.SetPlan(plan, HtpSawtoothTddStepsTests.ShortA);
            RoutePlanSession.SetCommands(RouteCommandParser.Generate(plan, edges));
            RouteExecSession.Start(RoutePlanSession.Commands);
            var captured = RouteExecSession.Commands;

            RoutePlanSession.SetPlan(plan, HtpSawtoothTddStepsTests.ShortA);

            Assert.Empty(RoutePlanSession.Commands);
            Assert.Same(captured, RouteExecSession.Commands);
            Assert.True(RouteExecSession.Active);
        }
        finally
        {
            RouteExecSession.Stop();
            RoutePlanSession.Clear();
        }
    }

    [Fact]
    public void Smoke_16_2_ship2_session_brakes_until_the_first_drive_decision()
    {
        RouteExecSession.Stop();
        try
        {
            RouteExecSession.Start(Sawtooth);
            Assert.True(RouteExecSession.WantsBrake);
            Assert.True(RouteExecSession.TravelReverse);

            RouteExecSession.Tick(In());

            Assert.False(RouteExecSession.WantsBrake);
            Assert.Equal(YardApproachKinematics.IntermediateSpeedKmh, RouteExecSession.RequestKmh);
        }
        finally
        {
            RouteExecSession.Stop();
        }
    }

    [Theory]
    [InlineData(true, false, true, RouteExecStartResult.JobListActive)]
    [InlineData(false, true, true, RouteExecStartResult.SwitchListGoActive)]
    [InlineData(false, false, false, RouteExecStartResult.NeedDispatcher)]
    [InlineData(false, false, true, RouteExecStartResult.Ok)]
    public void Smoke_16_2_ship2_route_go_start_gates(
        bool jobList,
        bool listGo,
        bool dispatcher,
        RouteExecStartResult expected)
    {
        Assert.Equal(
            expected,
            RouteExecSession.CanStart(Sawtooth, dispatcher, listGo, jobList));
        Assert.Equal(
            RouteExecStartResult.NoQueue,
            RouteExecSession.CanStart(new LocoCommand[0], hasDispatcher: true, switchListGoActive: false, jobListActive: false));
    }

    [Fact]
    public void Smoke_16_2_34_4_b1s_final_leg_creeps_to_the_car_instead_of_braking_at_45m()
    {
        // Cab 2.16.34.4: rear gap 61 → 7 m at 24 → 22 km/h, catch-down only at tgt=3,
        // then a hard couple. From 45 m the request is 3 so catch-down arms. Full
        // brake stays at the couple scan. An empty corridor does not get that request.
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-B1S", travelReverse: true) };
        var far = RouteCommandExecutor.Begin(cmds);
        var mid = RouteCommandExecutor.Begin(cmds);
        var near = RouteCommandExecutor.Begin(cmds);
        var pad = RouteCommandExecutor.Begin(cmds);

        var at70 = Run(cmds, ref far, In(remaining: 400f, speed: 24f, car: 70f));
        var at45 = Run(cmds, ref mid, In(remaining: 400f, speed: 24f, car: 45f));
        var at3 = Run(cmds, ref near, In(remaining: 400f, speed: 8f, car: 3f));
        var empty = Run(cmds, ref pad, In(remaining: 40f, speed: 24f));
        Assert.Equal(RouteExecAction.Drive, at70.Action);
        Assert.Equal(YardApproachKinematics.CruiseSpeedKmh, at70.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, at45.Action);
        Assert.Equal(YardApproachKinematics.TouchdownSpeedKmh, at45.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, at3.Action);
        Assert.Equal(YardApproachKinematics.TouchdownSpeedKmh, at3.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, empty.Action);
        Assert.Equal(YardApproachKinematics.ApproachSpeedKmh, empty.RequestKmh);

        var s = RouteCommandExecutor.Begin(cmds);
        var kiss = Run(cmds, ref s, In(remaining: 400f, speed: 3f, car: 1f));
        var done = Run(cmds, ref s, In(remaining: 400f, speed: 0f, car: 1f));
        Assert.Equal(RouteExecAction.Drive, kiss.Action);
        Assert.Equal(RouteCommandExecutor.KissCoastKmh, kiss.RequestKmh);
        Assert.Equal(RouteExecAction.Done, done.Action);
    }

    [Fact]
    public void Smoke_16_2_34_6_b1s_holds_drive_at_13_until_the_train_has_crept()
    {
        // Cab 2.16.34.5: full brake at ~2.7 m while still at 13 km/h closed the
        // knuckle. Drive stays until speed is a creep, or the gap is an impact.
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-B1S", travelReverse: true) };
        var fast = RouteCommandExecutor.Begin(cmds);
        var emergency = RouteCommandExecutor.Begin(cmds);

        var at13 = Run(cmds, ref fast, In(remaining: 400f, speed: 13f, car: 1f));
        var impact = Run(cmds, ref emergency, In(remaining: 400f, speed: 13f, car: 0.2f));

        Assert.Equal(RouteExecAction.Drive, at13.Action);
        Assert.Equal(YardApproachKinematics.TouchdownSpeedKmh, at13.RequestKmh);
        Assert.Equal(RouteExecAction.Brake, impact.Action);
        Assert.Equal(RouteCommandExecutor.ReasonArriving, impact.Reason);
        Assert.Equal(0.85f, PrepCreepPolicy.CatchDownTrain);
    }

    [Fact]
    public void Smoke_16_2_34_7_b1s_does_not_dump_the_independent_at_a_3kmh_kiss()
    {
        // Cab 2.16.34.6: catch-down reached 3 km/h, then Brake arriving snapped
        // the independent to 100 at the knuckle. The couple made and the gap
        // sprang open. Stay on Drive through a 3 km/h kiss. Go-stop waits
        // until the train is already stopped.
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-B1S", travelReverse: true) };
        var moving = RouteCommandExecutor.Begin(cmds);
        var contact = RouteCommandExecutor.Begin(cmds);

        var kiss = Run(cmds, ref moving, In(remaining: 400f, speed: 3f, car: 1f));
        var atContact = Run(cmds, ref contact, In(remaining: 400f, speed: 3f, car: 0.2f));

        Assert.Equal(RouteExecAction.Drive, kiss.Action);
        Assert.Equal(RouteCommandExecutor.KissCoastKmh, kiss.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, atContact.Action);
        Assert.Equal(RouteCommandExecutor.KissCoastKmh, atContact.RequestKmh);
    }

    [Fact]
    public void Smoke_16_2_34_8_b1s_throttle_off_once_the_knuckle_is_closed()
    {
        // Cab 2.16.34.7: the 1 km/h coast kept Drive, then the knuckle shut and
        // clearance dropped. The governor first-notched 9% at 0–1 km/h and
        // shoved the couple open. Throttle off (req 0) while that knuckle is
        // closed. The open coast stays 1 km/h — it is not a finished kiss.
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-B1S", travelReverse: true) };
        var open = RouteCommandExecutor.Begin(cmds);
        var shut = RouteCommandExecutor.Begin(cmds);
        var rest = RouteCommandExecutor.Begin(cmds);

        var coast = Run(cmds, ref open, In(remaining: 400f, speed: 0.4f, car: 1f));
        var shove = Run(cmds, ref shut, In(remaining: 400f, speed: 0.4f, tipCoupled: true));
        var done = Run(cmds, ref rest, In(remaining: 400f, speed: 0f, tipCoupled: true));

        Assert.Equal(RouteExecAction.Drive, coast.Action);
        Assert.Equal(RouteCommandExecutor.KissCoastKmh, coast.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, shove.Action);
        Assert.Equal(0f, shove.RequestKmh);
        Assert.Equal(RouteCommandExecutor.ReasonKnuckle, shove.Reason);
        Assert.NotEqual(RouteExecAction.Brake, shove.Action);
        Assert.Equal(RouteExecAction.Done, done.Action);
    }

    [Fact]
    public void Smoke_16_2_34_9_b1s_does_not_resume_the_rail_end_after_the_kiss()
    {
        // Cab 2.16.34.8: B1S kissed, then the gap vanished because the car
        // joined the consist. The final leg aimed at the far end of the rail
        // and drove on. A lost kiss gap stays throttle-off. A far gap that
        // drops out still uses the rail. An empty track still uses the pad.
        var cmds = new[] { new LocoCommand(LocoCommandAction.Drive, "SW-B1S", travelReverse: true) };
        var kissed = RouteCommandExecutor.Begin(cmds);
        var coast = Run(cmds, ref kissed, In(remaining: 400f, speed: 0.4f, car: 1f));
        var lost = Run(cmds, ref kissed, In(remaining: 400f, speed: 0.4f));
        var rest = Run(cmds, ref kissed, In(remaining: 400f, speed: 0f));

        Assert.Equal(RouteExecAction.Drive, coast.Action);
        Assert.Equal(RouteCommandExecutor.KissCoastKmh, coast.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, lost.Action);
        Assert.Equal(0f, lost.RequestKmh);
        Assert.Equal(RouteCommandExecutor.ReasonKnuckle, lost.Reason);
        Assert.NotEqual(RouteExecAction.Brake, lost.Action);
        Assert.Equal(RouteExecAction.Done, rest.Action);

        var far = RouteCommandExecutor.Begin(cmds);
        var approach = Run(cmds, ref far, In(remaining: 400f, speed: 24f, car: 70f));
        var dropout = Run(cmds, ref far, In(remaining: 400f, speed: 24f));
        Assert.Equal(YardApproachKinematics.CruiseSpeedKmh, approach.RequestKmh);
        Assert.Equal(RouteExecAction.Drive, dropout.Action);
        Assert.Null(dropout.Reason);
        Assert.Equal(YardApproachKinematics.CruiseSpeedKmh, dropout.RequestKmh);
    }

    [Fact]
    public void Smoke_16_2_34_10_b1s_knuckle_zero_does_not_become_cruise()
    {
        // Cab 2.16.34.9: route logged req=0 knuckle, then the PID notched
        // 9% through 63% at 0–4 km/h. Resolve treats 0 as missing and
        // substitutes 25. A route zero stays 0. A plain zero still cruises.
        Assert.Equal(PidSpeedTarget.DefaultRequestKmh, PidSpeedTarget.Resolve(0f, null));

        var shoved = default(PidSpeedState);
        var cruise = PidSpeedHold.Tick(
            KnuckleInput(speed: 0.5f, honorZero: false),
            ref shoved);
        Assert.Equal(PidSpeedTarget.DefaultRequestKmh, cruise.TargetKmh);
        Assert.True(cruise.DesiredThrottle > 0f);

        var held = default(PidSpeedState);
        var crawl = PidSpeedHold.Tick(
            KnuckleInput(speed: 0.5f, honorZero: true),
            ref held);
        var fast = PidSpeedHold.Tick(
            KnuckleInput(speed: 4f, honorZero: true, throttle: 0.09f),
            ref held);
        Assert.Equal(0f, crawl.TargetKmh);
        Assert.Equal(0f, crawl.DesiredThrottle);
        Assert.Equal(0f, fast.TargetKmh);
        Assert.Equal(0f, fast.DesiredThrottle);
    }

    private static PidSpeedInput KnuckleInput(float speed, bool honorZero, float throttle = 0f) =>
        new(
            0.02f,
            speed,
            requestKmh: 0f,
            postedKmh: null,
            throttle,
            independent: 0f,
            armed: true,
            derailIntervening: false,
            thermalCeiling: 1f,
            reverser: PidSpeedGear.ReverseValue,
            legNeedsReverse: true,
            honorZero: honorZero);

    [Fact]
    public void Smoke_16_2_34_8_b1s_reverse_keeps_the_rear_gap_when_the_other_end_is_coupled()
    {
        // Cab B1S reverse: end=Rear tenths=-1. The free coupler on the
        // already-coupled end faces away, and the link to our own car faces
        // the travel intent. Neither is the standing car. The pushed cut's
        // free knuckle is the rear gap.
        var otherEnd = ConsistTravelLead.ClassifyApproachCoupler(
            alignment: -0.9f,
            coupled: false,
            coupledToConsistMate: false,
            oppositeCoupled: true,
            alone: false);
        var ownCar = ConsistTravelLead.ClassifyApproachCoupler(
            alignment: 0.95f,
            coupled: true,
            coupledToConsistMate: true,
            oppositeCoupled: true,
            alone: false);
        var rearTip = ConsistTravelLead.ClassifyApproachCoupler(
            alignment: 0.92f,
            coupled: false,
            coupledToConsistMate: false,
            oppositeCoupled: true,
            alone: false);
        Assert.Equal(ApproachCouplerRole.Ignore, otherEnd);
        Assert.Equal(ApproachCouplerRole.Ignore, ownCar);
        Assert.Equal(ApproachCouplerRole.FreeTip, rearTip);

        var score = default(ApproachTipScore);
        Assert.False(score.Consider(-0.9f, false, false, true, false, out _));
        Assert.False(score.Consider(0.95f, true, true, true, false, out _));
        Assert.True(score.Consider(0.92f, false, false, true, false, out var free));
        Assert.True(free);
        Assert.False(score.KnuckleClosed);

        score = default;
        Assert.True(score.Consider(0.9f, true, false, true, false, out var shut));
        Assert.False(shut);
        Assert.True(score.KnuckleClosed);
    }

    [Fact]
    public void Smoke_16_2_34_1_route_go_keeps_clearance_for_its_own_frog()
    {
        // Cab 2.16.34.1 C4S: the route list board pin (1576584) discarded samples
        // for the queue's pivot (1589214), so step 0 braked on "wait pin" forever.
        SwitchListSession.Clear();
        RoutePinBoardSession.Clear();
        RouteExecSession.Stop();
        try
        {
            var snap = HtpFixtures.LoadGraph();
            var plan = PathPlan.Find(
                snap.Edges,
                snap.Selected,
                HtpSetDestAuditTests.Sl55ViaSpur,
                HtpSetDestAuditTests.Sl55SecondPickup,
                destYardId: "SW",
                yardFor: PathRouteConstraints.YardIdOf,
                mode: PathPlanMode.Yard);
            var steps = SwitchListPlanner.BuildFromRoute("SW", HtpSetDestAuditTests.Sl55SecondPickup, plan, true, true);
            Assert.NotNull(steps);
            SwitchListSession.Bind("route:SW", steps!);
            Assert.True(RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW", HtpSetDestAuditTests.Sl55ViaSpur) > 0);
            var board = RoutePinBoardSession.PinIdForStep(SwitchListSession.CurrentStep!.Index);
            Assert.False(string.IsNullOrEmpty(board));

            Assert.False(RouteClearanceSession.ShouldAcceptPin(board + "-other"));
            RouteExecSession.Start(Sawtooth);
            Assert.True(RouteClearanceSession.ShouldAcceptPin(board + "-other"));
        }
        finally
        {
            RouteExecSession.Stop();
            SwitchListSession.Clear();
            RoutePinBoardSession.Clear();
        }
    }

    [Fact]
    public void Smoke_16_2_34_4_c4s_board_pin_is_the_queue_throw_not_the_walk()
    {
        // Cab 2.16.34.3: pin-board step 1 was 1576584 while the queue threw 1589214.
        SwitchListSession.Clear();
        RoutePinBoardSession.Clear();
        RoutePlanSession.Clear();
        try
        {
            var snap = HtpFixtures.LoadGraph();
            var plan = PathPlan.Find(
                snap.Edges,
                snap.Selected,
                HtpSetDestAuditTests.Sl55ViaSpur,
                HtpSetDestAuditTests.Sl55SecondPickup,
                destYardId: "SW",
                yardFor: PathRouteConstraints.YardIdOf,
                mode: PathPlanMode.Yard);
            var cmds = RouteCommandParser.Generate(plan, snap.Edges, startReverse: true);
            var queuePin = cmds[2].TargetId;
            Assert.Equal(LocoCommandAction.ThrowSwitch, cmds[2].Action);

            var steps = SwitchListPlanner.BuildFromRoute(
                "SW",
                HtpSetDestAuditTests.Sl55SecondPickup,
                plan,
                pinNeedsReverse: true,
                destNeedsReverse: true);
            Assert.NotNull(steps);
            var raw = new RoutePinBoardEntry[RoutePinBoard.Capacity];
            var rawCount = RoutePinBoard.Collect(
                steps,
                snap.Edges,
                snap.Selected,
                "SW",
                raw,
                raw.Length,
                HtpSetDestAuditTests.Sl55ViaSpur);
            Assert.True(rawCount > 0);
            Assert.NotEqual(queuePin, raw[0].PinId);

            RoutePlanSession.SetPlan(plan, HtpSetDestAuditTests.Sl55ViaSpur);
            RoutePlanSession.SetCommands(cmds);
            SwitchListSession.Bind("route:SW", steps!);
            Assert.True(RoutePinBoardSession.Rebuild(
                snap.Edges,
                snap.Selected,
                "SW",
                HtpSetDestAuditTests.Sl55ViaSpur) > 0);
            Assert.Equal(queuePin, RoutePinBoardSession.PinIdForStep(SwitchListSession.CurrentStep!.Index));
        }
        finally
        {
            RoutePlanSession.Clear();
            SwitchListSession.Clear();
            RoutePinBoardSession.Clear();
        }
    }

    [Fact]
    public void Smoke_16_2_34_b1s_start_direction_follows_origin_exit_end_not_a_stale_pin()
    {
        // Cab 2.16.34 FAIL: B1S Set dest dot-tested toward the still-latched C4S pin
        // (behind) → Drive< → rolled out the #S959 end onto the C4S corridor.
        // Origin B4L runs A(0,0)–B(100,0); B1S corridor's next hop (#S767) touches B.
        const float locoX = 50f;
        var staleC4sPinBehind = DriveSetFacing.IsTargetBehind(1f, 0f, -250f - locoX, 0f);

        var reverse = RouteStartFacing.NeedsReverse(
            1f, 0f, locoX, 0f,
            0f, 0f, 100f, 0f,
            100f, 0f, 160f, 5f);

        Assert.True(staleC4sPinBehind);
        Assert.False(reverse);
    }

    [Fact]
    public void Smoke_16_2_34_c4s_start_direction_is_reverse_when_exit_end_is_behind()
    {
        // C4S corridor leaves B4L through the other end (#S959) — behind the hood.
        var reverse = RouteStartFacing.NeedsReverse(
            1f, 0f, 50f, 0f,
            0f, 0f, 100f, 0f,
            -60f, 3f, 0f, 0f);

        Assert.True(reverse);
    }

    [Fact]
    public void Smoke_16_2_34_start_direction_ignores_origin_track_point_order()
    {
        var ab = RouteStartFacing.NeedsReverse(-1f, 0f, 50f, 0f, 0f, 0f, 100f, 0f, 100f, 0f, 160f, 0f);
        var ba = RouteStartFacing.NeedsReverse(-1f, 0f, 50f, 0f, 100f, 0f, 0f, 0f, 160f, 0f, 100f, 0f);

        Assert.True(ab);
        Assert.Equal(ab, ba);
    }

    [Fact]
    public void Smoke_16_2_ship2_exec_log_names_the_throw_and_branch()
    {
        var s = RouteCommandExecutor.Begin(Sawtooth);
        var d = Run(Sawtooth, ref s, In(RouteClearancePhase.Cleared));

        Assert.Equal(
            "T2 route-exec: step 2/5 Throw R J-pivot branch=1",
            RouteExecTelemetry.Format(in s, Sawtooth.Length, in d));
    }
}
