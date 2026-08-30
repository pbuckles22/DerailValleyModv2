using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Desk picker defaults for smoke/testing — SW + Turntable when no session dest.
/// </summary>
public static class MapsDeskDefaults
{
    public const string PreferredYard = "SW";

    /// <summary>
    /// Prefer <see cref="PreferredYard"/> when present and no dest yard is bound;
    /// otherwise keep <paramref name="currentIndex"/> (clamped).
    /// </summary>
    public static int ResolveYardIndex(
        IReadOnlyList<string>? yards,
        string? sessionYardId,
        int currentIndex)
    {
        if (yards == null || yards.Count == 0)
        {
            return 0;
        }

        if (!string.IsNullOrEmpty(sessionYardId))
        {
            for (var i = 0; i < yards.Count; i++)
            {
                if (string.Equals(yards[i], sessionYardId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        for (var i = 0; i < yards.Count; i++)
        {
            if (string.Equals(yards[i], PreferredYard, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        if (currentIndex < 0 || currentIndex >= yards.Count)
        {
            return 0;
        }

        return currentIndex;
    }

    /// <summary>
    /// Prefer Turntable token when no session track is bound; else match session
    /// or clamp.
    /// </summary>
    public static int ResolveTrackIndex(
        IReadOnlyList<string>? tracks,
        string? sessionTrackId,
        int currentIndex)
    {
        if (tracks == null || tracks.Count == 0)
        {
            return 0;
        }

        if (!string.IsNullOrEmpty(sessionTrackId))
        {
            for (var i = 0; i < tracks.Count; i++)
            {
                if (string.Equals(tracks[i], sessionTrackId, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }
        else
        {
            for (var i = 0; i < tracks.Count; i++)
            {
                if (MapsTurntableDest.IsToken(tracks[i]))
                {
                    return i;
                }
            }
        }

        if (currentIndex < 0 || currentIndex >= tracks.Count)
        {
            return 0;
        }

        return currentIndex;
    }
}
