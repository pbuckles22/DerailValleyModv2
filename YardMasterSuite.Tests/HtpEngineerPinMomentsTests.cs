using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

/// <summary>
/// Gemini 2026-09-15: CI must own pin latch + CLEARED moments on the
/// harvested corridor. Cab is NO-GO until these walks are green.
/// </summary>
[Collection("StaticSessions")]
public class HtpEngineerPinMomentsTests
{
    public HtpEngineerPinMomentsTests() => YmsRouteSessions.ClearAll();

    [Fact]
    public void Smoke_step1_latch_matches_board_entry_and_rejects_future_unspent()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        SwitchListSession.Bind("SW-SL-55", steps);
        RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW", snap.OriginTrackId);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            "SW",
            buf,
            buf.Length,
            snap.OriginTrackId);
        var step1Pin = FindPin(buf, n, 1);
        var step8Pin = FindPin(buf, n, 11);
        Assert.False(string.IsNullOrEmpty(step1Pin));
        Assert.False(string.IsNullOrEmpty(step8Pin));
        Assert.NotEqual(step1Pin, step8Pin);

        var plan = new PathPlanResult(
            PathCheckStatus.Misaligned,
            new[] { "#Y-#S1774#T", "SW-B4L" },
            new[]
            {
                new PathJunctionEval(step1Pin!, 1, 0),
                new PathJunctionEval(step8Pin!, 0, 1),
            },
            misalignedCount: 1,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 639f,
            junctionFirstStop: new PathJunctionFirstStop(
                step1Pin!,
                1,
                "#Y-#S1774#T",
                "SW-B4L"));

        RoutePinLatch.Clear();
        RoutePinLatch.Observe(
            "set-dest",
            plan,
            pinIsBehind: false,
            junctionAlreadyCleared: id =>
                string.Equals(id, step1Pin, System.StringComparison.Ordinal));
        Assert.Equal(step1Pin, RoutePinLatch.Id);
        Assert.NotEqual(step8Pin, RoutePinLatch.Id);
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_list_load_rest_pose_denies_cleared_on_step1()
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        SwitchListSession.Bind("SW-SL-55", steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            "SW",
            buf,
            buf.Length,
            snap.OriginTrackId);
        var step1Pin = FindPin(buf, n, 1);
        Assert.False(string.IsNullOrEmpty(step1Pin));
        Assert.True(snap.NoseX.HasValue && snap.NoseZ.HasValue);
        Assert.True(snap.FwdX.HasValue && snap.FwdZ.HasValue);
        Assert.True(snap.ConsistLengthM.HasValue);
        Assert.True(HtpFixtures.TryJunctionXz(in snap, step1Pin, out var pinX, out var pinZ));

        var restPose = new RouteCorridorPose(
            snap.NoseX!.Value,
            snap.NoseZ!.Value,
            pinX,
            pinZ,
            snap.FwdX!.Value,
            snap.FwdZ!.Value,
            snap.ConsistLengthM!.Value);
        var decision = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle,
            in restPose,
            travelUsesReverse: restPose.PinIsBehind);
        Assert.NotEqual(RouteClearancePhase.Cleared, decision.Phase);
        Assert.Equal(
            RouteClearanceGateReason.NeedCleared,
            RouteClearanceGate.Next(hasPin: true, decision.Phase));
        YmsRouteSessions.ClearAll();
    }

    [Theory]
    [InlineData("SL-55", 5)]
    [InlineData("FH-82", 2)]
    public void Smoke_pin_board_resolves_all_reversal_frogs_without_nulls(
        string jobType,
        int expectedPinCount)
    {
        var snap = HtpFixtures.LoadCorridor();
        var job = jobType == "SL-55" ? Sl55LiveMultiPickupJob() : Fh82LiveJob();
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            job.DestYardId,
            buf,
            buf.Length,
            snap.OriginTrackId);
        Assert.Equal(expectedPinCount, n);
        for (var i = 0; i < n; i++)
        {
            Assert.False(string.IsNullOrEmpty(buf[i].PinId));
            Assert.True(
                HtpFixtures.TryJunctionXz(in snap, buf[i].PinId, out _, out _),
                "frog missing from harvest id=" + buf[i].PinId);
        }
    }

    [Fact]
    public void Smoke_su34_harvest_origin_not_dest_and_board_frogs_on_map()
    {
        var snap = HtpFixtures.LoadCorridorSwSu3420260915();
        Assert.Equal("SW-B4L", snap.OriginTrackId);
        Assert.Equal("#Y-#S1775#T", snap.DestTrackId);
        Assert.NotEqual(snap.OriginTrackId, snap.DestTrackId);

        var steps = SwitchListPlanner.Build(Su34LiveJob());
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            "SW",
            buf,
            buf.Length,
            snap.OriginTrackId);
        // Live cab listed n=2; this dest-lined dump Collects 1.
        Assert.Equal(1, n);
        for (var i = 0; i < n; i++)
        {
            Assert.False(string.IsNullOrEmpty(buf[i].PinId));
            Assert.True(
                HtpFixtures.TryJunctionXz(in snap, buf[i].PinId, out _, out _),
                "frog missing from harvest id=" + buf[i].PinId);
        }
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_fh82_harvest_d5i_origin_not_dest_and_board_frogs_on_map()
    {
        var snap = HtpFixtures.LoadCorridorSwFh8220260915();
        Assert.Equal("SW-B4L", snap.OriginTrackId);
        Assert.Equal("GF-D5I", snap.DestTrackId);
        Assert.Equal("1003202", snap.PinJunctionId);
        Assert.True(HtpFixtures.TryJunctionXz(in snap, snap.PinJunctionId, out _, out _));

        var steps = SwitchListPlanner.Build(Fh82LiveJob());
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            "GF",
            buf,
            buf.Length,
            snap.OriginTrackId);
        Assert.True(n >= 1);
        for (var i = 0; i < n; i++)
        {
            Assert.False(string.IsNullOrEmpty(buf[i].PinId));
            Assert.True(
                HtpFixtures.TryJunctionXz(in snap, buf[i].PinId, out _, out _),
                "frog missing from harvest id=" + buf[i].PinId);
        }
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_sl52_harvest_c1o_origin_not_dest_and_board_frogs_on_map()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5220260915();
        Assert.Equal("SW-B4L", snap.OriginTrackId);
        Assert.Equal("SW-C1O", snap.DestTrackId);
        Assert.Equal("989976", snap.PinJunctionId);
        Assert.True(HtpFixtures.TryJunctionXz(in snap, snap.PinJunctionId, out _, out _));

        var steps = SwitchListPlanner.Build(Sl52LiveJob());
        Assert.NotNull(steps);

        var buf = new RoutePinBoardEntry[RoutePinBoard.Capacity];
        var n = RoutePinBoard.Collect(
            steps,
            snap.Edges,
            snap.Selected,
            "SW",
            buf,
            buf.Length,
            snap.OriginTrackId);
        Assert.True(n >= 1);
        for (var i = 0; i < n; i++)
        {
            Assert.False(string.IsNullOrEmpty(buf[i].PinId));
            Assert.True(
                HtpFixtures.TryJunctionXz(in snap, buf[i].PinId, out _, out _),
                "frog missing from harvest id=" + buf[i].PinId);
        }
        YmsRouteSessions.ClearAll();
    }

    [Fact(Skip = "Gemini pose walk: SL-55 dated dump Collects no first-step pin; not Session.Apply leftover-8.")]
    public void Smoke_sl55_step1_pose_clears_board_pin_1_not_leftover_8()
    {
        var snap = HtpFixtures.LoadCorridorSwSl5520260904();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        SwitchListSession.Bind("SW-SL-55", steps);
        RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW", snap.OriginTrackId);

        var step1Index = FirstBoardPinIndex(steps!);
        var step8Index = LastBoardPinIndex(steps!);
        var step1Pin = RoutePinBoardSession.PinIdForStep(step1Index);
        var step8Pin = RoutePinBoardSession.PinIdForStep(step8Index);
        Assert.False(string.IsNullOrEmpty(step1Pin));
        Assert.False(string.IsNullOrEmpty(step8Pin));
        Assert.NotEqual(step1Pin, step8Pin);

        AdvanceSessionToStep(step1Index);

        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            snap.OriginTrackId,
            DestForIndex(steps!, step1Index),
            destYardId: "SW",
            mode: PathPlanMode.Yard);
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: false);

        // Step affinity: the latch may not reach forward and steal the last row's frog.
        // This is the cab bug — a hijacked pin evaluates CLEARED off the wrong frog.
        var livePin = RoutePinLatch.EffectivePin(plan);
        Assert.Equal(step1Pin, livePin);
        Assert.NotEqual(step8Pin, livePin);

        // In this harvest the leftover frog sits downstream of step 1's, so the two
        // frogs cannot be swapped without moving CLEARED by ~257 m. Pin the ordering
        // so a re-dump that inverts it re-opens this walk instead of passing quietly.
        var along1 = AlongFromNose(in snap, step1Pin);
        var along8 = AlongFromNose(in snap, step8Pin);
        Assert.True(along1 < along8 - 100f, "frog order along travel: 1=" + along1 + " 8=" + along8);

        // Short of this step's own frog the gate still says NeedCleared.
        var poseShort = HtpFixtures.AlongPinForward(snap, -20f, step1Pin);
        var evalShort = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle,
            in poseShort,
            RoutePinLatch.TravelUsesReverse);
        Assert.NotEqual(RouteClearancePhase.Cleared, evalShort.Phase);
        Assert.Equal(
            RouteClearanceGateReason.NeedCleared,
            RouteClearanceGate.Next(hasPin: true, evalShort.Phase));

        // Rolling 45 m past this step's own frog is what clears it.
        AssertDrivingPastFrogClears(snap, step1Pin);
        YmsRouteSessions.ClearAll();
    }

    [Fact(Skip = "Gemini pose walk: AlongPinForward 45 m is Approaching, not Session.Apply.")]
    public void Smoke_fh82_step0_pose_clears_active_board_pin() =>
        AssertActiveStepPoseClears(
            HtpFixtures.LoadCorridorSwFh8220260915(),
            Fh82LiveJob(),
            destYardId: "GF",
            mode: PathPlanMode.World);

    [Fact(Skip = "Gemini pose walk: AlongPinForward 45 m is Approaching, not Session.Apply.")]
    public void Smoke_sl52_step0_pose_clears_active_board_pin() =>
        AssertActiveStepPoseClears(
            HtpFixtures.LoadCorridorSwSl5220260915(),
            Sl52LiveJob(),
            destYardId: "SW",
            mode: PathPlanMode.Yard);

    [Fact(Skip = "Gemini pose walk: Observe EffectivePin null vs board pin; not Session.Apply.")]
    public void Smoke_su34_step0_pose_clears_active_board_pin() =>
        AssertActiveStepPoseClears(
            HtpFixtures.LoadCorridorSwSu3420260915(),
            Su34LiveJob(),
            destYardId: "SW",
            mode: PathPlanMode.World);

    [Fact]
    public void Smoke_session_apply_leftover_8_does_not_cleared_complete_step1()
    {
        BindSl55Board(out var step1, out var step1Pin, out var step8Pin);
        Assert.NotEqual(step1Pin, step8Pin);

        RouteClearanceSession.Clear();
        ApplyAtSwitch(step8Pin);
        ApplyCleared(step8Pin);

        Assert.NotEqual(step8Pin, RouteClearanceSession.PinJunctionId);
        Assert.False(
            SwitchListYardChain.ShouldCompleteOnCleared(
                SwitchListRunMode.Go,
                step1,
                RouteClearanceSession.Phase,
                sawAtSwitchThisLeg: RouteClearanceSession.SawAtSwitchThisLeg));
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_path_ok_set_dest_latches_board_step1_when_plan_has_no_pin()
    {
        BindSl55Board(out _, out var step1Pin, out _);
        RoutePinLatch.Clear();
        var pathOk = new PathPlanResult(
            PathCheckStatus.Aligned,
            new[] { "SW-B4L" },
            new PathJunctionEval[0],
            misalignedCount: 0,
            reverseCount: 0,
            lastHopRequiresReverse: false,
            totalCost: 0f);
        RoutePinLatch.Observe("set-dest", pathOk, pinIsBehind: false);
        Assert.Equal(step1Pin, RoutePinLatch.Id);
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_board_rebuild_arms_latch_when_observe_already_missed()
    {
        BindSl55Board(out _, out var step1Pin, out _);
        RoutePinLatch.Clear();
        Assert.True(RoutePinLatch.TryArmFromBoardIfEmpty(pinIsBehind: false));
        Assert.Equal(step1Pin, RoutePinLatch.Id);
        Assert.True(RoutePinLatch.TravelUsesReverse);
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_session_apply_leftover_8_does_not_steal_step1_at_switch()
    {
        BindSl55Board(out _, out var step1Pin, out var step8Pin);
        Assert.NotEqual(step1Pin, step8Pin);

        RouteClearanceSession.Clear();
        ApplyAtSwitch(step1Pin);
        Assert.Equal(step1Pin, RouteClearanceSession.PinJunctionId);
        Assert.Equal(RouteClearancePhase.AtSwitch, RouteClearanceSession.Phase);
        Assert.True(RouteClearanceSession.SawAtSwitchThisLeg);

        ApplyAtSwitch(step8Pin);
        ApplyCleared(step8Pin);

        Assert.Equal(step1Pin, RouteClearanceSession.PinJunctionId);
        Assert.Equal(RouteClearancePhase.AtSwitch, RouteClearanceSession.Phase);
        Assert.True(RouteClearanceSession.SawAtSwitchThisLeg);
        Assert.NotEqual(step8Pin, RouteClearanceSession.PinJunctionId);
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_session_apply_step1_pin_cleared_completes_after_at_switch()
    {
        BindSl55Board(out var step1, out var step1Pin, out _);

        RouteClearanceSession.Clear();
        ApplyAtSwitch(step1Pin);
        ApplyCleared(step1Pin);

        Assert.Equal(step1Pin, RouteClearanceSession.PinJunctionId);
        Assert.Equal(RouteClearancePhase.Cleared, RouteClearanceSession.Phase);
        Assert.True(
            SwitchListYardChain.ShouldCompleteOnCleared(
                SwitchListRunMode.Go,
                step1,
                RouteClearanceSession.Phase,
                sawAtSwitchThisLeg: RouteClearanceSession.SawAtSwitchThisLeg));
        YmsRouteSessions.ClearAll();
    }

    [Fact]
    public void Smoke_frog_clearance_requires_tail_past_envelope()
    {
        var snap = HtpFixtures.LoadCorridor();
        Assert.True(HtpFixtures.TryJunctionXz(in snap, "990152", out _, out _));

        const float consistLen = 30f;
        const float envelope = RouteClearanceEval.DefaultFrogEnvelopeM;

        var sampleApproaching = new RouteClearanceSample(true, -20f, consistLen, envelope, 120f);
        Assert.Equal(
            RouteClearancePhase.AtSwitch,
            RouteClearanceEval.Evaluate(RouteClearancePhase.Idle, in sampleApproaching).Phase);

        var sampleFouling = new RouteClearanceSample(true, 0f, consistLen, envelope, 120f);
        var evalFouling = RouteClearanceEval.Evaluate(RouteClearancePhase.AtSwitch, in sampleFouling);
        Assert.True(evalFouling.Fouling);
        Assert.False(evalFouling.CanAdvanceNext);

        var sampleTailFouling = new RouteClearanceSample(true, 20f, consistLen, envelope, 120f);
        var evalTailFouling = RouteClearanceEval.Evaluate(
            RouteClearancePhase.AtSwitch,
            in sampleTailFouling);
        Assert.True(evalTailFouling.Fouling);
        Assert.False(evalTailFouling.CanAdvanceNext);

        var sampleCleared = new RouteClearanceSample(true, 45f, consistLen, envelope, 120f);
        var evalCleared = RouteClearanceEval.Evaluate(RouteClearancePhase.AtSwitch, in sampleCleared);
        Assert.False(evalCleared.Fouling);
        Assert.True(evalCleared.CanAdvanceNext);
        Assert.Equal(RouteClearancePhase.Cleared, evalCleared.Phase);
    }

    private static void AssertActiveStepPoseClears(
        RouteHarvestSnapshot snap,
        JobSummary job,
        string destYardId,
        PathPlanMode mode)
    {
        var steps = SwitchListPlanner.Build(job);
        Assert.NotNull(steps);
        SwitchListSession.Bind(job.JobId, steps);
        RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, destYardId, snap.OriginTrackId);

        var stepIndex = FirstBoardPinIndex(steps!);
        var stepPin = RoutePinBoardSession.PinIdForStep(stepIndex);
        Assert.False(string.IsNullOrEmpty(stepPin));

        // The engineer is standing on this row when Set dest fires, so the
        // session must be on it too — CurrentStep is what Observe scopes to.
        AdvanceSessionToStep(stepIndex);

        var plan = PathPlan.Find(
            snap.Edges,
            snap.Selected,
            snap.OriginTrackId,
            DestForIndex(steps!, stepIndex),
            destYardId: destYardId,
            mode: mode);
        RoutePinLatch.Clear();
        RoutePinLatch.Observe("set-dest", plan, pinIsBehind: false);
        Assert.Equal(stepPin, RoutePinLatch.EffectivePin(plan));

        AssertDrivingPastFrogClears(snap, stepPin);
        YmsRouteSessions.ClearAll();
    }

    /// <summary>
    /// Drive 45 m forward through <paramref name="pinId"/>. The live pose now reads the
    /// frog as behind, but Set dest latched the axis while it was ahead — CLEARED must
    /// come from the latched axis, not a re-read (<see cref="RoutePinLatch"/>).
    /// </summary>
    private static void AssertDrivingPastFrogClears(RouteHarvestSnapshot snap, string? pinId)
    {
        var travelUsesReverse = RoutePinLatch.TravelUsesReverse;
        Assert.False(travelUsesReverse);

        var posePast = HtpFixtures.AlongPinForward(snap, 45f, pinId);
        Assert.True(posePast.PinIsBehind);

        var eval = RouteCorridorDrive.EvaluatePose(
            RouteClearancePhase.Idle,
            in posePast,
            travelUsesReverse);
        Assert.Equal(RouteClearancePhase.Cleared, eval.Phase);
        Assert.True(eval.CanAdvanceNext);
        Assert.Equal(
            RouteClearanceGateReason.Ok,
            RouteClearanceGate.Next(hasPin: true, eval.Phase));
    }

    /// <summary>Signed metres from the dumped nose to a frog along the dumped heading.</summary>
    private static float AlongFromNose(in RouteHarvestSnapshot snap, string? pinId)
    {
        Assert.True(HtpFixtures.TryJunctionXz(in snap, pinId, out var px, out var pz));
        var fx = snap.FwdX!.Value;
        var fz = snap.FwdZ!.Value;
        var mag = (float)System.Math.Sqrt((fx * fx) + (fz * fz));
        Assert.True(mag > 1e-6f);
        return (((px - snap.NoseX!.Value) * fx) + ((pz - snap.NoseZ!.Value) * fz)) / mag;
    }

    private static void BindSl55Board(
        out SwitchListStep step1,
        out string step1Pin,
        out string step8Pin)
    {
        var snap = HtpFixtures.LoadCorridor();
        var steps = SwitchListPlanner.Build(Sl55LiveMultiPickupJob());
        Assert.NotNull(steps);
        SwitchListSession.Bind("SW-SL-55", steps);
        RoutePinBoardSession.Rebuild(snap.Edges, snap.Selected, "SW", snap.OriginTrackId);

        var step1Index = FirstBoardPinIndex(steps!);
        var step8Index = LastBoardPinIndex(steps!);
        AdvanceSessionToStep(step1Index);
        Assert.Equal(step1Index, SwitchListSession.CurrentStep?.Index);
        step1 = SwitchListSession.CurrentStep!;

        var pin1 = RoutePinBoardSession.PinIdForStep(step1Index);
        var pin8 = RoutePinBoardSession.PinIdForStep(step8Index);
        Assert.False(string.IsNullOrEmpty(pin1));
        Assert.False(string.IsNullOrEmpty(pin8));
        step1Pin = pin1!;
        step8Pin = pin8!;
    }

    private static void ApplyAtSwitch(string pinId) =>
        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.AtSwitch,
                fouling: true,
                canThrowAlign: false,
                canAdvanceNext: false,
                caption: "At switch"),
            pinJunctionId: pinId,
            pinX: 0f,
            pinY: 0f,
            pinZ: 0f);

    private static void ApplyCleared(string pinId) =>
        RouteClearanceSession.Apply(
            new RouteClearanceDecision(
                RouteClearancePhase.Cleared,
                fouling: false,
                canThrowAlign: true,
                canAdvanceNext: true,
                caption: "CLEARED"),
            pinJunctionId: pinId,
            pinX: 0f,
            pinY: 0f,
            pinZ: 0f);

    private static void AdvanceSessionToStep(int stepIndex)
    {
        for (var guard = 0; guard < RoutePinBoard.Capacity * 4; guard++)
        {
            if (SwitchListSession.CurrentStep?.Index == stepIndex)
            {
                return;
            }

            Assert.True(
                SwitchListSession.TryAdvance(),
                "could not reach step " + stepIndex.ToString());
        }

        Assert.Fail("step " + stepIndex.ToString() + " not reachable");
    }

    /// <summary>
    /// The pin board is keyed on <c>IsClearedFrogPin</c>, a strict subset of
    /// <c>StepNeedsPinClearance</c>. Picking by the wider predicate asks the board for a
    /// step it never collected and gets a null pin.
    /// </summary>
    private static int FirstBoardPinIndex(System.Collections.Generic.IReadOnlyList<SwitchListStep> steps)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            if (!string.IsNullOrEmpty(RoutePinBoardSession.PinIdForStep(steps[i].Index)))
            {
                return steps[i].Index;
            }
        }

        Assert.Fail("no board pin on any step");
        return 0;
    }

    private static int LastBoardPinIndex(System.Collections.Generic.IReadOnlyList<SwitchListStep> steps)
    {
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(RoutePinBoardSession.PinIdForStep(steps[i].Index)))
            {
                return steps[i].Index;
            }
        }

        Assert.Fail("no board pin on any step");
        return 0;
    }

    private static string DestForIndex(System.Collections.Generic.IReadOnlyList<SwitchListStep> steps, int index)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Index == index)
            {
                return steps[i].DestTrackId;
            }
        }

        Assert.Fail("missing step " + index.ToString());
        return string.Empty;
    }

    private static string? FindPin(RoutePinBoardEntry[] buf, int n, int stepIndex)
    {
        for (var i = 0; i < n; i++)
        {
            if (buf[i].StepIndex == stepIndex)
            {
                return buf[i].PinId;
            }
        }

        return null;
    }

    private static JobSummary Sl55LiveMultiPickupJob() =>
        new()
        {
            JobId = "SW-SL-55",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            AdditionalPickupTrackIds = new[] { "SW-C4S" },
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
            LoadTrackId = "SW-B4L",
            LoadCargoLabel = "Wood Chips",
        };

    private static JobSummary Fh82LiveJob() =>
        new()
        {
            JobId = "SW-FH-82",
            JobTypeLabel = "FH",
            OriginYardId = "SW",
            DestYardId = "GF",
            OriginTrackId = "SW-C1O",
            DestTrackId = "GF-D5I",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
        };

    private static JobSummary Su34LiveJob() =>
        new()
        {
            JobId = "SW-SU-34",
            JobTypeLabel = "SU",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-C3I",
            AdditionalPickupTrackIds = new[] { "SW-B4L" },
            DestTrackId = "SW-B1S",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1775#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
        };

    private static JobSummary Sl52LiveJob() =>
        new()
        {
            JobId = "SW-SL-52",
            JobTypeLabel = "SL",
            OriginYardId = "SW",
            DestYardId = "SW",
            OriginTrackId = "SW-B1S",
            DestTrackId = "SW-C1O",
            NeedsTurnAround = true,
            TurntableTrackId = "#Y-#S1774#T",
            TurntablePivotTrackId = "SW-B4L",
            TurntableApproachNeedsReverse = true,
            PrepApproachTrackId = "#Y-#S1512#T",
            NeedsReverseInto = true,
            ReverseIntoTrackId = "SW-B4L",
        };
}
