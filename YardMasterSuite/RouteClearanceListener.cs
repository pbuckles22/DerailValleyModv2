using System;
using System.Collections.Generic;
using DV;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Golden <c>2.8.7.2</c> pin + reverse virtual nose: when Set Reverse,
    /// sample the butt (rear trainset car) as the leading edge.
    /// </summary>
    public sealed class RouteClearanceListener : MonoBehaviour
    {
        private const float PollSeconds = 0.35f;

        internal static Action<string>? EmitLog;

        private readonly float[] _lengthScratch = new float[64];
        private readonly float[] _hopLengthScratch = new float[128];
        private PathGraphMapper? _graph;
        private RouteClearanceTelemetryCache _log;
        private float _nextPoll;
        private RouteClearancePhase _phase = RouteClearancePhase.Idle;
        private bool _idleWhileLatchedLogged;
        private string? _approachHoldTrack;
        private string? _tailAlongHopLogged;

        private void OnEnable()
        {
            _graph = GetComponent<PathGraphMapper>();
            _log = default;
            _phase = RouteClearancePhase.Idle;
            _idleWhileLatchedLogged = false;
            _approachHoldTrack = null;
            _tailAlongHopLogged = null;
            _nextPoll = 0f;
            RouteClearanceSession.Clear();
            RoutePinLatch.Clear();
            YmsEventBus.OnMapsDestCommand += OnMapsDestCommand;
        }

        private void OnDisable()
        {
            YmsEventBus.OnMapsDestCommand -= OnMapsDestCommand;
            RouteClearanceSession.Clear();
            RoutePinLatch.Clear();
            _phase = RouteClearancePhase.Idle;
            _log = default;
        }

        private void OnMapsDestCommand(MapsDestCommand command)
        {
            if (command.Kind != MapsDestKind.Clear)
            {
                return;
            }

            _phase = RouteClearancePhase.Idle;
            RouteClearanceSession.Clear();
            RoutePinLatch.Clear();
            var line = RouteClearanceTelemetry.Observe(RouteClearancePhase.Idle, null, ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        private void Update()
        {
            if (!WorldSessionGate.IsActive())
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now < _nextPoll)
            {
                return;
            }

            _nextPoll = now + PollSeconds;
            try
            {
                EvaluateOnce();
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke("T2 route-pin: eval " + ex.GetType().Name);
            }
        }

        private void EvaluateOnce()
        {
            var plan = RoutePlanSession.Plan;
            if (plan == null
                || plan.Status == PathCheckStatus.NoPath
                || plan.Status == PathCheckStatus.NoOrigin
                || !RouteDestSession.HasDestination)
            {
                ApplyIdle();
                return;
            }

            if (!SwitchListRunner.PinDisplayAllowed(
                    SwitchListSession.CurrentStep,
                    SwitchListSession.HasActive))
            {
                ApplyIdle();
                return;
            }

            var pinId = RoutePinLatch.EffectivePin(plan);
            if (RoutePinLatch.DisplayDismissed
                || string.IsNullOrEmpty(pinId)
                || _graph == null
                || !_graph.TryGetJunction(pinId!, out var junction)
                || junction == null)
            {
                if (RoutePinLatch.HasLatch && !_idleWhileLatchedLogged)
                {
                    _idleWhileLatchedLogged = true;
                    EmitLog?.Invoke(
                        "T2 route-pin: idle while latched id="
                        + RoutePinLatch.Id
                        + (RoutePinLatch.DisplayDismissed ? " dismissed=1" : " junction=?"));
                }

                ApplyIdle();
                return;
            }

            _idleWhileLatchedLogged = false;

            if (!TryMeasure(plan, junction, out var nosePastM, out var lengthM, out var pinX, out var pinY, out var pinZ))
            {
                Commit(
                    RouteClearanceEval.Evaluate(
                        _phase,
                        new RouteClearanceSample(
                            hasPin: true,
                            nosePastJunctionM: 0f,
                            consistLengthM: 0f,
                            frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                            approachWindowM: RouteClearanceEval.DefaultApproachWindowM)),
                    pinId,
                    pinX,
                    pinY,
                    pinZ,
                    nosePastJunctionM: null,
                    consistLengthM: 0f);
                return;
            }

            var samePin = string.Equals(
                RouteClearanceSession.PinJunctionId,
                pinId,
                StringComparison.Ordinal);
            string? tailTrack = null;
            var tailOnPlan = plan != null
                && TryTailTrackId(plan, out tailTrack)
                && plan.ContainsTrack(tailTrack);
            var tailBeforeExit = RouteTailAlongTrack.ShouldHoldApproachForTail(
                tailOnPlan,
                tailOnPlan && plan!.TailStillBeforePinExit(pinId, tailTrack));
            if (tailBeforeExit)
            {
                if (!string.Equals(_approachHoldTrack, tailTrack, StringComparison.Ordinal))
                {
                    _approachHoldTrack = tailTrack;
                    EmitLog?.Invoke(RouteClearanceTelemetry.StillApproach + tailTrack);
                }

                nosePastM = RouteClearanceTravel.NoseHeldOnApproachSide();
                var held = new RouteClearanceSample(
                    hasPin: true,
                    nosePastJunctionM: nosePastM,
                    consistLengthM: lengthM,
                    frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                    approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
                Commit(
                    RouteClearanceEval.Evaluate(RouteClearancePhase.Approaching, in held),
                    pinId,
                    pinX,
                    pinY,
                    pinZ,
                    nosePastM,
                    lengthM);
                return;
            }

            _approachHoldTrack = null;

            // Plan present: CLEARED only from along-track tail past the pin.
            // Straight-line lead-car forward is not enough (cab 2.16.30 rem=0).
            if (TryAlongTrackNosePast(
                    plan!,
                    pinId!,
                    lengthM,
                    out var alongNosePast,
                    out var tailPast,
                    out var leadHopId))
            {
                nosePastM = RouteClearanceTravel.StabilizeNosePast(
                    samePin && RouteClearanceSession.SawAtSwitchThisLeg,
                    RouteClearanceSession.BestNosePastMeters,
                    alongNosePast,
                    lengthM);
                if (!string.Equals(_tailAlongHopLogged, leadHopId, StringComparison.Ordinal))
                {
                    _tailAlongHopLogged = leadHopId;
                    EmitLog?.Invoke(RouteClearanceTelemetry.FormatTailAlong(tailPast, leadHopId));
                }
            }
            else
            {
                // Off the plan after At switch: keep the saved along-track sample.
                // A raw 3D dot is not a clearance (cab 2.16.30).
                nosePastM = RouteClearanceTravel.NosePastWhenAlongTrackLost(
                    samePin,
                    RouteClearanceSession.SawAtSwitchThisLeg,
                    RouteClearanceSession.BestNosePastMeters,
                    lengthM);
                if (!string.Equals(_tailAlongHopLogged, "?", StringComparison.Ordinal))
                {
                    _tailAlongHopLogged = "?";
                    EmitLog?.Invoke(RouteClearanceTelemetry.FormatTailAlong(float.NaN, hopId: null));
                }
            }

            var sample = new RouteClearanceSample(
                hasPin: true,
                nosePastJunctionM: nosePastM,
                consistLengthM: lengthM,
                frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
            Commit(RouteClearanceEval.Evaluate(_phase, in sample), pinId, pinX, pinY, pinZ, nosePastM, lengthM);
        }

        private void ApplyIdle()
        {
            Commit(
                RouteClearanceEval.Evaluate(
                    _phase,
                    new RouteClearanceSample(
                        hasPin: false,
                        nosePastJunctionM: 0f,
                        consistLengthM: 0f,
                        frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                        approachWindowM: RouteClearanceEval.DefaultApproachWindowM)),
                pinJunctionId: null,
                pinX: 0f,
                pinY: 0f,
                pinZ: 0f,
                nosePastJunctionM: null,
                consistLengthM: 0f);
        }

        private void Commit(
            in RouteClearanceDecision decision,
            string? pinJunctionId,
            float pinX,
            float pinY,
            float pinZ,
            float? nosePastJunctionM = null,
            float consistLengthM = 0f)
        {
            _phase = decision.Phase;
            RouteClearanceSession.Apply(
                in decision,
                pinJunctionId,
                pinX,
                pinY,
                pinZ,
                nosePastJunctionM,
                consistLengthM);
            var line = RouteClearanceTelemetry.Observe(
                RouteClearanceSession.Phase,
                RouteClearanceSession.Caption,
                ref _log);
            if (line != null)
            {
                EmitLog?.Invoke(line);
            }
        }

        /// <summary>
        /// True when this frog is already CLEARED for the consist (spent stop).
        /// Uses that junction's behind/ahead axis, not the previous latch.
        /// </summary>
        internal bool IsJunctionAlreadyCleared(PathPlanResult plan, string? junctionId)
        {
            var id = junctionId?.Trim();
            if (string.IsNullOrEmpty(id)
                || _graph == null
                || !_graph.TryGetJunction(id!, out var junction)
                || junction == null)
            {
                return false;
            }

            if (!TryMeasure(
                    plan,
                    junction,
                    out var nosePastM,
                    out var lengthM,
                    out _,
                    out _,
                    out _,
                    useCandidateTravelReverse: true))
            {
                return false;
            }

            if (TryAlongTrackNosePast(
                    plan,
                    id!,
                    lengthM,
                    out var alongNose,
                    out _,
                    out _))
            {
                nosePastM = alongNose;
            }
            else
            {
                return false;
            }

            var sample = new RouteClearanceSample(
                hasPin: true,
                nosePastJunctionM: nosePastM,
                consistLengthM: lengthM,
                frogEnvelopeM: RouteClearanceEval.DefaultFrogEnvelopeM,
                approachWindowM: RouteClearanceEval.DefaultApproachWindowM);
            return RouteClearanceEval.IsClearedOfFrog(in sample);
        }

        /// <summary>
        /// Along-corridor nosePast for CLEARED. Fail closed when hops / lead /
        /// approach cannot be resolved — never CLEAR from a flipped car forward.
        /// </summary>
        private bool TryAlongTrackNosePast(
            PathPlanResult plan,
            string pinId,
            float consistLengthM,
            out float nosePastM,
            out float tailPastM,
            out string? leadHopId)
        {
            nosePastM = 0f;
            tailPastM = 0f;
            leadHopId = null;
            if (_graph == null
                || consistLengthM <= 0f
                || plan.TrackIds == null
                || plan.TrackIds.Count == 0)
            {
                return false;
            }

            var approachIndex = -1;
            if (plan.TryGetApproachTrack(pinId, out var approachId)
                && !string.IsNullOrEmpty(approachId))
            {
                approachIndex = RouteTailAlongTrack.IndexOfHop(plan.TrackIds, approachId);
            }

            if (approachIndex < 0
                && !RouteTailAlongTrack.TryFindApproachHopIndex(
                    plan.TrackIds,
                    _graph.PathCheckEdges,
                    pinId,
                    out approachIndex))
            {
                return false;
            }

            if (!TryResolveConsist(out var cars, out var solo))
            {
                return false;
            }

            var reverse = RoutePinLatch.HasLatch
                ? RoutePinLatch.TravelUsesReverse
                : RouteFacingResolver.IsTargetBehind(plan, _graph);
            var multi = cars != null && cars.Count > 1;
            var lead = PickLeadCar(cars, solo, travelReverse: reverse && multi);
            if (lead == null
                || !TryCarTrackPose(lead, out var leadTrackId, out var spanMeters, out var leadTrackLen)
                || string.IsNullOrEmpty(leadTrackId))
            {
                return false;
            }

            leadHopId = leadTrackId;
            var leadIndex = RouteTailAlongTrack.IndexOfHop(plan.TrackIds, leadTrackId);
            var tailBogieOnPlan = false;
            if (leadIndex < 0)
            {
                // Nose bogie left the plan. The other bogie may still be on the stem.
                if (!TryBogiePose(lead, rear: true, out var rearTrackId, out spanMeters, out leadTrackLen)
                    || !RouteTailAlongTrack.TryOnPlanBogieHop(
                        plan.TrackIds,
                        frontTrackId: null,
                        rearTrackId,
                        out leadIndex,
                        out _))
                {
                    return false;
                }

                leadTrackId = rearTrackId;
                leadHopId = rearTrackId;
                tailBogieOnPlan = true;
            }

            var hopCount = plan.TrackIds.Count;
            if (hopCount > _hopLengthScratch.Length)
            {
                return false;
            }

            for (var i = 0; i < hopCount; i++)
            {
                if (!TryHopLengthMeters(plan.TrackIds[i], out var len) || len <= 0f)
                {
                    return false;
                }

                _hopLengthScratch[i] = len;
            }

            // Prefer live lead-track length over the graph cache.
            if (leadTrackLen > 0f)
            {
                _hopLengthScratch[leadIndex] = leadTrackLen;
            }

            var travelIncreasing = TravelIncreasesSpanOnHop(plan, leadIndex, leadTrackId!);
            var into = TrackPathSpan.WithinTrackMeters(
                spanMeters,
                _hopLengthScratch[leadIndex],
                travelIncreasing);

            if (tailBogieOnPlan)
            {
                if (!RouteTailAlongTrack.TryPointPastPin(
                        plan.TrackIds,
                        _hopLengthScratch,
                        leadIndex,
                        into,
                        approachIndex,
                        out tailPastM))
                {
                    return false;
                }
            }
            else if (!RouteTailAlongTrack.TryTailPastPin(
                    plan.TrackIds,
                    _hopLengthScratch,
                    leadIndex,
                    into,
                    consistLengthM,
                    approachIndex,
                    out tailPastM))
            {
                return false;
            }

            nosePastM = RouteTailAlongTrack.NosePastFromTail(tailPastM, consistLengthM);
            return true;
        }

        private bool TryHopLengthMeters(string? trackId, out float lengthMeters)
        {
            lengthMeters = 0f;
            if (_graph == null || string.IsNullOrEmpty(trackId))
            {
                return false;
            }

            if (!_graph.TryGetRailTrack(trackId!, out var rail) || rail == null)
            {
                return false;
            }

            lengthMeters = PathTrackProbe.LengthMeters(rail);
            return lengthMeters > 0f;
        }

        private bool TravelIncreasesSpanOnHop(PathPlanResult plan, int hopIndex, string hopId)
        {
            if (_graph == null
                || hopIndex < 0
                || hopIndex >= plan.TrackIds.Count
                || !_graph.TryGetRailTrack(hopId, out var rail)
                || rail == null)
            {
                return true;
            }

            // Next hop on the plan: prefer the rail end closer to that neighbor.
            string? neighborId = null;
            if (hopIndex + 1 < plan.TrackIds.Count)
            {
                neighborId = plan.TrackIds[hopIndex + 1];
            }
            else if (hopIndex > 0)
            {
                neighborId = plan.TrackIds[hopIndex - 1];
            }

            if (string.IsNullOrEmpty(neighborId)
                || !_graph.TryGetRailTrack(neighborId!, out var neighbor)
                || neighbor == null)
            {
                return true;
            }

            try
            {
                var inPos = rail.curve != null && rail.curve.pointCount > 0
                    ? rail.curve[0].position
                    : rail.transform.position;
                var outPos = rail.curve != null && rail.curve.pointCount > 1
                    ? rail.curve[rail.curve.pointCount - 1].position
                    : inPos;
                var nIn = neighbor.curve != null && neighbor.curve.pointCount > 0
                    ? neighbor.curve[0].position
                    : neighbor.transform.position;
                var nOut = neighbor.curve != null && neighbor.curve.pointCount > 1
                    ? neighbor.curve[neighbor.curve.pointCount - 1].position
                    : nIn;

                var toNextFromOut = MinDistSq(outPos, nIn, nOut);
                var toNextFromIn = MinDistSq(inPos, nIn, nOut);
                if (hopIndex + 1 < plan.TrackIds.Count)
                {
                    // Leaving toward next hop: increasing span when Out is nearer next.
                    return toNextFromOut <= toNextFromIn;
                }

                // Last hop: entered from previous at the nearer end.
                return toNextFromIn <= toNextFromOut;
            }
            catch
            {
                return true;
            }
        }

        private static float MinDistSq(Vector3 a, Vector3 b, Vector3 c)
        {
            var db = (a - b).sqrMagnitude;
            var dc = (a - c).sqrMagnitude;
            return db <= dc ? db : dc;
        }

        private static bool TryCarTrackPose(
            TrainCar car,
            out string? logicTrackId,
            out float spanMeters,
            out float trackLengthMeters) =>
            TryBogiePose(car, rear: false, out logicTrackId, out spanMeters, out trackLengthMeters)
            || TryBogiePose(car, rear: true, out logicTrackId, out spanMeters, out trackLengthMeters);

        private static bool TryBogiePose(
            TrainCar car,
            bool rear,
            out string? logicTrackId,
            out float spanMeters,
            out float trackLengthMeters)
        {
            logicTrackId = null;
            spanMeters = float.NaN;
            trackLengthMeters = 0f;
            try
            {
                var bogie = rear ? car.RearBogie : car.FrontBogie;
                if (bogie == null || bogie.track == null || bogie.traveller == null)
                {
                    return false;
                }

                logicTrackId = LogicTrackKey.FromRail(bogie.track);
                if (string.IsNullOrEmpty(logicTrackId))
                {
                    return false;
                }

                spanMeters = (float)bogie.traveller.Span;
                trackLengthMeters = PathTrackProbe.LengthMeters(bogie.track);
                return trackLengthMeters > 0f;
            }
            catch
            {
                logicTrackId = null;
                spanMeters = float.NaN;
                trackLengthMeters = 0f;
                return false;
            }
        }

        private bool TryMeasure(
            PathPlanResult plan,
            Junction junction,
            out float nosePastM,
            out float lengthM,
            out float pinX,
            out float pinY,
            out float pinZ,
            bool useCandidateTravelReverse = false)
        {
            nosePastM = 0f;
            lengthM = 0f;
            pinX = pinY = pinZ = 0f;

            if (!JunctionPinWorld.TryGet(junction, out pinX, out pinY, out pinZ))
            {
                return false;
            }

            if (!TryResolveConsist(out var cars, out var solo))
            {
                return false;
            }

            var reverse = RoutePinLatch.HasLatch
                ? RoutePinLatch.TravelUsesReverse
                : RouteFacingResolver.IsTargetBehind(plan, _graph);
            var multi = cars != null && cars.Count > 1;
            var lead = PickLeadCar(cars, solo, travelReverse: reverse && multi);
            if (lead == null)
            {
                return false;
            }

            lengthM = cars != null && cars.Count > 0
                ? MeasureLength(cars)
                : MeasureSingle(lead);
            ConsistLengthSession.Observe(lengthM);

            if (lengthM <= 0f)
            {
                return false;
            }

            Vector3 nose;
            Vector3 fwd;
            try
            {
                var t = lead.transform;
                nose = t.position;
                fwd = t.forward;
            }
            catch
            {
                return false;
            }

            fwd.y = 0f;
            var mag = Mathf.Sqrt((fwd.x * fwd.x) + (fwd.z * fwd.z));
            if (mag < 1e-4f)
            {
                return false;
            }

            fwd.x /= mag;
            fwd.z /= mag;

            if (useCandidateTravelReverse)
            {
                reverse = DriveSetFacing.IsTargetBehind(
                    fwd.x, fwd.z, pinX - nose.x, pinZ - nose.z);
                if (reverse && multi)
                {
                    lead = PickLeadCar(cars, solo, travelReverse: true);
                    if (lead == null)
                    {
                        return false;
                    }

                    try
                    {
                        var t = lead.transform;
                        nose = t.position;
                        fwd = t.forward;
                    }
                    catch
                    {
                        return false;
                    }

                    fwd.y = 0f;
                    mag = Mathf.Sqrt((fwd.x * fwd.x) + (fwd.z * fwd.z));
                    if (mag < 1e-4f)
                    {
                        return false;
                    }

                    fwd.x /= mag;
                    fwd.z /= mag;
                }
            }

            var goldenNosePast = ((nose.x - pinX) * fwd.x) + ((nose.z - pinZ) * fwd.z);
            nosePastM = reverse && solo
                ? RouteClearanceTravel.SampleTravelPastM(
                    nose.x,
                    nose.z,
                    pinX,
                    pinZ,
                    fwd.x,
                    fwd.z,
                    lengthM,
                    travelUsesReverse: true,
                    soloConsist: true)
                : reverse
                    ? RouteClearanceTravel.LeadingEdgePastM(goldenNosePast, travelReverse: true)
                    : goldenNosePast;
            return true;
        }

        private static float MeasureSingle(TrainCar car) => ReadCarLength(car);

        private static float ReadCarLength(TrainCar car)
        {
            var coupler = 0f;
            var bounds = 0f;
            try
            {
                coupler = car.InterCouplerDistance;
            }
            catch
            {
                // fall through
            }

            try
            {
                bounds = car.Bounds.size.z;
            }
            catch
            {
                // fall through
            }

            return ConsistLengthMeters.OccupancyCar(coupler, bounds);
        }

        private float MeasureLength(IList<TrainCar> cars)
        {
            var n = 0;
            for (var i = 0; i < cars.Count && n < _lengthScratch.Length; i++)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                _lengthScratch[n++] = ReadCarLength(car);
            }

            if (n == 0)
            {
                return 0f;
            }

            return ConsistLengthMeters.Sum(_lengthScratch, n);
        }

        private static bool TryResolveConsist(out IList<TrainCar>? cars, out TrainCar? solo)
        {
            cars = null;
            solo = null;
            try
            {
                var car = PlayerManager.Car ?? PlayerManager.LastLoco;
                if (car == null)
                {
                    return false;
                }

                cars = car.trainset != null ? car.trainset.cars : null;
                if (cars == null || cars.Count == 0)
                {
                    solo = car;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryTailTrackId(PathPlanResult plan, out string? trackId)
        {
            trackId = null;
            if (!TryResolveConsist(out var cars, out var solo))
            {
                return false;
            }

            var reverse = RoutePinLatch.HasLatch
                ? RoutePinLatch.TravelUsesReverse
                : RouteFacingResolver.IsTargetBehind(plan, _graph);
            var tail = PickLeadCar(cars, solo, travelReverse: !reverse);
            trackId = LogicTrackKey.FromCar(tail);
            return !string.IsNullOrEmpty(trackId);
        }

        private static TrainCar? PickLeadCar(IList<TrainCar>? cars, TrainCar? solo, bool travelReverse)
        {
            if (cars == null || cars.Count == 0)
            {
                return solo;
            }

            var min = int.MaxValue;
            var max = int.MinValue;
            TrainCar? minCar = null;
            TrainCar? maxCar = null;
            for (var i = 0; i < cars.Count; i++)
            {
                var c = cars[i];
                if (c == null)
                {
                    continue;
                }

                var idx = c.indexInTrainset;
                if (idx < min)
                {
                    min = idx;
                    minCar = c;
                }

                if (idx > max)
                {
                    max = idx;
                    maxCar = c;
                }
            }

            if (minCar == null)
            {
                return solo;
            }

            var want = ConsistTravelLead.LeadingIndex(min, max, travelReverse);
            return want == max ? maxCar : minCar;
        }
    }
}
