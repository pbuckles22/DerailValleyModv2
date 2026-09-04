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

/// <summary>Pure stop-command tick while <see cref="PidGoStopSession"/> is armed.</summary>
public static class PidGoStop
{
    public const float StopIndependent = 1f;
    public const float StopTrain = 0.90f;

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
        var idle = PidSpeedHold.ApproachThrottle(throttle, 0f, dt);
        var indy = PidSpeedHold.ApproachBrake(independent, StopIndependent, dt);
        var trn = PidSpeedHold.ApproachBrake(train, StopTrain, dt);
        return new PidSpeedCommand(
            active: true,
            targetKmh: 0f,
            desiredThrottle: idle,
            desiredIndependent: indy,
            desiredReverser: reverser,
            gearPending: false,
            desiredTrain: trn,
            brakePending: true);
    }
}
