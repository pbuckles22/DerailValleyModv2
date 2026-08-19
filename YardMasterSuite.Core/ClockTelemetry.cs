namespace YardMasterSuite.Core;

/// <summary>World-clock minute bucket. Type A extras publish on change only.</summary>
public struct ClockCache
{
    public int Hour;
    public int Minute;
    public bool Seeded;
    public bool Known;
}

public enum ClockLogKind
{
    Init = 0,
    Change = 1,
    Hide = 2,
}

/// <summary>
/// Unity-free clock gate. HUD updates when the in-game hour:minute bucket
/// changes; T2 is init / change / hide — not per LateUpdate tick.
/// </summary>
public static class ClockTelemetry
{
    public static bool Observe(bool known, int hour, int minute, ref ClockCache cache)
    {
        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.Known = known;
            if (!known)
            {
                return false;
            }

            cache.Hour = hour;
            cache.Minute = minute;
            return true;
        }

        if (cache.Known == known && (!known || (cache.Hour == hour && cache.Minute == minute)))
        {
            return false;
        }

        cache.Known = known;
        if (known)
        {
            cache.Hour = hour;
            cache.Minute = minute;
        }

        return true;
    }

    public static string? NextLog(int hour, int minute, ClockLogKind kind)
    {
        if (kind == ClockLogKind.Hide)
        {
            return "T2 clock hide";
        }

        var token = hour.ToString("00") + ":" + minute.ToString("00");
        if (kind == ClockLogKind.Init)
        {
            return "T2 clock init: " + token;
        }

        return "T2 clock change: " + token;
    }
}
