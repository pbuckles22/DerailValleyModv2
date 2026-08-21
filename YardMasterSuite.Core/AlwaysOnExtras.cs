namespace YardMasterSuite.Core;

/// <summary>
/// Always-on extras after Heading: Marked · Path · Clock. Station is **6.12**.
/// </summary>
public static class AlwaysOnExtras
{
    public static string Join(string? marked, string? path, string? clock) =>
        MonitorHudLine.Join(new[] { marked ?? "", path ?? "", clock ?? "" });
}
