namespace YardMasterSuite.Core;

public struct OnConsistCache
{
    public bool Seeded;
    public bool Armed;
}

/// <summary>
/// Unity-free on-consist arm gate. T2 is arm / disarm — not every notch.
/// </summary>
public static class OnConsistTelemetry
{
    public const string ArmedLine = "T2 on-consist: armed (cab bindings → front loco)";
    public const string DisarmedLine = "T2 on-consist: disarmed";

    public static bool Observe(bool armed, ref OnConsistCache cache)
    {
        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.Armed = armed;
            return armed;
        }

        if (cache.Armed == armed)
        {
            return false;
        }

        cache.Armed = armed;
        return true;
    }

    public static string? NextLog(bool wasSeeded, bool wasArmed, bool armed)
    {
        if (wasSeeded && wasArmed == armed)
        {
            return null;
        }

        if (!wasSeeded && !armed)
        {
            return null;
        }

        return armed ? ArmedLine : DisarmedLine;
    }
}
