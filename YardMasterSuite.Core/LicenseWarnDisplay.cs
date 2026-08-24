using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Early warn when a held overview/booklet needs job licenses the player lacks
/// (**6.20**). Display only — does not change Preview wipe.
/// </summary>
public static class LicenseWarnDisplay
{
    public const string WarnColor = "#FF5555";

    /// <summary>
    /// <c>No license: FH</c> / <c>No license: FH, HZ1</c>. Null when nothing missing.
    /// </summary>
    public static string? Format(IReadOnlyList<string>? missingShortCodes, bool richText = false)
    {
        if (missingShortCodes == null || missingShortCodes.Count == 0)
        {
            return null;
        }

        var parts = new List<string>(missingShortCodes.Count);
        for (var i = 0; i < missingShortCodes.Count; i++)
        {
            var trimmed = missingShortCodes[i]?.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                parts.Add(trimmed!);
            }
        }

        if (parts.Count == 0)
        {
            return null;
        }

        var text = "No license: " + string.Join(", ", parts);
        if (!richText)
        {
            return text;
        }

        return $"<color={WarnColor}>{text}</color>";
    }

    public static string Abbreviate(string? licenseEnumOrId)
    {
        var key = licenseEnumOrId?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        switch (key)
        {
            case "FreightHaul":
            case "FH":
                return "FH";
            case "Shunting":
            case "SH":
                return "SH";
            case "LogisticalHaul":
            case "LH":
                return "LH";
            case "Hazmat1":
            case "HZ1":
                return "HZ1";
            case "Hazmat2":
            case "HZ2":
                return "HZ2";
            case "Hazmat3":
            case "HZ3":
                return "HZ3";
            case "Military1":
            case "M1":
                return "M1";
            case "Military2":
            case "M2":
                return "M2";
            case "Military3":
            case "M3":
                return "M3";
            case "TrainLength1":
            case "TL1":
                return "TL1";
            case "TrainLength2":
            case "TL2":
                return "TL2";
            case "Fragile":
            case "FR":
                return "FR";
            case "Dispatcher1":
            case "Dispatcher":
            case "DISP":
                return "DISP";
            case "Basic":
                return "Basic";
            default:
                return key!;
        }
    }

    public static IReadOnlyList<string> NormalizeCodes(IEnumerable<string>? codes)
    {
        if (codes == null)
        {
            return Array.Empty<string>();
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<string>();
        foreach (var raw in codes)
        {
            var abbr = Abbreviate(raw);
            if (string.IsNullOrEmpty(abbr) || !seen.Add(abbr))
            {
                continue;
            }

            list.Add(abbr);
        }

        return list;
    }
}
