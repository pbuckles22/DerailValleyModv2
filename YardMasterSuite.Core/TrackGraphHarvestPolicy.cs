namespace YardMasterSuite.Core;

/// <summary>
/// 9.1.3 Win 0 — one-shot local subgraph (≤2.5 km), not full-map cache.
/// Sit still + Maps dest so the player can throw the leave switch first.
/// </summary>
public static class TrackGraphHarvestPolicy
{
    public const float RadiusMeters = PostedBoardActiveRoster.ActiveRadiusMeters;

    public static bool IsStill(float speedKmh) =>
        speedKmh <= PostedStickyLimit.StandstillMaxSpeedKmh;

    public static bool ShouldScan(bool alreadyWritten, bool mapsLeg, bool still) =>
        !alreadyWritten && mapsLeg && still;

    public static bool ShouldWrite(
        bool alreadyWritten,
        bool mapsLeg,
        bool still,
        int trackCount,
        int junctionCount) =>
        ShouldScan(alreadyWritten, mapsLeg, still) && trackCount > 0 && junctionCount > 0;

    public static bool IsWithinRadius(float locoX, float locoZ, float x, float z)
    {
        var dx = x - locoX;
        var dz = z - locoZ;
        var r = RadiusMeters;
        return (dx * dx) + (dz * dz) <= r * r;
    }

    /// <summary>Keep a track if either Bezier end sits inside the dump circle.</summary>
    public static bool IncludeTrack(
        float locoX,
        float locoZ,
        float inX,
        float inZ,
        float outX,
        float outZ) =>
        IsWithinRadius(locoX, locoZ, inX, inZ) || IsWithinRadius(locoX, locoZ, outX, outZ);
}
