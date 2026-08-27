using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Desk synthetic <c>Turntable</c> token (**8.4**). Same Maps engine as **8.1** —
/// resolve to a real <c>#Y-…</c> track id, then <see cref="MapsDestApply.SetDest"/>.
/// Does not pathfind or bind Switch List multi-leg (that is **8.5**).
/// </summary>
public static class MapsTurntableDest
{
    public const string Token = "Turntable";

    public static bool IsToken(string? trackOrToken) =>
        string.Equals(trackOrToken?.Trim(), Token, StringComparison.OrdinalIgnoreCase);

    /// <summary>Put <see cref="Token"/> first so the Track dropdown can pick Town TT.</summary>
    public static IReadOnlyList<string> WithTokenFirst(IReadOnlyList<string>? listed)
    {
        if (listed == null || listed.Count == 0)
        {
            return new[] { Token };
        }

        var result = new List<string>(listed.Count + 1) { Token };
        for (var i = 0; i < listed.Count; i++)
        {
            var t = listed[i];
            if (IsToken(t))
            {
                continue;
            }

            result.Add(t);
        }

        return result;
    }

    /// <summary>
    /// UI selection → graph track id. Token resolves via <paramref name="resolveTurntable"/>
    /// (Unity FoT + <see cref="TurntableTrackResolver"/>). Named tracks pass through.
    /// </summary>
    public static bool TryResolveTrackId(
        string? yard,
        string? trackOrToken,
        Func<string, string?>? resolveTurntable,
        out string trackId,
        out string? error)
    {
        trackId = string.Empty;
        error = null;
        var y = yard?.Trim();
        var t = trackOrToken?.Trim();
        if (string.IsNullOrEmpty(y) || string.IsNullOrEmpty(t))
        {
            error = "pick city + track";
            return false;
        }

        if (!IsToken(t))
        {
            trackId = t!;
            return true;
        }

        if (resolveTurntable == null)
        {
            error = "no turntable in " + y;
            return false;
        }

        var tt = resolveTurntable(y!);
        if (string.IsNullOrWhiteSpace(tt))
        {
            error = "no turntable in " + y;
            return false;
        }

        trackId = tt!.Trim();
        return true;
    }
}
