using System;

namespace YardMasterSuite.Core;

public struct PidSpeedState
{
    public float Integral;
    public float CommandedThrottle;
    public bool WaitCrawl;
}

public readonly struct PidSpeedInput
{
    public PidSpeedInput(
        float dt,
        float speedKmh,
        float requestKmh,
        float? postedKmh,
        float throttle,
        float independent,
        bool armed,
        bool derailIntervening,
        float thermalCeiling,
        float reverser,
        bool legNeedsReverse,
        float trainBrake = 0f)
    {
        Dt = dt;
        SpeedKmh = speedKmh;
        RequestKmh = requestKmh;
        PostedKmh = postedKmh;
        Throttle = throttle;
        Independent = independent;
        Armed = armed;
        DerailIntervening = derailIntervening;
        ThermalCeiling = thermalCeiling;
        Reverser = reverser;
        LegNeedsReverse = legNeedsReverse;
        TrainBrake = trainBrake;
    }

    public float Dt { get; }
    public float SpeedKmh { get; }
    public float RequestKmh { get; }
    public float? PostedKmh { get; }
    public float Throttle { get; }
    public float Independent { get; }
    public bool Armed { get; }
    public bool DerailIntervening { get; }
    public float ThermalCeiling { get; }
    public float Reverser { get; }
    public bool LegNeedsReverse { get; }
    public float TrainBrake { get; }
}

public readonly struct PidSpeedCommand
{
    public PidSpeedCommand(
        bool active,
        float targetKmh,
        float desiredThrottle,
        float desiredIndependent,
        float desiredReverser,
        bool gearPending,
        float desiredTrain = 0f,
        bool brakePending = false)
    {
        Active = active;
        TargetKmh = targetKmh;
        DesiredThrottle = desiredThrottle;
        DesiredIndependent = desiredIndependent;
        DesiredReverser = desiredReverser;
        GearPending = gearPending;
        DesiredTrain = desiredTrain;
        BrakePending = brakePending;
    }

    public bool Active { get; }
    public float TargetKmh { get; }
    public float DesiredThrottle { get; }
    public float DesiredIndependent { get; }
    public float DesiredReverser { get; }
    public bool GearPending { get; }
    public float DesiredTrain { get; }
    public bool BrakePending { get; }
}

/// <summary>
/// <b>9.1</b> PI throttle hold + independent raise on overspeed. Never dumps
/// air on overspeed / derail (reuses <see cref="LimitThrottleCap.ComputeDesiredBrake"/>).
/// Departure at crawl releases indy + train toward 0 before throttle.
/// Yields to <b>7.5</b>. Caps throttle at the <b>7.2</b> thermal ceiling. Sets
/// reverser to the current Switch List step before notching throttle.
/// Commanded throttle lives in <see cref="PidSpeedState"/> so a snapped
/// cab lever at 0 still ramps (DE2 sub-notch).
/// </summary>
public static class PidSpeedHold
{
    public const float Kp = 0.05f;
    public const float Ki = 0.008f;
    public const float IntegralLimit = 12f;
    /// <summary>
    /// Coast band around target: no throttle raise, no indy slam. Stops
    /// thr↔indy chatter that blows TMs after CLEARED. Indy only above
    /// <c>target + OverspeedBandKmh</c>; at/above target coasts thr=0.
    /// </summary>
    public const float OverspeedBandKmh = 2f;
    public const float OverspeedIndependent = 0.27f;
    /// <summary>
    /// Takeoff slew. 0.12 hit ~81% by 10 km/h (cab slip / motors=Dead).
    /// </summary>
    public const float ThrottleRaisePerSecond = 0.05f;
    public const float ThrottleIdlePerSecond = LimitThrottleCap.DefaultApplyPerSecond;
    public const float BrakeReleasePerSecond = LimitThrottleCap.DefaultApplyPerSecond;
    public const float BrakeReleaseEpsilon = 0.02f;
    public const float DepartureCrawlKmh = 2f;
    /// <summary>DE2 first notch (HUD 9%). Off-grid Sets stay on this notch.</summary>
    public const float MinNotch = PidSpeedNotch.Step;

    public static float ApproachThrottle(float current, float target, float dt)
    {
        var cur = Clamp01(current);
        var tgt = Clamp01(target);
        var perSecond = tgt >= cur ? ThrottleRaisePerSecond : ThrottleIdlePerSecond;
        var step = Math.Max(0f, perSecond) * Math.Max(0f, dt);
        if (step <= 0f)
        {
            return cur;
        }

        if (tgt > cur)
        {
            return Math.Min(tgt, cur + step);
        }

        if (tgt < cur)
        {
            return Math.Max(tgt, cur - step);
        }

        return cur;
    }

    public static float ApproachBrake(float current, float target, float dt)
    {
        var cur = Clamp01(current);
        var tgt = Clamp01(target);
        var step = Math.Max(0f, BrakeReleasePerSecond) * Math.Max(0f, dt);
        if (step <= 0f)
        {
            return cur;
        }

        if (tgt > cur)
        {
            return Math.Min(tgt, cur + step);
        }

        if (tgt < cur)
        {
            return Math.Max(tgt, cur - step);
        }

        return cur;
    }

    public static PidSpeedCommand Tick(in PidSpeedInput input, ref PidSpeedState state)
    {
        var target = PidSpeedTarget.Resolve(input.RequestKmh, input.PostedKmh);
        var throttle = Clamp01(input.Throttle);
        var independent = Clamp01(input.Independent);
        var train = Clamp01(input.TrainBrake);
        var reverser = input.Reverser;
        var fromThrottle = Math.Max(throttle, Clamp01(state.CommandedThrottle));

        if (!input.Armed)
        {
            state = default;
            return new PidSpeedCommand(
                active: false,
                target,
                throttle,
                independent,
                reverser,
                gearPending: false,
                train);
        }

        if (input.DerailIntervening)
        {
            state = default;
            return new PidSpeedCommand(
                active: false,
                target,
                throttle,
                independent,
                reverser,
                gearPending: false,
                train);
        }

        var dt = Math.Max(0f, input.Dt);
        var speed = input.SpeedKmh < 0f || float.IsNaN(input.SpeedKmh) ? 0f : input.SpeedKmh;
        var band = Math.Max(0f, OverspeedBandKmh);
        var overspeed = speed > target + band;
        var coast = !overspeed && speed >= target;

        var dead = Clamp01(input.ThermalCeiling) <= 1e-4f;
        var gearMismatch = !PidSpeedGear.Matches(reverser, input.LegNeedsReverse);

        // Latch WaitCrawl if motors die or gear is flipped while rolling fast.
        if (speed > DepartureCrawlKmh && (dead || gearMismatch))
        {
            state.WaitCrawl = true;
        }

        // Unlatch only once we have slowed to a crawl and motors are restored.
        if (state.WaitCrawl && speed <= DepartureCrawlKmh && !dead)
        {
            state.WaitCrawl = false;
        }

        if (dead)
        {
            state.Integral = 0f;
            state.CommandedThrottle = 0f;
            return new PidSpeedCommand(
                active: true,
                target,
                0f,
                independent,
                reverser,
                gearPending: gearMismatch,
                train);
        }

        if (state.WaitCrawl)
        {
            state.Integral = 0f;
            state.CommandedThrottle = 0f;
            return new PidSpeedCommand(
                true,
                target,
                0f,
                overspeed ? OverspeedIndependentTarget(independent) : ApproachBrake(independent, 0f, dt),
                reverser,
                gearPending: gearMismatch,
                ApproachBrake(train, 0f, dt),
                brakePending: false);
        }

        if (gearMismatch)
        {
            state.Integral = 0f;
            if (overspeed)
            {
                state.CommandedThrottle = 0f;
                return new PidSpeedCommand(
                    true,
                    target,
                    0f,
                    OverspeedIndependentTarget(independent),
                    PidSpeedGear.TargetReverser(input.LegNeedsReverse),
                    gearPending: true,
                    train);
            }

            var idle = ApproachThrottle(fromThrottle, 0f, dt);
            idle = Math.Min(idle, Clamp01(input.ThermalCeiling));
            state.CommandedThrottle = idle;
            var heldBrake = LimitThrottleCap.ComputeDesiredBrake(
                independent,
                target: 0f,
                intervening: false,
                dt);
            return new PidSpeedCommand(
                true,
                target,
                NotchWrite(idle, 0f),
                heldBrake,
                PidSpeedGear.TargetReverser(input.LegNeedsReverse),
                gearPending: true,
                train);
        }

        var airOn = independent > BrakeReleaseEpsilon || train > BrakeReleaseEpsilon;
        if (speed <= DepartureCrawlKmh && airOn)
        {
            state.Integral = 0f;
            var idle = ApproachThrottle(fromThrottle, 0f, dt);
            idle = Math.Min(idle, Clamp01(input.ThermalCeiling));
            state.CommandedThrottle = idle;
            return new PidSpeedCommand(
                true,
                target,
                NotchWrite(idle, 0f),
                ApproachBrake(independent, 0f, dt),
                reverser,
                gearPending: false,
                ApproachBrake(train, 0f, dt),
                brakePending: true);
        }

        if (overspeed)
        {
            state.Integral = 0f;
            state.CommandedThrottle = 0f;
            return new PidSpeedCommand(
                true,
                target,
                0f,
                OverspeedIndependentTarget(independent),
                reverser,
                gearPending: false,
                train);
        }

        if (coast)
        {
            // At/above target but inside overspeed band: thr off, soft indy
            // release — no slam that chatters thr↔indy into motors=Dead.
            state.Integral = 0f;
            state.CommandedThrottle = 0f;
            return new PidSpeedCommand(
                true,
                target,
                0f,
                ApproachBrake(independent, 0f, dt),
                reverser,
                gearPending: false,
                train);
        }

        var error = target - speed;
        state.Integral = Clamp(state.Integral + (error * dt), -IntegralLimit, IntegralLimit);
        var feedforward = CruiseThrottle(target);
        var raw = feedforward + (Kp * error) + (Ki * state.Integral);
        var pi = Math.Min(Clamp01(raw), Clamp01(input.ThermalCeiling));
        var desired = ApproachThrottle(fromThrottle, pi, dt);
        desired = FirstNotchIfRaising(desired, pi);
        state.CommandedThrottle = desired;
        return new PidSpeedCommand(
            true,
            target,
            NotchWrite(desired, pi),
            ApproachBrake(independent, 0f, dt),
            reverser,
            gearPending: false,
            train);
    }

    public static float NotchWrite(float analog, float target)
    {
        analog = Clamp01(analog);
        if (target + 1e-4f < analog)
        {
            return PidSpeedNotch.Floor(analog);
        }

        return PidSpeedNotch.Snap(analog);
    }

    public static float OverspeedIndependentTarget(float current)
    {
        var snapped = PidSpeedNotch.Snap(Clamp01(current));
        return snapped > OverspeedIndependent ? snapped : OverspeedIndependent;
    }

    public static float FirstNotchIfRaising(float desired, float pi)
    {
        if (pi + 1e-4f < MinNotch)
        {
            return desired;
        }

        if (desired > 0f && desired < MinNotch)
        {
            return MinNotch;
        }

        return desired;
    }

    public static float CruiseThrottle(float targetKmh)
    {
        if (targetKmh <= 0f || float.IsNaN(targetKmh))
        {
            return 0f;
        }

        return Clamp01(
            (PidSpeedPlant.DragPerKmh * targetKmh) / PidSpeedPlant.MaxAccelKmhPerS);
    }

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }

    private static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}