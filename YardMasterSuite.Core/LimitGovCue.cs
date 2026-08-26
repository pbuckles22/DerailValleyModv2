namespace YardMasterSuite.Core;

/// <summary>Which cab levers <b>7.5</b> is moving (HUD flash). Type A.</summary>
public readonly struct LimitGovCue
{
    public readonly bool Throttle;
    public readonly bool Independent;
    public readonly bool TrainBrake;

    public LimitGovCue(bool throttle, bool independent, bool trainBrake)
    {
        Throttle = throttle;
        Independent = independent;
        TrainBrake = trainBrake;
    }

    public static LimitGovCue None => default;

    public bool Any => Throttle || Independent || TrainBrake;
}

public struct LimitGovCueCache
{
    public bool Throttle;
    public bool Independent;
    public bool TrainBrake;
    public bool Seeded;
}

/// <summary>~2.5 Hz red blink for governor-owned lever chips.</summary>
public static class GovernorFlash
{
    public const float PeriodSeconds = 0.4f;

    public static bool Lit(float unscaledTime)
    {
        if (unscaledTime < 0f || float.IsNaN(unscaledTime))
        {
            return false;
        }

        var cycle = unscaledTime % PeriodSeconds;
        if (cycle < 0f)
        {
            cycle += PeriodSeconds;
        }

        return cycle < PeriodSeconds * 0.5f;
    }
}

public static class LimitGovCueTelemetry
{
    public static bool Observe(in LimitGovCue cue, ref LimitGovCueCache cache)
    {
        if (cache.Seeded
            && cache.Throttle == cue.Throttle
            && cache.Independent == cue.Independent
            && cache.TrainBrake == cue.TrainBrake)
        {
            return false;
        }

        cache.Seeded = true;
        cache.Throttle = cue.Throttle;
        cache.Independent = cue.Independent;
        cache.TrainBrake = cue.TrainBrake;
        return true;
    }
}
