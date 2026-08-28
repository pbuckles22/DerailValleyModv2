using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Type B Maps route plan ready for main-thread session update (**8.2**).</summary>
public readonly struct RoutePlanReady
{
    public readonly int Generation;
    public readonly PathPlanResult? Plan;
    public readonly string? OriginTrackId;
    public readonly string? ExitCue;
    public readonly float? TravelEtaSeconds;
    public readonly Dictionary<string, int>? JunctionSnapshot;
    public readonly string? LogLine;
    public readonly string? ComputeReason;

    public RoutePlanReady(
        int generation,
        PathPlanResult? plan,
        string? originTrackId,
        string? exitCue,
        float? travelEtaSeconds,
        Dictionary<string, int>? junctionSnapshot,
        string? logLine,
        string? computeReason = null)
    {
        Generation = generation;
        Plan = plan;
        OriginTrackId = originTrackId;
        ExitCue = exitCue;
        TravelEtaSeconds = travelEtaSeconds;
        JunctionSnapshot = junctionSnapshot;
        LogLine = logLine;
        ComputeReason = computeReason;
    }
}
