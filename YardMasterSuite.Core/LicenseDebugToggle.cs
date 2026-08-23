namespace YardMasterSuite.Core;

/// <summary>License debug: real career licenses ↔ grant all.</summary>
public enum LicenseDebugMode
{
    Real = 0,
    AllGranted = 1,
}

/// <summary>Pure toggle helper for all-licenses override.</summary>
public static class LicenseDebugToggle
{
    /// <summary>
    /// Debug grant key. Not F11 — that toggles Derail Valley / Unity stats
    /// and poisoned hitch windows during 6.16 smoke.
    /// </summary>
    public const string HotkeyName = "F8";

    public static LicenseDebugMode Next(LicenseDebugMode current) =>
        current == LicenseDebugMode.Real
            ? LicenseDebugMode.AllGranted
            : LicenseDebugMode.Real;

    public static string StatusFragment(LicenseDebugMode mode) =>
        mode == LicenseDebugMode.AllGranted ? "all licenses" : "real licenses";

    public static string FormatLog(LicenseDebugMode mode) =>
        "T2 licenses debug: " + StatusFragment(mode);
}
