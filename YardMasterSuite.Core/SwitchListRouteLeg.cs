namespace YardMasterSuite.Core;

/// <summary>RouteLeg pin target for active Switch List leg (switch gate, not dest track).</summary>
public static class SwitchListRouteLeg
{
    /// <summary>
    /// Pin target: junction-first stop (Yard dual-branch approach) when present;
    /// else first misaligned junction (RequiredFlips order).
    /// </summary>
    public static string? PickPinJunctionId(PathPlanResult? plan)
    {
        if (plan == null)
        {
            return null;
        }

        if (plan.JunctionFirstStop is PathJunctionFirstStop stop)
        {
            var stopId = stop.JunctionId?.Trim();
            if (!string.IsNullOrEmpty(stopId))
            {
                return stopId;
            }
        }

        var flips = PathPlan.RequiredFlips(plan);
        if (flips.Count == 0)
        {
            return null;
        }

        var id = flips[0].JunctionId?.Trim();
        return string.IsNullOrEmpty(id) ? null : id;
    }
}
