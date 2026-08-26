namespace YardMasterSuite.Core;

/// <summary>Discrete Player.log lines for **8.1** Maps desk. Click/open only — not per frame.</summary>
public static class MapsDestTelemetry
{
    public const string DestSet = "T2 maps: dest set";
    public const string Recheck = "T2 maps: recheck";
    public const string DestClear = "T2 maps: dest clear";
    public const string RejectEmpty = "T2 maps: reject empty";
    public const string DeskOpen = "T2 maps-desk: open";
    public const string DeskClose = "T2 maps-desk: close";

    public static string Format(MapsDestKind kind, string? city, string? track)
    {
        switch (kind)
        {
            case MapsDestKind.Set:
                return DestSet + " city=" + (city ?? string.Empty) + " track=" + (track ?? string.Empty);
            case MapsDestKind.Recheck:
                return Recheck + " city=" + (city ?? string.Empty) + " track=" + (track ?? string.Empty);
            case MapsDestKind.Clear:
                return DestClear;
            default:
                return RejectEmpty;
        }
    }

    public static string FormatCatalog(int cities, int tracks) =>
        "T2 maps-desk: catalog cities=" + cities + " tracks=" + tracks;
}
