using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Ordered Prep spurs for Switch List. Ticket SL-55:
/// <c>[ B1S, C3S ] → B4L → C1O</c> — not only starts[0].
/// </summary>
public static class SwitchListPickupTracks
{
    /// <summary>
    /// Distinct spur ids from DV task starts, skipping connectors, the final
    /// dest, and staging / reverse-into leads (e.g. B4L on SL-55).
    /// </summary>
    public static IReadOnlyList<string> FromTaskStarts(
        IReadOnlyList<string>? starts,
        string? destTrackId,
        string? reverseIntoTrackId = null)
    {
        if (starts == null || starts.Count == 0)
        {
            return System.Array.Empty<string>();
        }

        var dest = destTrackId?.Trim();
        var staging = reverseIntoTrackId?.Trim();
        var list = new List<string>(starts.Count);
        for (var i = 0; i < starts.Count; i++)
        {
            var id = starts[i]?.Trim();
            if (string.IsNullOrEmpty(id) || IsConnector(id))
            {
                continue;
            }

            if (dest != null && string.Equals(id, dest, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (staging != null && string.Equals(id, staging, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dup = false;
            for (var j = 0; j < list.Count; j++)
            {
                if (string.Equals(list[j], id, System.StringComparison.OrdinalIgnoreCase))
                {
                    dup = true;
                    break;
                }
            }

            if (!dup)
            {
                list.Add(id!);
            }
        }

        return list;
    }

    /// <summary>
    /// Prep order: <see cref="JobSummary.OriginTrackId"/> then
    /// <see cref="JobSummary.AdditionalPickupTrackIds"/>.
    /// </summary>
    public static IReadOnlyList<string> Resolve(JobSummary? job)
    {
        if (job == null)
        {
            return System.Array.Empty<string>();
        }

        var origin = job.OriginTrackId?.Trim();
        if (string.IsNullOrEmpty(origin) || IsConnector(origin))
        {
            return System.Array.Empty<string>();
        }

        var list = new List<string> { origin! };
        var extra = job.AdditionalPickupTrackIds;
        if (extra == null)
        {
            return list;
        }

        for (var i = 0; i < extra.Length; i++)
        {
            var id = extra[i]?.Trim();
            if (string.IsNullOrEmpty(id) || IsConnector(id))
            {
                continue;
            }

            if (string.Equals(id, origin, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dup = false;
            for (var j = 0; j < list.Count; j++)
            {
                if (string.Equals(list[j], id, System.StringComparison.OrdinalIgnoreCase))
                {
                    dup = true;
                    break;
                }
            }

            if (!dup)
            {
                list.Add(id!);
            }
        }

        return list;
    }

    public static bool IsConnector(string? trackId)
    {
        var id = trackId?.Trim();
        if (string.IsNullOrEmpty(id) || id == "---")
        {
            return true;
        }

        return id!.StartsWith("#Y-", System.StringComparison.OrdinalIgnoreCase)
            || id.StartsWith("#Y#", System.StringComparison.OrdinalIgnoreCase);
    }
}
