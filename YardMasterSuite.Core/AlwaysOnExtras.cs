namespace YardMasterSuite.Core;

/// <summary>
/// Always-on extras after Heading: Marked · Station · Path · Clock.
/// </summary>
public static class AlwaysOnExtras
{
    public static string Join(string? marked, string? path, string? clock) =>
        Join(marked, station: null, path, clock);

    public static string Join(string? marked, string? station, string? path, string? clock) =>
        MonitorHudLine.Join(new[] { marked ?? "", station ?? "", path ?? "", clock ?? "" });
}
