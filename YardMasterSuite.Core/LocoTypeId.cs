using System;

namespace YardMasterSuite.Core;

/// <summary>Normalize loco type ids for dropdown match (LocoDH4 / DH4 / Loco DH4).</summary>
public static class LocoTypeId
{
    public const string De2 = "DE2";

    /// <summary>
    /// DE2 11-notch expander. Avoids <see cref="Normalize"/> so HTP plant
    /// ticks stay alloc-free.
    /// </summary>
    public static bool IsDe2(string? typeId)
    {
        if (string.IsNullOrEmpty(typeId))
        {
            return false;
        }

        var id = typeId!;
        if (id.Equals(De2, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (id.Equals("LocoDE2", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return id.Equals("Loco DE2", StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var id = raw!.Trim();
        const string locoPrefix = "Loco";
        if (id.StartsWith(locoPrefix, StringComparison.OrdinalIgnoreCase)
            && id.Length > locoPrefix.Length)
        {
            var rest = id.Substring(locoPrefix.Length).TrimStart();
            if (!string.IsNullOrEmpty(rest))
            {
                id = rest;
            }
        }

        return id.ToUpperInvariant();
    }

    public static bool Matches(string? carTypeId, string? selectedNormalized)
    {
        var selected = Normalize(selectedNormalized);
        if (string.IsNullOrEmpty(selected))
        {
            return false;
        }

        return string.Equals(Normalize(carTypeId), selected, StringComparison.Ordinal);
    }

    /// <summary>Short desk label (DH4, DE6) from any raw id.</summary>
    public static string DisplayLabel(string? raw)
    {
        var n = Normalize(raw);
        return string.IsNullOrEmpty(n) ? "—" : n;
    }
}
