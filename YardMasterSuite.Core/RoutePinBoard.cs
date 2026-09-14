using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// One Switch List pin-leg: label dest walk vs Maps dest walk.
/// Cab check with no driving — split means the overlay stop is not the Maps pin.
/// </summary>
public readonly struct RoutePinBoardEntry
{
    public RoutePinBoardEntry(
        int stepIndex,
        string? fromTrackId,
        string? labelDest,
        string? mapsDest,
        string? labelPinId,
        string? mapsPinId)
    {
        StepIndex = stepIndex;
        FromTrackId = fromTrackId;
        LabelDest = labelDest;
        MapsDest = mapsDest;
        LabelPinId = labelPinId;
        MapsPinId = mapsPinId;
    }

    public int StepIndex { get; }
    public string? FromTrackId { get; }
    public string? LabelDest { get; }
    public string? MapsDest { get; }
    public string? LabelPinId { get; }
    public string? MapsPinId { get; }

    public bool Split
    {
        get
        {
            var label = LabelPinId?.Trim();
            var maps = MapsPinId?.Trim();
            return !string.IsNullOrEmpty(label)
                && !string.IsNullOrEmpty(maps)
                && !string.Equals(label, maps, StringComparison.Ordinal);
        }
    }
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
/// Planned pin board at list-load / Set dest. Walks every pin-leg from the
/// itinerary origin (previous Maps dest), not the live loco.
/// </summary>
public static class RoutePinBoard
{
    public const int Capacity = 8;

    public static int Collect(
        IReadOnlyList<SwitchListStep>? steps,
        IReadOnlyList<PathEdge> edges,
        IReadOnlyDictionary<string, int> selected,
        string? destYardId,
        RoutePinBoardEntry[] dest,
        int destLength)
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
            if (step == null || !SwitchListRunner.StepNeedsPinClearance(step.Kind))
            {
                continue;
            }

            if (!RouteStepDestPolicy.TryMapsDestForListProgress(
                    steps,
                    i,
                    "list-next",
                    out var mapsDest,
                    out _,
                    out _))
            {
                continue;
            }

            var from = RouteStepDestPolicy.WalkFromTrack(steps, i, mapsDest);
            var labelDest = step.DestTrackId?.Trim();
            var yard = string.IsNullOrWhiteSpace(step.DestYardId) ? destYardId : step.DestYardId;
            var pullOut = RouteStepDestPolicy.IsPullOutAfterPrep(steps, i);
            var labelPin = SameTrack(from, labelDest)
                ? null
                : pullOut
                    ? RouteStepDestPolicy.WalkPullOutThroatPin(edges, selected, from, labelDest, yard)
                    : RouteStepDestPolicy.WalkFirstStopPin(edges, selected, from, labelDest, yard);
            var mapsPin = SameTrack(from, mapsDest)
                ? null
                : pullOut
                    ? RouteStepDestPolicy.WalkPullOutThroatPin(edges, selected, from, mapsDest, yard)
                    : RouteStepDestPolicy.WalkFirstStopPin(edges, selected, from, mapsDest, yard);
            dest[n++] = new RoutePinBoardEntry(
                step.Index,
                from,
                labelDest,
                mapsDest?.Trim(),
                labelPin,
                mapsPin);
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
            var e = entries[i];
            if (e.Split)
            {
                n = AddOrMerge(dest, n, cap, e.LabelPinId, e.StepIndex + "L");
                n = AddOrMerge(dest, n, cap, e.MapsPinId, e.StepIndex + "M");
                continue;
            }

            var pin = !string.IsNullOrEmpty(e.LabelPinId?.Trim()) ? e.LabelPinId : e.MapsPinId;
            n = AddOrMerge(dest, n, cap, pin, e.StepIndex.ToString());
        }

        return n;
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

    public static int CountSplits(RoutePinBoardEntry[] entries, int entryCount)
    {
        if (entries == null || entryCount <= 0)
        {
            return 0;
        }

        var n = 0;
        var max = entryCount < entries.Length ? entryCount : entries.Length;
        for (var i = 0; i < max; i++)
        {
            if (entries[i].Split)
            {
                n++;
            }
        }

        return n;
    }

    public static string? FormatEntryLog(in RoutePinBoardEntry entry)
    {
        return "T2 pin-board: step "
            + entry.StepIndex
            + " from="
            + (entry.FromTrackId ?? "—")
            + " L="
            + (entry.LabelDest ?? "—")
            + " pin="
            + (entry.LabelPinId ?? "—")
            + " M="
            + (entry.MapsDest ?? "—")
            + " pin="
            + (entry.MapsPinId ?? "—")
            + (entry.Split ? " split=1" : " split=0");
    }

    public static string FormatCaption(int stepIndex, string? destTrackId)
    {
        var shortDest = JobCarMarkerDisplay.ShortSpurLabel(destTrackId) ?? "pin";
        return stepIndex + " " + shortDest;
    }

    public static string FormatDeskLine(in RoutePinBoardEntry entry)
    {
        return entry.StepIndex
            + " L="
            + ShortDest(entry.LabelDest)
            + " p="
            + (entry.LabelPinId ?? "—")
            + " M="
            + ShortDest(entry.MapsDest)
            + " p="
            + (entry.MapsPinId ?? "—")
            + (entry.Split ? " SPLIT" : "");
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

            dest[i] = new RoutePinBoardMarker(id!, dest[i].Caption + "+" + caption);
            return count;
        }

        if (count >= cap)
        {
            return count;
        }

        dest[count] = new RoutePinBoardMarker(id!, caption);
        return count + 1;
    }

    private static string ShortDest(string? dest)
    {
        return JobCarMarkerDisplay.ShortSpurLabel(dest) ?? dest ?? "—";
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
