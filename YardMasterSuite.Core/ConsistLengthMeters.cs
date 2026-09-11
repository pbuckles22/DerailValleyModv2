namespace YardMasterSuite.Core;

/// <summary>Sum of measured car occupancy (max coupler/bounds). No DE2 hardcode.</summary>
public static class ConsistLengthMeters
{
    public static float Sum(float[]? carLengthsMeters) =>
        Sum(carLengthsMeters, carLengthsMeters == null ? 0 : carLengthsMeters.Length);

    public static float Sum(float[]? carLengthsMeters, int count)
    {
        if (carLengthsMeters == null || count <= 0)
        {
            return 0f;
        }

        var n = count < carLengthsMeters.Length ? count : carLengthsMeters.Length;
        var total = 0f;
        for (var i = 0; i < n; i++)
        {
            var L = carLengthsMeters[i];
            if (L > 0f && !float.IsNaN(L) && !float.IsInfinity(L))
            {
                total += L;
            }
        }

        return total;
    }

    /// <summary>
    /// Along-track occupancy for one car. Coupler span under-counts body
    /// overhang (cab 2.13.2.5.15 <c>len=44</c> sat rem=7 fouling). Use the
    /// longer of InterCouplerDistance and bounds.
    /// </summary>
    public static float OccupancyCar(float couplerMeters, float boundsMeters)
    {
        var coupler = Positive(couplerMeters);
        var bounds = Positive(boundsMeters);
        return bounds > coupler ? bounds : coupler;
    }

    private static float Positive(float value) =>
        value > 0f && !float.IsNaN(value) && !float.IsInfinity(value) ? value : 0f;
}
