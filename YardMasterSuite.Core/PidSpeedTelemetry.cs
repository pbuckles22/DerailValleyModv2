using System;

namespace YardMasterSuite.Core;

public enum PidSpeedMode
{
    Idle = 0,
    Hold = 1,
    YieldDerail = 2,
    Gear = 3,
    ReleaseAir = 4,
    MotorsDead = 5,
    WaitCrawl = 6,
}

public struct PidSpeedLogCache
{
    public PidSpeedMode Last;
    public bool Seeded;
}

public struct PidSpeedThrCache
{
    public bool On;
    public bool Seeded;
}

public struct PidSpeedApplyCache
{
    public int ThrPct;
    public int IndyPct;
    public bool SkipOverlay;
    public bool SkipGate;
    public bool SeededApply;
    public bool SeededSkip;
}

/// <summary>
/// Discrete Player.log lines for <b>9.1</b>. Change-only; interned. No per-tick spam.
/// </summary>
public static class PidSpeedTelemetry
{
    public const string Hold = "T2 pid: hold";
    public const string Idle = "T2 pid: idle";
    public const string YieldDerail = "T2 pid: yield-derail";
    public const string Gear = "T2 pid: gear";
    public const string ReleaseAir = "T2 pid: brakes";
    public const string ThrOn = "T2 pid: thr-on";
    public const string ThrOff = "T2 pid: thr-off";
    public const string SkipOverlay = "T2 pid: skip overlay";
    public const string SkipGate = "T2 pid: skip gate";
    public const string CruiseOn = "T2 pid: cruise-on";
    public const string CruiseOff = "T2 pid: cruise-off";
    public const string MotorsDead = "T2 pid: motors-dead";
    public const string WaitCrawl = "T2 pid: wait-crawl";

    public static string FormatCruise(bool enabled) => enabled ? CruiseOn : CruiseOff;

    public static PidSpeedMode Mode(bool armed, bool derailIntervening) =>
        Mode(armed, derailIntervening, gearPending: false);

    public static PidSpeedMode Mode(bool armed, bool derailIntervening, bool gearPending) =>
        Mode(armed, derailIntervening, gearPending, brakePending: false);

    public static PidSpeedMode Mode(
        bool armed,
        bool derailIntervening,
        bool gearPending,
        bool brakePending) =>
        Mode(armed, derailIntervening, gearPending, brakePending, motorsDead: false);

    public static PidSpeedMode Mode(
        bool armed,
        bool derailIntervening,
        bool gearPending,
        bool brakePending,
        bool motorsDead) =>
        Mode(armed, derailIntervening, gearPending, brakePending, motorsDead, waitCrawl: false);

    public static PidSpeedMode Mode(
        bool armed,
        bool derailIntervening,
        bool gearPending,
        bool brakePending,
        bool motorsDead,
        bool waitCrawl)
    {
        if (!armed)
        {
            return PidSpeedMode.Idle;
        }

        if (derailIntervening)
        {
            return PidSpeedMode.YieldDerail;
        }

        if (motorsDead)
        {
            return PidSpeedMode.MotorsDead;
        }

        if (waitCrawl)
        {
            return PidSpeedMode.WaitCrawl;
        }

        if (gearPending)
        {
            return PidSpeedMode.Gear;
        }

        return brakePending ? PidSpeedMode.ReleaseAir : PidSpeedMode.Hold;
    }

    public static string? NextLog(PidSpeedMode mode, ref PidSpeedLogCache cache)
    {
        if (cache.Seeded && cache.Last == mode)
        {
            return null;
        }

        cache.Seeded = true;
        cache.Last = mode;
        return mode switch
        {
            PidSpeedMode.Hold => Hold,
            PidSpeedMode.YieldDerail => YieldDerail,
            PidSpeedMode.Gear => Gear,
            PidSpeedMode.ReleaseAir => ReleaseAir,
            PidSpeedMode.MotorsDead => MotorsDead,
            PidSpeedMode.WaitCrawl => WaitCrawl,
            _ => Idle,
        };
    }

    public static bool WantsThrottle(bool armed, bool gearPending, bool brakePending, float desiredThrottle) =>
        armed
        && !gearPending
        && !brakePending
        && desiredThrottle + 1e-4f >= PidSpeedHold.MinNotch;

    public static string? NextThr(bool want, ref PidSpeedThrCache cache)
    {
        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.On = want;
            return want ? ThrOn : null;
        }

        if (cache.On == want)
        {
            return null;
        }

        cache.On = want;
        return want ? ThrOn : ThrOff;
    }

    /// <summary>
    /// 2.9.1.10: <c>thr-off</c> with no <c>T2 controls</c> — write intent was
    /// dropped before SoftWrite. Change-only skip reason.
    /// </summary>
    public static string? NextSkip(bool overlay, ref PidSpeedApplyCache cache)
    {
        if (overlay)
        {
            if (cache.SeededSkip && cache.SkipOverlay && !cache.SkipGate)
            {
                return null;
            }

            cache.SeededSkip = true;
            cache.SkipOverlay = true;
            cache.SkipGate = false;
            return SkipOverlay;
        }

        if (cache.SeededSkip && cache.SkipGate && !cache.SkipOverlay)
        {
            return null;
        }

        cache.SeededSkip = true;
        cache.SkipOverlay = false;
        cache.SkipGate = true;
        return SkipGate;
    }

    public static string? NextApply(float throttle, float independent, ref PidSpeedApplyCache cache)
    {
        var thr = ToPct(throttle);
        var indy = ToPct(independent);
        if (cache.SeededApply && cache.ThrPct == thr && cache.IndyPct == indy)
        {
            return null;
        }

        cache.SeededApply = true;
        cache.ThrPct = thr;
        cache.IndyPct = indy;
        return "T2 pid: apply thr=" + thr + " indy=" + indy;
    }

    private static int ToPct(float normalized)
    {
        if (float.IsNaN(normalized) || normalized <= 0f)
        {
            return 0;
        }

        if (normalized >= 1f)
        {
            return 100;
        }

        return (int)Math.Round(normalized * 100.0, MidpointRounding.AwayFromZero);
    }
}

