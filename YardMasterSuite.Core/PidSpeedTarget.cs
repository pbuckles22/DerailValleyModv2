using System;

namespace YardMasterSuite.Core;

/// <summary>
/// <b>9.1</b> cruise target: <c>min(request, Posted Limit)</c>. Missing/non-positive
/// posted → request only. Default request is 25 km/h.
/// </summary>
public static class PidSpeedTarget
{
    public const float DefaultRequestKmh = 25f;

    public static float Resolve(float requestKmh, float? postedKmh)
    {
        var request = PositiveOrDefault(requestKmh, DefaultRequestKmh);
        if (postedKmh is float posted && posted > 0f && !float.IsNaN(posted))
        {
            return Math.Min(request, posted);
        }

        return request;
    }

    private static float PositiveOrDefault(float value, float fallback)
    {
        if (float.IsNaN(value) || value <= 0f)
        {
            return fallback;
        }

        return value;
    }
}
