namespace YardMasterSuite.Core;

/// <summary>RouteLeg pin target — golden <c>2.8.7.2</c>.</summary>
public static class SwitchListRouteLeg
{
    public static bool ShouldArmPin(PathPlanResult? plan)
    {
        if (plan == null)
        {
            return false;
        }

        if (plan.JunctionFirstStop is PathJunctionFirstStop stop)
        {
            var stopId = stop.JunctionId?.Trim();
            if (!string.IsNullOrEmpty(stopId))
            {
                return true;
            }
        }

        return PathPlan.RequiredFlips(plan).Count > 0;
    }

    public static string? PickPinJunctionId(PathPlanResult? plan)
    {
        if (plan == null || !ShouldArmPin(plan))
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

        var flipId = flips[0].JunctionId?.Trim();
        return string.IsNullOrEmpty(flipId) ? null : flipId;
    }
}
