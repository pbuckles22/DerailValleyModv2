namespace YardMasterSuite.Core;

/// <summary>
/// After desk <c>Stop GO</c>, keep writing thr idle + raise air until crawl
/// so GO does not leave a runaway consist (**13.4** smoke).
/// </summary>
public static class PidGoStopSession
{
    public static bool Active { get; private set; }

    public static void Arm() => Active = true;

    public static void Clear() => Active = false;
}

/// <summary>
/// Pure stop-command tick while <see cref="PidGoStopSession"/> is armed.
/// <b>13.4.13:</b> snap thr + indy (DE2 notch expander traps slow indy slew at ~18%);
/// train brake applies at <see cref="TrainApplyPerSecond"/>.
/// </summary>
public static class PidGoStop
{
    public const float StopIndependent = 1f;
    public const float StopTrain = 0.90f;

    /// <summary>Gemini tactical: train brake 0.50/s on Stop GO (continuous lever).</summary>
    public const float TrainApplyPerSecond = 0.50f;

    public static bool ShouldApply(bool sessionActive, bool goArmed) =>
        sessionActive && !goArmed;

    public static bool IsStopped(float speedKmh) =>
        speedKmh <= PidSpeedHold.DepartureCrawlKmh;

    public static PidSpeedCommand Tick(
        float dt,
        float throttle,
        float independent,
        float train,
        float reverser)
    {
        _ = throttle;
        _ = independent;
        var trn = ApproachApply(train, StopTrain, dt, TrainApplyPerSecond);
        return new PidSpeedCommand(
            active: true,
            targetKmh: 0f,
            desiredThrottle: 0f,
            desiredIndependent: StopIndependent,
            desiredReverser: reverser,
            gearPending: false,
            desiredTrain: trn,
            brakePending: true);
    }

    /// <summary>
    /// Go-stop lever intent — do not run through <see cref="PidSpeedCab"/> /
    /// notch expander (FixedUpdate 0.02 steps stick on 0.18).
    /// </summary>
    public static void ApplyLevers(in PidSpeedCommand stopCmd, ref float throttle, ref float independent)
    {
        if (!stopCmd.Active)
        {
            return;
        }

        throttle = 0f;
        independent = stopCmd.DesiredIndependent;
    }

    static float ApproachApply(float current, float target, float dt, float applyPerSecond)
    {
        var cur = current < 0f ? 0f : (current > 1f ? 1f : current);
        var tgt = target < 0f ? 0f : (target > 1f ? 1f : target);
        var step = System.Math.Max(0f, applyPerSecond) * System.Math.Max(0f, dt);
        if (step <= 0f)
        {
            return cur;
        }

        if (tgt > cur)
        {
            return System.Math.Min(tgt, cur + step);
        }

        if (tgt < cur)
        {
            return System.Math.Max(tgt, cur - step);
        }

        return cur;
    }
}
