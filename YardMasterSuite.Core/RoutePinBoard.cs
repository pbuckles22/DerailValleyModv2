using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>One Switch List step's planned frog for the numbered pin board.</summary>
public readonly struct RoutePinBoardEntry
{
    public RoutePinBoardEntry(
        int stepIndex,
        string? fromTrackId,
        string? destTrackId,
        string? pinId)
    {
        StepIndex = stepIndex;
        FromTrackId = fromTrackId;
        DestTrackId = destTrackId;
        PinId = pinId;
    }

    public int StepIndex { get; }
    public string? FromTrackId { get; }
    public string? DestTrackId { get; }
    public string? PinId { get; }
}

/// <summary>Unique frog for AR: one world pin, caption lists the steps that own it.</summary>
public readonly struct RoutePinBoardMarker
{
    public RoutePinBoardMarker(string pinId, string caption)
    {
        PinId = pinId;
        Caption = caption;
    }

    public string PinId { get; }
    public string Caption { get; }
}

/// <summary>
/// Planned pin board at list-load. Only CLEARED reversal frogs (dest-side of
/// this-leg dest). Prep / TT / through-frogs are not pins.
/// </summary>
public static class RoutePinBoard
{
    public const int Capacity = 10;

    public static int Collect(
        IReadOnlyList<SwitchListStep>? steps,
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> selected,
        string? destYardId,
        RoutePinBoardEntry[] dest,
        int destLength,
        string? originTrackId = null)
    {
        if (steps == null || dest == null || destLength <= 0 || edges == null || selected == null)
        {
            return 0;
        }

        var n = 0;
        var cap = destLength < dest.Length ? destLength : dest.Length;
        if (cap > Capacity)
        {
            cap = Capacity;
        }

        for (var i = 0; i < steps.Count && n < cap; i++)
        {
            var step = steps[i];
            if (step == null || !SwitchListPinFacing.IsClearedFrogPin(step))
            {
                continue;
            }

            var destForPin = step.DestTrackId?.Trim();
            if (string.IsNullOrEmpty(destForPin))
            {
                continue;
            }

            var from = RouteStepDestPolicy.WalkOppositeEndTrack(steps, i, destForPin);
            if (string.IsNullOrEmpty(from) || SameTrack(from, destForPin))
            {
                var origin = originTrackId?.Trim();
                if (string.IsNullOrEmpty(origin) || SameTrack(origin, destForPin))
                {
                    continue;
                }

                from = origin;
            }

            var yard = string.IsNullOrWhiteSpace(step.DestYardId) ? destYardId : step.DestYardId;
            var walkDest = RouteStepDestPolicy.WalkClearedFrogWalkDest(steps, i, from, destForPin)
                ?? destForPin;
            string? ladderPin = null;
            for (var p = 0; p < n; p++)
            {
                if (SameTrack(dest[p].DestTrackId, walkDest))
                {
                    ladderPin = dest[p].PinId;
                    break;
                }
            }

            string? spentPin = ladderPin;
            string? pin;
            if (RouteStepDestPolicy.NextStepIsLoaderSpot(steps, i)
                && !string.IsNullOrEmpty(ladderPin))
            {
                pin = ladderPin;
            }
            else if (RouteStepDestPolicy.PreferCorridorDestSidePin(steps, i))
            {
                pin = RouteStepDestPolicy.WalkAfterPrepPin(
                    edges,
                    selected,
                    FirstTurnAroundDest(steps) ?? walkDest,
                    from,
                    yard,
                    id => PinAlreadyOnBoard(dest, n, id) || string.Equals(id, spentPin, StringComparison.Ordinal));
            }
            else if (RouteStepDestPolicy.WalkClearedFrogUsesFirstStop(steps, i, walkDest, destForPin))
            {
                pin = RouteStepDestPolicy.WalkFirstStopPin(edges, selected, from, walkDest, yard)
                    ?? RouteStepDestPolicy.WalkClearedFrogPin(
                        edges,
                        selected,
                        from,
                        walkDest,
                        yard,
                        RouteStepDestPolicy.TrackIsTurntableOnList(steps, from));
            }
            else
            {
                pin = RouteStepDestPolicy.WalkClearedFrogPin(
                    edges,
                    selected,
                    from,
                    walkDest,
                    yard,
                    RouteStepDestPolicy.TrackIsTurntableOnList(steps, from));
            }
            if (string.IsNullOrEmpty(pin))
            {
                continue;
            }

            dest[n++] = new RoutePinBoardEntry(step.Index, from, destForPin, pin);
        }

        return n;
    }

    public static int Flatten(
        RoutePinBoardEntry[] entries,
        int entryCount,
        RoutePinBoardMarker[] dest,
        int destLength)
    {
        if (entries == null || dest == null || entryCount <= 0 || destLength <= 0)
        {
            return 0;
        }

        var n = 0;
        var cap = destLength < dest.Length ? destLength : dest.Length;
        var max = entryCount < entries.Length ? entryCount : entries.Length;
        for (var i = 0; i < max && n < cap; i++)
        {
            n = AddOrMerge(dest, n, cap, entries[i].PinId, entries[i].StepIndex.ToString());
        }

        return n;
    }

    /// <summary>
    /// Caption markers for frogs still owned by a later list step.
    /// Spent step 1 must not drop pin 4 when they share a frog.
    /// </summary>
    public static int FlattenRemaining(
        RoutePinBoardEntry[] entries,
        int entryCount,
        RoutePinBoardMarker[] dest,
        int destLength,
        int completedStepIndex)
    {
        if (entries == null || dest == null || entryCount <= 0 || destLength <= 0)
        {
            return 0;
        }

        var n = 0;
        var cap = destLength < dest.Length ? destLength : dest.Length;
        var max = entryCount < entries.Length ? entryCount : entries.Length;
        for (var i = 0; i < max && n < cap; i++)
        {
            if (entries[i].StepIndex <= completedStepIndex)
            {
                continue;
            }

            n = AddOrMerge(dest, n, cap, entries[i].PinId, entries[i].StepIndex.ToString());
        }

        return n;
    }

    public static bool LaterStepOwnsPin(
        RoutePinBoardEntry[] entries,
        int entryCount,
        string? pinId,
        int currentStepIndex)
    {
        var id = pinId?.Trim();
        if (entries == null || entryCount <= 0 || string.IsNullOrEmpty(id))
        {
            return false;
        }

        var max = entryCount < entries.Length ? entryCount : entries.Length;
        for (var i = 0; i < max; i++)
        {
            if (entries[i].StepIndex <= currentStepIndex)
            {
                continue;
            }

            if (string.Equals(entries[i].PinId, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string? PinIdForStep(
        RoutePinBoardEntry[] entries,
        int entryCount,
        int stepIndex)
    {
        if (entries == null || entryCount <= 0)
        {
            return null;
        }

        var max = entryCount < entries.Length ? entryCount : entries.Length;
        for (var i = 0; i < max; i++)
        {
            if (entries[i].StepIndex == stepIndex)
            {
                return entries[i].PinId;
            }
        }

        return null;
    }

    public static string? CaptionForPin(
        RoutePinBoardMarker[] markers,
        int markerCount,
        string? pinId)
    {
        var id = pinId?.Trim();
        if (markers == null || string.IsNullOrEmpty(id) || markerCount <= 0)
        {
            return null;
        }

        var max = markerCount < markers.Length ? markerCount : markers.Length;
        for (var i = 0; i < max; i++)
        {
            if (string.Equals(markers[i].PinId, id, StringComparison.Ordinal))
            {
                return markers[i].Caption;
            }
        }

        return null;
    }

    /// <summary>
    /// Same frog as live At switch / CLEARED — one chip, itinerary label on that pin.
    /// </summary>
    public static string FormatLivePinCaption(string? phaseCaption, string? boardCaption)
    {
        var board = boardCaption?.Trim();
        var phase = phaseCaption?.Trim();
        if (string.IsNullOrEmpty(board))
        {
            return string.IsNullOrEmpty(phase) ? "PIN" : phase!;
        }

        if (string.IsNullOrEmpty(phase) || string.Equals(phase, "PIN", StringComparison.Ordinal))
        {
            return board!;
        }

        return board + " " + phase;
    }

    public static string? FormatEntryLog(in RoutePinBoardEntry entry)
    {
        return "T2 pin-board: step "
            + entry.StepIndex
            + " from="
            + (entry.FromTrackId ?? "—")
            + " dest="
            + (entry.DestTrackId ?? "—")
            + " pin="
            + (entry.PinId ?? "—");
    }

    private static int AddOrMerge(
        RoutePinBoardMarker[] dest,
        int count,
        int cap,
        string? pinId,
        string caption)
    {
        var id = pinId?.Trim();
        if (string.IsNullOrEmpty(id) || count < 0)
        {
            return count;
        }

        for (var i = 0; i < count; i++)
        {
            if (!string.Equals(dest[i].PinId, id, StringComparison.Ordinal))
            {
                continue;
            }

            // Keep the earliest step number only (cab: 1, not 1+4).
            return count;
        }

        if (count >= cap)
        {
            return count;
        }

        dest[count] = new RoutePinBoardMarker(id!, caption);
        return count + 1;
    }

    private static bool PinAlreadyOnBoard(RoutePinBoardEntry[] dest, int n, string? pinId)
    {
        var id = pinId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (var i = 0; i < n; i++)
        {
            if (string.Equals(dest[i].PinId, id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? FirstTurnAroundDest(
        System.Collections.Generic.IReadOnlyList<SwitchListStep> steps)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Kind != SwitchListStepKind.TurnAround)
            {
                continue;
            }

            var dest = steps[i].DestTrackId?.Trim();
            if (!string.IsNullOrEmpty(dest))
            {
                return dest;
            }
        }

        return null;
    }

    private static bool SameTrack(string? a, string? b)
    {
        var x = a?.Trim();
        var y = b?.Trim();
        return !string.IsNullOrEmpty(x)
            && !string.IsNullOrEmpty(y)
            && string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
    }
}
