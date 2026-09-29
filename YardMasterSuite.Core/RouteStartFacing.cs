namespace YardMasterSuite.Core;

/// <summary>
/// Start direction for the route queue from topology: the plan leaves the origin track
/// through the end nearest <c>TrackIds[1]</c>. A straight-line dot at a far pin is not
/// the rail direction (cab 2.16.34 B1S: dot at a stale C4S pin said Reverse, the
/// corridor leaves B4L Forward).
/// </summary>
public static class RouteStartFacing
{
    /// <param name="aX">Origin track end A (X).</param>
    /// <param name="bX">Origin track end B (X).</param>
    /// <param name="n1X">Next plan track end 1 (X).</param>
    /// <param name="n2X">Next plan track end 2 (X).</param>
    public static bool NeedsReverse(
        float fwdX,
        float fwdZ,
        float posX,
        float posZ,
        float aX,
        float aZ,
        float bX,
        float bZ,
        float n1X,
        float n1Z,
        float n2X,
        float n2Z)
    {
        var toA = MinSq(aX, aZ, n1X, n1Z, n2X, n2Z);
        var toB = MinSq(bX, bZ, n1X, n1Z, n2X, n2Z);
        var exitX = toA <= toB ? aX : bX;
        var exitZ = toA <= toB ? aZ : bZ;
        return DriveSetFacing.IsTargetBehind(fwdX, fwdZ, exitX - posX, exitZ - posZ);
    }

    private static float MinSq(float x, float z, float x1, float z1, float x2, float z2)
    {
        var d1 = ((x - x1) * (x - x1)) + ((z - z1) * (z - z1));
        var d2 = ((x - x2) * (x - x2)) + ((z - z2) * (z - z2));
        return d1 <= d2 ? d1 : d2;
    }
}
