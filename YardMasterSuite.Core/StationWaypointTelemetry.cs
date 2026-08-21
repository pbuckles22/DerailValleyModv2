using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Discrete Player.log lines for Tier 2 in-zone station waypoint (**6.12**).
/// Logs on zone / yard / bearing — not every meter.
/// </summary>
public readonly struct StationWaypointDebugSnapshot
{
    public StationWaypointDebugSnapshot(bool inZone, string? yardId, string? walkPoint)
    {
        InZone = inZone;
        YardId = yardId;
        WalkPoint = walkPoint;
    }

    public bool InZone { get; }
    public string? YardId { get; }

    /// <summary>16-point walk bearing, <c>here</c>, or null when out of zone / unknown.</summary>
    public string? WalkPoint { get; }

    public string FormatFragment()
    {
        if (!InZone)
        {
            return "— Station";
        }

        var label = string.IsNullOrWhiteSpace(YardId) ? "—" : YardId!.Trim();
        return WalkPoint is null ? "Station " + label : "Station " + label + " " + WalkPoint;
    }
}

public struct StationWaypointCache
{
    public bool Seeded;
    public bool InZone;
    public string? YardId;
    public int PointIndex;
    public int Meters;
}

/// <summary>
/// Unity-free Station gate. HUD updates when bearing or integer meters change;
/// T2 is init / yard / bearing / leave — not every LateUpdate tick.
/// </summary>
public static class StationWaypointTelemetry
{
    private const int Outside = -1;
    private const int Dash = -2;
    private const int Here = 16;

    public static bool Observe(
        bool inZone,
        string? yardId,
        float? stationX,
        float? stationZ,
        float? playerX,
        float? playerZ,
        bool atOffice,
        ref StationWaypointCache cache)
    {
        var pointIndex = Outside;
        var meters = 0;
        string? id = null;
        if (inZone)
        {
            id = yardId;
            if (stationX is null || stationZ is null || playerX is null || playerZ is null)
            {
                pointIndex = Dash;
            }
            else if (atOffice)
            {
                pointIndex = Here;
            }
            else
            {
                var dx = stationX.Value - playerX.Value;
                var dz = stationZ.Value - playerZ.Value;
                var distance = Math.Sqrt((dx * dx) + (dz * dz));
                pointIndex = HeadingDisplay.ToPointIndex(HeadingDisplay.FromForward(dx, dz));
                meters = (int)Math.Round(distance, MidpointRounding.AwayFromZero);
            }
        }

        if (!cache.Seeded)
        {
            cache.Seeded = true;
            cache.InZone = inZone;
            cache.YardId = id;
            cache.PointIndex = pointIndex;
            cache.Meters = meters;
            return inZone;
        }

        if (cache.InZone == inZone
            && string.Equals(cache.YardId, id, StringComparison.Ordinal)
            && cache.PointIndex == pointIndex
            && cache.Meters == meters)
        {
            return false;
        }

        cache.InZone = inZone;
        cache.YardId = id;
        cache.PointIndex = pointIndex;
        cache.Meters = meters;
        return true;
    }

    public static StationWaypointDebugSnapshot Snapshot(ref StationWaypointCache cache)
    {
        if (!cache.InZone)
        {
            return new StationWaypointDebugSnapshot(false, null, null);
        }

        if (cache.PointIndex == Here)
        {
            return new StationWaypointDebugSnapshot(true, cache.YardId, "here");
        }

        if (cache.PointIndex >= 0 && cache.PointIndex < HeadingDisplay.PointCount)
        {
            return new StationWaypointDebugSnapshot(
                true,
                cache.YardId,
                HeadingDisplay.PointName(cache.PointIndex));
        }

        return new StationWaypointDebugSnapshot(true, cache.YardId, null);
    }

    public static string? NextLog(
        StationWaypointDebugSnapshot? previous,
        StationWaypointDebugSnapshot current)
    {
        if (previous is null)
        {
            return "T2 station init: " + current.FormatFragment();
        }

        var prior = previous.Value;
        if (prior.InZone == current.InZone
            && string.Equals(prior.YardId, current.YardId, StringComparison.Ordinal)
            && string.Equals(prior.WalkPoint, current.WalkPoint, StringComparison.Ordinal))
        {
            return null;
        }

        return "T2 station change: " + current.FormatFragment();
    }
}
