using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure consist safety for <b>7.5</b>: when Derail Risk is ≥65 %, idle throttle
/// and raise air. Speed and posted/Next Limit are HUD-only. Never dumps brakes.
/// Chip yellow stays 15 %.
/// </summary>
public static class LimitThrottleCap
{
    public const float DefaultApplyPerSecond = 0.20f;
    public const float CritApplyPerSecond = 0.35f;
    public const float DerailIntervenePercent = 65f;
    public const float DerailWarnIndependent = 0.60f;
    public const float DerailWarnTrain = 0.45f;
    public const float DerailCritIndependent = 1f;
    public const float DerailCritTrain = 0.90f;

    private const float Epsilon = 1e-4f;

    public static bool ShouldIntervene(float? derailPercent) =>
        derailPercent is float risk
        && !float.IsNaN(risk)
        && risk >= DerailIntervenePercent;

    public static float ApplyPerSecond(float? derailPercent) =>
        IsDerailCrit(derailPercent) ? CritApplyPerSecond : DefaultApplyPerSecond;

    public static float IndependentTarget(float? derailPercent)
    {
        if (IsDerailCrit(derailPercent))
        {
            return DerailCritIndependent;
        }

        return ShouldIntervene(derailPercent) ? DerailWarnIndependent : 0f;
    }

    public static float TrainTarget(float? derailPercent)
    {
        if (IsDerailCrit(derailPercent))
        {
            return DerailCritTrain;
        }

        return ShouldIntervene(derailPercent) ? DerailWarnTrain : 0f;
    }

    /// <summary>Soft-roll throttle toward idle while intervening. Never raises. Zero delta → snap to 0.</summary>
    public static float ComputeDesiredThrottle(
        float currentThrottle,
        bool intervening,
        float deltaTime,
        float applyPerSecond = DefaultApplyPerSecond)
    {
        var current = Clamp01(currentThrottle);
        if (!intervening)
        {
            return current;
        }

        var step = Math.Max(0f, applyPerSecond) * Math.Max(0f, deltaTime);
        if (step <= 0f)
        {
            return 0f;
        }

        return Math.Max(0f, current - step);
    }

    public static float ComputeDesiredThrottle(float currentThrottle, bool intervening) =>
        ComputeDesiredThrottle(currentThrottle, intervening, deltaTime: 0f, applyPerSecond: 0f);

    /// <summary>Soft-roll brake up to <paramref name="target"/>. Never dumps air.</summary>
    public static float ComputeDesiredBrake(
        float currentBrake,
        float target,
        bool intervening,
        float deltaTime,
        float applyPerSecond = DefaultApplyPerSecond)
    {
        var current = Clamp01(currentBrake);
        if (!intervening)
        {
            return current;
        }

        var max = Clamp01(target);
        if (current >= max)
        {
            return current;
        }

        var step = Math.Max(0f, applyPerSecond) * Math.Max(0f, deltaTime);
        if (step <= 0f)
        {
            return max;
        }

        return Math.Min(max, current + step);
    }

    public static bool ShouldLower(float current, float desired) =>
        desired + Epsilon < Clamp01(current);

    public static bool ShouldRaise(float current, float desired) =>
        desired > Clamp01(current) + Epsilon;

    /// <summary>Which levers 7.5 is still moving (HUD flash). None when not intervening.</summary>
    public static LimitGovCue CueForLevers(
        bool intervening,
        float throttle,
        float independent,
        float train,
        float indyTarget,
        float trainTarget)
    {
        if (!intervening)
        {
            return LimitGovCue.None;
        }

        return new LimitGovCue(
            Clamp01(throttle) > Epsilon,
            Clamp01(independent) + Epsilon < Clamp01(indyTarget),
            Clamp01(train) + Epsilon < Clamp01(trainTarget));
    }

    public static bool NeedsWork(
        float throttle,
        float independent,
        float train,
        float indyTarget,
        float trainTarget) =>
        CueForLevers(
            intervening: true,
            throttle,
            independent,
            train,
            indyTarget,
            trainTarget).Any;

    public static bool ShouldHold(bool intervening, bool needsWork) =>
        intervening && !needsWork;

    public static bool IsSafeToWrite(
        bool hasUsableLoco,
        bool controlsPresent,
        bool controlNotBlocked,
        bool intervening,
        bool needsWork) =>
        hasUsableLoco
        && controlsPresent
        && controlNotBlocked
        && intervening
        && needsWork;

    private static bool IsDerailCrit(float? derailPercent) =>
        derailPercent is float risk
        && !float.IsNaN(risk)
        && risk >= DerailRiskDisplay.CriticalThresholdPercent;

    private static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value < 0f)
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
