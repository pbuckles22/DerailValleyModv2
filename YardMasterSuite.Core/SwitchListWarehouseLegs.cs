namespace YardMasterSuite.Core;

/// <summary>
/// Cab 2.13.2.5.22.22: booklet Load at B4L after pickups. Reader used to
/// skip <c>WarehouseTask</c>, so the list jumped pickup → dest.
/// </summary>
public static class SwitchListWarehouseLegs
{
    public const string IntoLoaderAction = "Into loader";

    public static bool HasWarehouse(string? loadTrackId)
    {
        var id = loadTrackId?.Trim();
        return !string.IsNullOrEmpty(id);
    }

    public static bool ShouldSpotLoader(string? loadTrackId, string? lastPickupTrackId)
    {
        var load = loadTrackId?.Trim();
        var last = lastPickupTrackId?.Trim();
        return !string.IsNullOrEmpty(load)
            && (string.IsNullOrEmpty(last)
                || !string.Equals(load, last, System.StringComparison.OrdinalIgnoreCase));
    }

    public static string SpotLabel(string loadTrackId) =>
        IntoLoaderAction + " → " + loadTrackId.Trim();

    public static bool IsLoaderSpot(string? label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return false;
        }

        return label!.IndexOf(IntoLoaderAction, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string FormatCargoLabel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "None")
        {
            return "";
        }

        var t = raw!.Trim();
        var sb = new System.Text.StringBuilder(t.Length + 4);
        for (var i = 0; i < t.Length; i++)
        {
            if (i > 0 && char.IsUpper(t[i]) && char.IsLower(t[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(t[i]);
        }

        return sb.ToString();
    }

    public static string LoadLabel(string loadTrackId, string? cargoPretty, bool unload)
    {
        var cargo = cargoPretty?.Trim();
        var track = loadTrackId.Trim();
        var verb = unload ? "Unload" : "Load";
        if (string.IsNullOrEmpty(cargo))
        {
            return verb + " at " + track;
        }

        return verb + " " + cargo + " at " + track;
    }

    /// <summary>
    /// Cab 2.13.2.5.22.36: loader sits behind the TT. After last pickup, Past
    /// the inbound pivot (B4L) until CLEARED, then Reverse into the loader —
    /// not a same-row Forward Prep onto B4L.
    /// </summary>
    public static string? LoaderSwitchApproachTrack(
        string? loadTrackId,
        string? lastPickupTrackId,
        string? turntablePivotTrackId,
        string? leaveHopTrackId,
        string? turntableTrackId)
    {
        if (!ShouldSpotLoader(loadTrackId, lastPickupTrackId))
        {
            return null;
        }

        var viaPivot = SwitchListPlanner.LeaveTurntablePastTrack(
            turntablePivotTrackId,
            leaveHopTrackId,
            turntableTrackId,
            lastPickupTrackId);
        if (viaPivot != null)
        {
            return viaPivot;
        }

        var load = loadTrackId?.Trim();
        if (string.IsNullOrEmpty(load))
        {
            return null;
        }

        var last = lastPickupTrackId?.Trim();
        if (!string.IsNullOrEmpty(last)
            && string.Equals(load, last, System.StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return load;
    }
}
