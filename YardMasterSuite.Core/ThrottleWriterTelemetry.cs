using System;

namespace YardMasterSuite.Core;

public enum ThrottleWriterKind
{
    None = 0,
    Pid = 1,
    Thermal = 2,
    DerailGov = 3,
}

/// <summary>Last <c>T2 writer:</c> harvest line (<b>13.1.15</b>).</summary>
public struct ThrottleWriterLogCache
{
    public bool Seeded;
    public ThrottleWriterKind Writer;
    public int ThrPct;
    public int SpdKmh;
    public int LimitKmh;
    public bool LimitKnown;
    public int RiskPct;
}

/// <summary>Per-physics-frame mailbox: which governor wrote throttle.</summary>
public static class ThrottleWriterNote
{
    private static int _frame = int.MinValue;
    private static ThrottleWriterKind _kind;

    public static void Reset()
    {
        _frame = int.MinValue;
        _kind = ThrottleWriterKind.None;
    }

    public static void Note(ThrottleWriterKind kind, int frame)
    {
        if (kind == ThrottleWriterKind.None)
        {
            return;
        }

        if (_frame != frame)
        {
            _frame = frame;
            _kind = ThrottleWriterKind.None;
        }

        _kind = ThrottleWriterTelemetry.Merge(_kind, kind);
    }

    public static ThrottleWriterKind Peek(int frame) =>
        _frame == frame ? _kind : ThrottleWriterKind.None;
}

/// <summary>
/// Change-only throttle-writer harvest. Governor write, or none when
/// throttle drops with Cruise/GO off.
/// </summary>
public static class ThrottleWriterTelemetry
{
    public static ThrottleWriterKind Merge(ThrottleWriterKind a, ThrottleWriterKind b) =>
        (ThrottleWriterKind)Math.Max((int)a, (int)b);

    public static string Format(
        ThrottleWriterKind writer,
        int thrPct,
        int spdKmh,
        int? limitKmh,
        int riskPct)
    {
        var name = writer switch
        {
            ThrottleWriterKind.Pid => "pid",
            ThrottleWriterKind.Thermal => "thermal",
            ThrottleWriterKind.DerailGov => "derail-gov",
            _ => "none",
        };
        var limit = limitKmh is int lim ? lim.ToString() : "—";
        return "T2 writer: " + name
            + " thr=" + ClampPct(thrPct)
            + " spd=" + spdKmh
            + " limit=" + limit
            + " risk=" + ClampPct(riskPct);
    }

    public static string? NextLog(
        ThrottleWriterKind wroteThisTick,
        int thrPct,
        int spdKmh,
        int? limitKmh,
        int riskPct,
        bool cruiseOrGoOn,
        ref ThrottleWriterLogCache cache)
    {
        thrPct = ClampPct(thrPct);
        riskPct = ClampPct(riskPct);
        var limitKnown = limitKmh is int;
        var limit = limitKnown ? limitKmh!.Value : 0;

        if (wroteThisTick != ThrottleWriterKind.None)
        {
            if (cache.Seeded
                && cache.Writer == wroteThisTick
                && cache.ThrPct == thrPct
                && cache.SpdKmh == spdKmh
                && cache.LimitKnown == limitKnown
                && cache.LimitKmh == limit
                && cache.RiskPct == riskPct)
            {
                return null;
            }

            Store(wroteThisTick, thrPct, spdKmh, limitKnown, limit, riskPct, ref cache);
            return Format(wroteThisTick, thrPct, spdKmh, limitKmh, riskPct);
        }

        if (!cache.Seeded)
        {
            Store(ThrottleWriterKind.None, thrPct, spdKmh, limitKnown, limit, riskPct, ref cache);
            return null;
        }

        var dropped = thrPct < cache.ThrPct;
        Store(ThrottleWriterKind.None, thrPct, spdKmh, limitKnown, limit, riskPct, ref cache);
        if (!dropped || cruiseOrGoOn)
        {
            return null;
        }

        return Format(ThrottleWriterKind.None, thrPct, spdKmh, limitKmh, riskPct);
    }

    private static void Store(
        ThrottleWriterKind writer,
        int thrPct,
        int spdKmh,
        bool limitKnown,
        int limit,
        int riskPct,
        ref ThrottleWriterLogCache cache)
    {
        cache.Seeded = true;
        cache.Writer = writer;
        cache.ThrPct = thrPct;
        cache.SpdKmh = spdKmh;
        cache.LimitKnown = limitKnown;
        cache.LimitKmh = limit;
        cache.RiskPct = riskPct;
    }

    private static int ClampPct(int pct)
    {
        if (pct < 0)
        {
            return 0;
        }

        return pct > 100 ? 100 : pct;
    }
}
