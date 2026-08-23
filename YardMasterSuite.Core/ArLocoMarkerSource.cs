namespace YardMasterSuite.Core;

/// <summary>
/// Which locomotive drives the cyan LOCO AR marker (v1 4.9 / 6.16 smoke).
/// LastLoco wins; on a freight car after save-load UsableLoco is the fallback.
/// </summary>
public enum ArLocoMarkerPick
{
    None = 0,
    LastLoco = 1,
    UsableLoco = 2,
}

public static class ArLocoMarkerSource
{
    /// <summary>
    /// Usable-loco lookup is a spherecast + consist walk. AR runs every LateUpdate,
    /// so only probe when LastLoco is missing.
    /// </summary>
    public static bool ShouldProbeUsableLoco(bool hasLastLoco) => !hasLastLoco;

    public static ArLocoMarkerPick Pick(bool hasLastLoco, bool hasUsableLoco)
    {
        if (hasLastLoco)
        {
            return ArLocoMarkerPick.LastLoco;
        }

        return hasUsableLoco ? ArLocoMarkerPick.UsableLoco : ArLocoMarkerPick.None;
    }
}
