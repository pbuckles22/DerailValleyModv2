using System;

namespace YardMasterSuite.Core;

/// <summary>
/// Job AR pins: one per pickup track (car group), not per car.
/// Caption = job id · spur · count · distance.
/// </summary>
public static class JobCarMarkerDisplay
{
    /// <summary>Cap on simultaneous pickup-group pins.</summary>
    public const int DefaultMaxMarkers = 8;

    public static string? ShortSpurLabel(string? trackDisplay)
    {
        var t = trackDisplay?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return null;
        }

        // MF-C2S / HB-G3O → C2S / G3O (booklet-style spur). Keep #Y-* intact.
        var dash = t!.IndexOf('-');
        if (dash > 0 && dash <= 3 && dash < t.Length - 1 && t[0] != '#')
        {
            return t.Substring(dash + 1);
        }

        return t;
    }

    public static string FormatCaption(string? jobId, int carCount, float distanceMeters) =>
        FormatCaption(jobId, trackLabel: null, carCount, distanceMeters);

    public static string FormatCaption(
        string? jobId,
        string? trackLabel,
        int carCount,
        float distanceMeters)
    {
        var meters = CaptionMeters(distanceMeters);
        var id = jobId?.Trim();
        var track = trackLabel?.Trim();
        var n = carCount < 0 ? 0 : carCount;

        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(track))
        {
            return id + " · " + track + " · " + n + " " + meters + "m";
        }

        if (!string.IsNullOrEmpty(id))
        {
            return id + " · " + n + " " + meters + "m";
        }

        if (!string.IsNullOrEmpty(track))
        {
            return track + " · " + n + " " + meters + "m";
        }

        return n > 0 ? n + " cars " + meters + "m" : meters + "m";
    }

    /// <summary>
    /// Metre value the caption shows. AR draws every frame, so callers cache the
    /// caption against this and only rebuild the string when it changes.
    /// </summary>
    public static int CaptionMeters(float distanceMeters) =>
        (int)Math.Round(Math.Max(0f, distanceMeters), MidpointRounding.AwayFromZero);

    /// <summary>
    /// Hide AR when every expected job car is already on the consist (GO),
    /// including Preview paperwork. Hide while Switch List GO is driving
    /// (**13.4** automation). Backpack / held jobs still show while cars
    /// remain off the train and GO is off.
    /// </summary>
    public static bool ShouldShowAr(bool jobTaken, JobConsistStatus status, int expectedCars) =>
        ShouldShowAr(jobTaken, status, expectedCars, switchListGoActive: false);

    public static bool ShouldShowAr(
        bool jobTaken,
        JobConsistStatus status,
        int expectedCars,
        bool switchListGoActive)
    {
        if (expectedCars <= 0 || switchListGoActive)
        {
            return false;
        }

        _ = jobTaken;
        return status != JobConsistStatus.Ready;
    }

    /// <summary>
    /// No pin on anonymous <c>#Y</c> connectors / turntable tracks, or missing spur.
    /// Real pickup spurs (C1O) still pin.
    /// </summary>
    public static bool CanPinTrack(string? trackId)
    {
        var t = trackId?.Trim();
        if (string.IsNullOrEmpty(t) || t == "—" || t == "---")
        {
            return false;
        }

        if (PathRouteConstraints.IsAnonymousTrack(t))
        {
            return false;
        }

        var shortLabel = ShortSpurLabel(t);
        return !PathRouteConstraints.IsAnonymousTrack(shortLabel)
            && shortLabel != "—"
            && shortLabel != "---";
    }

    /// <summary>Coupled task cars hide even while the ticket is Preview.</summary>
    public static bool HideAttachedCarPin(bool attachedToConsist) => attachedToConsist;
}
