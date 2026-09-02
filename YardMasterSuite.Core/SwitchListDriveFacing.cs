namespace YardMasterSuite.Core;

/// <summary>
/// Drive-set wording for Switch List / Route steps (Set Forward · … / Set Reverse · …).
/// Decoupled from topological <c>plan.ReverseCount</c> (stub hops).
/// </summary>
public static class SwitchListDriveFacing
{
    public const string Forward = "Set Forward";
    public const string Reverse = "Set Reverse";

    /// <summary>Player-facing TurnAround step (not a drive-to track id).</summary>
    public const string TurnAroundOnTurntable = "TT turn around";

    public static string SetWord(bool needsReverse) => needsReverse ? Reverse : Forward;

    /// <summary>e.g. <c>Set Forward · TT turn around</c>.</summary>
    public static string FormatTurnAroundLabel(bool needsReverse) =>
        SetWord(needsReverse) + " · " + TurnAroundOnTurntable;

    /// <summary>e.g. <c>Set Reverse · Pivot → #Y-#S23#T</c>.</summary>
    public static string FormatDriveLabel(bool needsReverse, string action, string trackId)
    {
        var a = string.IsNullOrWhiteSpace(action) ? "Drive" : action.Trim();
        var t = string.IsNullOrWhiteSpace(trackId) ? "—" : trackId.Trim();
        return SetWord(needsReverse) + " · " + a + " → " + t;
    }

    /// <summary>Facing-only Switch List step label.</summary>
    public static string FormatFacingOnlyLabel(bool needsReverse) => SetWord(needsReverse);
}
