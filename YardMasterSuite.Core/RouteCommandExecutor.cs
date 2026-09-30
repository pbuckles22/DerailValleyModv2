using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>What the Unity side must do this tick for a <see cref="RouteCommandExecutor"/> step.</summary>
public enum RouteExecAction
{
    Idle = 0,
    Drive = 1,
    Brake = 2,
    Throw = 3,
    Align = 4,
    Done = 5,
    Hold = 6,
}

/// <summary>Live sensors the executor reads. Unity fills it from Core sessions.</summary>
public readonly struct RouteExecInput
{
    public RouteExecInput(
        bool hasPlan,
        bool hasDispatcher,
        RouteClearancePhase phase,
        string? pinJunctionId,
        float? remToClearedMeters,
        float? remainingMeters,
        float speedKmh,
        float consistLengthMeters,
        float? carClearanceMeters = null,
        bool tipCoupled = false,
        bool aimIsStall = false)
    {
        HasPlan = hasPlan;
        HasDispatcher = hasDispatcher;
        Phase = phase;
        PinJunctionId = pinJunctionId;
        RemToClearedMeters = remToClearedMeters;
        RemainingMeters = remainingMeters;
        SpeedKmh = speedKmh;
        ConsistLengthMeters = consistLengthMeters;
        CarClearanceMeters = carClearanceMeters;
        TipCoupled = tipCoupled;
        AimIsStall = aimIsStall;
    }

    public bool HasPlan { get; }
    public bool HasDispatcher { get; }
    public RouteClearancePhase Phase { get; }
    public string? PinJunctionId { get; }
    public float? RemToClearedMeters { get; }

    /// <summary>
    /// Final-leg meters. Stall aim: signed distance until the tail is
    /// <see cref="RouteCommandExecutor.StallMarginMeters"/> past the dest entry.
    /// Otherwise corridor meters to the path end.
    /// </summary>
    public float? RemainingMeters { get; }
    public float SpeedKmh { get; }
    public float ConsistLengthMeters { get; }

    /// <summary>Meters to a car ahead of the leading end. Null when none in range.</summary>
    public float? CarClearanceMeters { get; }

    /// <summary>
    /// The travel-direction knuckle is coupled to a car outside this consist.
    /// Not the car already coupled on the other end.
    /// </summary>
    public bool TipCoupled { get; }

    /// <summary>Remaining is stall clearance, not path-end leftover. Stop threshold is 0.</summary>
    public bool AimIsStall { get; }
}

public readonly struct RouteExecDecision
{
    public RouteExecDecision(
        RouteExecAction action,
        bool travelReverse = false,
        float requestKmh = 0f,
        string? targetId = null,
        int branch = -1,
        string? legPinId = null,
        string? reason = null)
    {
        Action = action;
        TravelReverse = travelReverse;
        RequestKmh = requestKmh;
        TargetId = targetId;
        Branch = branch;
        LegPinId = legPinId;
        Reason = reason;
    }

    public RouteExecAction Action { get; }
    public bool TravelReverse { get; }

    /// <summary>Drive: governor request (km/h). Brake / Done / Hold: 0.</summary>
    public float RequestKmh { get; }

    /// <summary>Throw: junction id. Align: pivot track.</summary>
    public string? TargetId { get; }
    public int Branch { get; }

    /// <summary>Junction this leg must clear before its Stop; null on the final leg.</summary>
    public string? LegPinId { get; }
    public string? Reason { get; }
}

/// <summary>Cursor into the frozen queue. Survives Align re-freezing the plan.</summary>
public struct RouteExecState
{
    public int Index;
    public bool TravelReverse;
    public bool AwaitingThrow;
    public bool Finished;
    public string? HoldReason;

    /// <summary>
    /// Final leg has been inside the couple scan. A later lost gap is the
    /// couple (the car joined the consist), not the far end of the rail.
    /// </summary>
    public bool SawFinalKiss;
}

/// <summary>
/// 16.2 ship 2: walks the <see cref="RouteCommandParser"/> queue. Drive to CLEARED →
/// Stop (full rest) → Throw (or Align) → direction → Drive to the dest end. Pure: the
/// Unity listener performs Throw / Align and reports back via <see cref="ReportThrow"/>.
/// </summary>
public static class RouteCommandExecutor
{
    /// <summary>Stop this far short of the dest track end, plus the consist length.</summary>
    public const float DestEndPadMeters = 15f;

    /// <summary>Tail must sit this far past the dest-track entry to count as fully in.</summary>
    public const float StallMarginMeters = 5f;

    /// <summary>
    /// Inside this gap, a car approach requests 3 km/h so catch-down arms.
    /// Cab 2.16.34.4 held ~24 km/h down to a 7 m rear gap because the heavy
    /// brake only engages at a 3 km/h request. This is not a full stop.
    /// </summary>
    public const float CarFullBrakeMeters = 45f;

    /// <summary>Full brake only inside the couple scan.</summary>
    public const float CarKissMeters = BackupProximityDisplay.CoupleNearRangeMeters;

    /// <summary>
    /// Throttle-off coast once the gap is the couple scan and speed is already
    /// inside the couple limit. Not a go-stop. Cab 2.16.34.6 snapped the
    /// independent to 100 at 3 km/h and the knuckle opened again.
    /// </summary>
    public const float KissCoastKmh = 1f;

    public const string ReasonPlanStale = "plan stale";
    public const string ReasonNeedDispatcher = "need Dispatcher";
    public const string ReasonWaitPin = "wait pin";
    public const string ReasonWaitRemaining = "wait rem";
    public const string ReasonStopping = "stopping";
    public const string ReasonAwaitThrow = "await throw";
    public const string ReasonThrowFailed = "throw failed";
    public const string ReasonArriving = "arriving";

    /// <summary>Pin leg: a car nearer than the frog owns the request.</summary>
    public const string ReasonCarAhead = "car ahead";

    /// <summary>Final leg, approach knuckle shut: throttle off, stay on Drive.</summary>
    public const string ReasonKnuckle = "knuckle";

    public static RouteExecState Begin(IReadOnlyList<LocoCommand>? cmds)
    {
        var state = default(RouteExecState);
        if (cmds != null && cmds.Count > 0)
        {
            state.TravelReverse = cmds[0].TravelReverse;
        }

        return state;
    }

    public static RouteExecDecision Tick(
        IReadOnlyList<LocoCommand>? cmds,
        ref RouteExecState state,
        in RouteExecInput input)
    {
        if (cmds == null || cmds.Count == 0)
        {
            return new RouteExecDecision(RouteExecAction.Idle);
        }

        if (state.Finished)
        {
            return new RouteExecDecision(RouteExecAction.Done, state.TravelReverse);
        }

        if (state.HoldReason != null)
        {
            return new RouteExecDecision(RouteExecAction.Hold, state.TravelReverse, reason: state.HoldReason);
        }

        if (!input.HasPlan)
        {
            state.HoldReason = ReasonPlanStale;
            return new RouteExecDecision(RouteExecAction.Hold, state.TravelReverse, reason: ReasonPlanStale);
        }

        // Each pass either returns or advances Index, so Count + 1 passes is enough.
        for (var guard = 0; guard <= cmds.Count; guard++)
        {
            if (state.Index >= cmds.Count)
            {
                if (!PidGoStop.IsFullyStopped(input.SpeedKmh))
                {
                    return new RouteExecDecision(RouteExecAction.Brake, state.TravelReverse, reason: ReasonArriving);
                }

                state.Finished = true;
                return new RouteExecDecision(RouteExecAction.Done, state.TravelReverse);
            }

            var c = cmds[state.Index];
            switch (c.Action)
            {
                case LocoCommandAction.Drive:
                {
                    state.TravelReverse = c.TravelReverse;
                    var hasNext = state.Index + 1 < cmds.Count;
                    if (hasNext && cmds[state.Index + 1].Action == LocoCommandAction.Stop)
                    {
                        var pin = LegPinAfter(cmds, state.Index);
                        if (!PinMatches(pin, input.PinJunctionId))
                        {
                            // Cab 2.16.34.1: braking here deadlocked C4S. The route list's
                            // pin board was a different frog, so this leg's pin never
                            // arrived and the train sat on the brakes. Roll toward it.
                            return new RouteExecDecision(
                                RouteExecAction.Drive,
                                c.TravelReverse,
                                YardApproachKinematics.ApproachSpeedKmh,
                                c.TargetId,
                                legPinId: pin,
                                reason: ReasonWaitPin);
                        }

                        if (input.Phase == RouteClearancePhase.Cleared)
                        {
                            state.Index++;
                            continue;
                        }

                        var request = input.RemToClearedMeters is float r && r >= 0f && !float.IsNaN(r)
                            ? YardApproachKinematics.ResolveTargetSpeedKmh(r)
                            : YardApproachKinematics.ApproachSpeedKmh;

                        // Run A: the pin is still beyond a car on this leg. Pin rem
                        // stays cruise while Front closes to 2.1 m. The nearer car
                        // owns the request once it is slower. Do not advance.
                        var carAhead = KnownMeters(input.CarClearanceMeters);
                        if (!input.TipCoupled
                            && carAhead is float gap
                            && (input.RemToClearedMeters is not float pinRem || gap < pinRem))
                        {
                            EvaluateCarApproach(gap, input.SpeedKmh, out var carAim, out _);
                            if (carAim < request)
                            {
                                return new RouteExecDecision(
                                    RouteExecAction.Drive,
                                    c.TravelReverse,
                                    carAim,
                                    c.TargetId,
                                    legPinId: pin,
                                    reason: ReasonCarAhead);
                            }
                        }

                        return new RouteExecDecision(
                            RouteExecAction.Drive,
                            c.TravelReverse,
                            request,
                            c.TargetId,
                            legPinId: pin);
                    }

                    if (hasNext)
                    {
                        state.Index++;
                        continue;
                    }

                    var gapGone = KnownMeters(input.CarClearanceMeters) == null;
                    if (input.TipCoupled || (state.SawFinalKiss && gapGone))
                    {
                        if (!PidGoStop.IsFullyStopped(input.SpeedKmh))
                        {
                            return new RouteExecDecision(
                                RouteExecAction.Drive,
                                c.TravelReverse,
                                0f,
                                c.TargetId,
                                reason: ReasonKnuckle);
                        }

                        state.Index++;
                        continue;
                    }

                    if (!TryFinalAim(in input, out var dist, out var stopAt, out var nearerIsCar))
                    {
                        return new RouteExecDecision(
                            RouteExecAction.Brake,
                            c.TravelReverse,
                            reason: ReasonWaitRemaining);
                    }

                    if (!nearerIsCar)
                    {
                        if (dist <= stopAt)
                        {
                            state.Index++;
                            continue;
                        }

                        return new RouteExecDecision(
                            RouteExecAction.Drive,
                            c.TravelReverse,
                            YardApproachKinematics.ResolveTargetSpeedKmh(dist - stopAt),
                            c.TargetId);
                    }

                    if (dist <= CarKissMeters)
                    {
                        state.SawFinalKiss = true;
                    }

                    EvaluateCarApproach(dist, input.SpeedKmh, out var aim, out var allowFullBrake);
                    if (allowFullBrake)
                    {
                        state.Index++;
                        continue;
                    }

                    return new RouteExecDecision(
                        RouteExecAction.Drive,
                        c.TravelReverse,
                        aim,
                        c.TargetId);
                }

                case LocoCommandAction.Stop:
                    if (!PidGoStop.IsFullyStopped(input.SpeedKmh))
                    {
                        return new RouteExecDecision(RouteExecAction.Brake, state.TravelReverse, reason: ReasonStopping);
                    }

                    state.Index++;
                    continue;

                case LocoCommandAction.ThrowSwitch:
                    if (!input.HasDispatcher)
                    {
                        state.HoldReason = ReasonNeedDispatcher;
                        return new RouteExecDecision(RouteExecAction.Hold, state.TravelReverse, reason: ReasonNeedDispatcher);
                    }

                    if (state.AwaitingThrow)
                    {
                        return new RouteExecDecision(RouteExecAction.Brake, state.TravelReverse, reason: ReasonAwaitThrow);
                    }

                    state.AwaitingThrow = true;
                    return c.TargetIsJunction && c.RequiredBranch >= 0
                        ? new RouteExecDecision(
                            RouteExecAction.Throw,
                            state.TravelReverse,
                            targetId: c.TargetId,
                            branch: c.RequiredBranch)
                        : new RouteExecDecision(
                            RouteExecAction.Align,
                            state.TravelReverse,
                            targetId: c.TargetId);

                case LocoCommandAction.ChangeDirection:
                    state.TravelReverse = c.TravelReverse;
                    state.Index++;
                    continue;

                default:
                    state.Index++;
                    continue;
            }
        }

        return new RouteExecDecision(RouteExecAction.Brake, state.TravelReverse, reason: ReasonStopping);
    }

    /// <summary>Unity reports the Throw / Align it just ran. Failure holds the train.</summary>
    public static void ReportThrow(ref RouteExecState state, bool ok)
    {
        if (!state.AwaitingThrow)
        {
            return;
        }

        state.AwaitingThrow = false;
        if (ok)
        {
            state.Index++;
        }
        else
        {
            state.HoldReason = ReasonThrowFailed;
        }
    }

    /// <summary>
    /// Car approach. Request 3 km/h inside <see cref="CarFullBrakeMeters"/> so
    /// catch-down arms. The couple-scan go-stop waits until the train is
    /// already stopped. A gap under 0.2 m while still above couple speed is
    /// the emergency.
    /// </summary>
    public static void EvaluateCarApproach(
        float clearanceMeters,
        float speedKmh,
        out float requestKmh,
        out bool allowFullBrake)
    {
        requestKmh = YardApproachKinematics.ResolveTargetSpeedKmh(clearanceMeters - CarKissMeters);
        if (clearanceMeters <= CarFullBrakeMeters)
        {
            requestKmh = YardApproachKinematics.TouchdownSpeedKmh;
        }

        var stopped = speedKmh <= PidGoStop.FullyStoppedKmh;
        var emergency = clearanceMeters <= 0.2f
            && speedKmh > AutoCoupleAssist.MaxCoupleSpeedKmh;
        allowFullBrake = clearanceMeters <= CarKissMeters && (stopped || emergency);
        if (!allowFullBrake
            && clearanceMeters <= CarKissMeters
            && speedKmh <= AutoCoupleAssist.MaxCoupleSpeedKmh)
        {
            requestKmh = KissCoastKmh;
        }
    }

    /// <summary>
    /// Final-leg aim. Stall aim is the B4L pin stop: tail clearance only.
    /// A car on that track is not a couple. A plain corridor still may kiss
    /// a nearer car, and an empty corridor stops at the end pad.
    /// </summary>
    private static bool TryFinalAim(
        in RouteExecInput input,
        out float dist,
        out float stopAt,
        out bool nearerIsCar)
    {
        dist = 0f;
        stopAt = 0f;
        nearerIsCar = false;
        var car = KnownMeters(input.CarClearanceMeters);
        if (input.AimIsStall)
        {
            if (input.RemainingMeters is not float stall
                || float.IsNaN(stall)
                || float.IsInfinity(stall))
            {
                return false;
            }

            dist = stall < 0f ? 0f : stall;
            stopAt = 0f;
            return true;
        }

        var corridor = KnownMeters(input.RemainingMeters);
        if (car == null && corridor == null)
        {
            return false;
        }

        nearerIsCar = car is float carM && (corridor is not float track || carM < track);
        dist = nearerIsCar ? car!.Value : corridor!.Value;
        stopAt = nearerIsCar
            ? CarKissMeters
            : DestStopRemMeters(input.ConsistLengthMeters);
        return true;
    }

    private static float? KnownMeters(float? value) =>
        value is float v && v >= 0f && !float.IsNaN(v) ? v : null;

    /// <summary>Corridor rem at which the final leg stops (end pad + consist, either end may lead).</summary>
    public static float DestStopRemMeters(float consistLengthMeters) =>
        DestEndPadMeters + (consistLengthMeters > 0f && !float.IsNaN(consistLengthMeters) ? consistLengthMeters : 0f);

    /// <summary>
    /// Track whose entry the final Drive measures. The route dest wins.
    /// A sawtooth step can name the other end (C4S leg, step dest B4L).
    /// </summary>
    public static string? FinalLegStallTrack(string? routeDestTrackId, string? stepDestTrackId)
    {
        var route = routeDestTrackId?.Trim();
        if (!string.IsNullOrEmpty(route))
        {
            return route;
        }

        var step = stepDestTrackId?.Trim();
        return string.IsNullOrEmpty(step) ? null : step;
    }

    /// <summary>
    /// Meters until the tail is <see cref="StallMarginMeters"/> past the dest entry.
    /// Negative signed entry means the nose has already crossed the switch.
    /// Null when the entry or the consist length is missing — the caller holds.
    /// </summary>
    public static float? StallRemainingMeters(float? signedMetersToEntry, float consistLengthMeters)
    {
        if (signedMetersToEntry is not float signed
            || float.IsNaN(signed)
            || float.IsInfinity(signed)
            || !(consistLengthMeters > 0f)
            || float.IsNaN(consistLengthMeters)
            || float.IsInfinity(consistLengthMeters))
        {
            return null;
        }

        return signed + consistLengthMeters + StallMarginMeters;
    }

    /// <summary>
    /// Far dest: the entry is not on the live path yet, so cruise the corridor.
    /// Once the entry is known, that stall distance replaces the path end.
    /// </summary>
    public static float? DriveRemainingMeters(float? stallRemainingMeters, float? pathEndMeters) =>
        stallRemainingMeters ?? pathEndMeters;

    /// <summary>Junction the Drive at <paramref name="driveIndex"/> must clear (its Stop's throw).</summary>
    public static string? LegPinAfter(IReadOnlyList<LocoCommand> cmds, int driveIndex)
    {
        var throwIndex = driveIndex + 2;
        if (throwIndex >= cmds.Count)
        {
            return null;
        }

        var t = cmds[throwIndex];
        return t.Action == LocoCommandAction.ThrowSwitch && t.TargetIsJunction
            ? t.TargetId
            : null;
    }

    /// <summary>
    /// CLEARED only counts for this leg's frog. A leftover pin (previous leg) must not
    /// trip the Stop. Align@track legs accept whichever pin the latch holds.
    /// </summary>
    private static bool PinMatches(string? legPin, string? sessionPin)
    {
        if (string.IsNullOrEmpty(sessionPin))
        {
            return false;
        }

        return legPin == null
            || string.Equals(legPin, sessionPin, StringComparison.Ordinal);
    }
}
