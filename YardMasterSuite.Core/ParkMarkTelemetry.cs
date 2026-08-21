using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 park/return mark checks.
/// Logs on set/clear and 16-point return bearing changes — not every meter.
/// </summary>
public readonly struct ParkDebugSnapshot
{
    public ParkDebugSnapshot(bool hasMark, string? returnPoint)
    {
        HasMark = hasMark;
        ReturnPoint = returnPoint;
    }

    public bool HasMark { get; }

    /// <summary>16-point return bearing, <c>here</c>, or null when unmarked / unknown.</summary>
    public string? ReturnPoint { get; }

    public string FormatFragment()
    {
        if (!HasMark)
        {
            return "— Marked";
        }

        return ReturnPoint is null ? "— Marked" : "Marked " + ReturnPoint;
    }
}

public struct ParkMarkCache
{
    public bool Seeded;
    public bool HasMark;
    public int PointIndex;
    public int Meters;
}

/// <summary>
/// Unity-free Marked gate. HUD updates when bearing or integer meters change;
/// T2 is init / bearing / clear — not every LateUpdate tick.
/// </summary>
public static class ParkMarkTelemetry
{
    private const int Unmarked = -1;
    private const int Dash = -2;
    private const int Here = 16;

    public static bool Observe(
        bool hasMark,
        float? markX,
        float? markZ,
        float? playerX,
        float? playerZ,
        ref ParkMarkCache cache)
    {
        var pointIndex = Unmarked;
        var meters = 0;
        if (hasMark && markX is not null && markZ is not null)
        {
            if (playerX is null || playerZ is null)
            {
                pointIndex = Dash;
            }
            else
            {
                var dx = markX.Value - playerX.Value;
                var dz = markZ.Value - playerZ.Value;
                var distance = Math.Sqrt((dx * dx) + (dz * dz));
                if (distance < ParkMarkDisplay.HereThresholdMeters)
                {
                    pointIndex = Here;
                }
                else
                {
                    pointIndex = HeadingDisplay.ToPointIndex(HeadingDisplay.FromForward(dx, dz));
                    meters = (int)Math.Round(distance, MidpointRounding.AwayFromZero);
                }
            }
        }

        var marked = pointIndex != Unmarked;
        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.HasMark = marked;
            cache.PointIndex = pointIndex;
            cache.Meters = meters;
            return marked;
        }

        if (cache.HasMark == marked
            && cache.PointIndex == pointIndex
            && cache.Meters == meters)
        {
            return false;
        }

        cache.HasMark = marked;
        cache.PointIndex = pointIndex;
        cache.Meters = meters;
        return true;
    }

    public static ParkDebugSnapshot Snapshot(ref ParkMarkCache cache)
    {
        if (!cache.HasMark)
        {
            return new ParkDebugSnapshot(false, null);
        }

        if (cache.PointIndex == Here)
        {
            return new ParkDebugSnapshot(true, "here");
        }

        if (cache.PointIndex >= 0 && cache.PointIndex < HeadingDisplay.PointCount)
        {
            return new ParkDebugSnapshot(true, HeadingDisplay.PointName(cache.PointIndex));
        }

        return new ParkDebugSnapshot(true, null);
    }

    public static string? NextLog(ParkDebugSnapshot? previous, ParkDebugSnapshot current)
    {
        if (previous is null)
        {
            return "T2 mark init: " + current.FormatFragment();
        }

        var prior = previous.Value;
        if (prior.HasMark == current.HasMark
            && string.Equals(prior.ReturnPoint, current.ReturnPoint, StringComparison.Ordinal))
        {
            return null;
        }

        return "T2 mark change: " + current.FormatFragment();
    }
}
