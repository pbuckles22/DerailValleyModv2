using System;
using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Prep after a Transit to the same dest. A zero path is legal only when the
/// probe is already that dest. The stale hold applies only when the probe is
/// the track just left (SL-55: SW-B1S). A third track (throat
/// <c>#Y-#S113#T</c>) plans from the live id.
/// </summary>
public static class PrepSameDestOrigin
{
    public enum Kind : byte
    {
        UseProbe = 0,
        AlreadyAtDest = 1,
        HoldDeparted = 2,
    }

    public readonly struct Choice
    {
        public Choice(Kind kind, bool sameDestPrep)
        {
            Kind = kind;
            SameDestPrep = sameDestPrep;
        }

        public Kind Kind { get; }

        /// <summary>Current row is Prep and the previous row is Transit to that same dest.</summary>
        public bool SameDestPrep { get; }
    }

    public static Choice Resolve(
        string? probed,
        string? dest,
        IReadOnlyList<SwitchListStep>? steps,
        int currentIndex)
    {
        var probe = probed?.Trim();
        var goal = dest?.Trim();
        if (string.IsNullOrEmpty(probe) || string.IsNullOrEmpty(goal))
        {
            return new Choice(Kind.UseProbe, sameDestPrep: false);
        }

        if (string.Equals(probe, goal, StringComparison.Ordinal))
        {
            return new Choice(Kind.AlreadyAtDest, IsSameDestPrep(steps, currentIndex, goal!, out _));
        }

        if (!IsSameDestPrep(steps, currentIndex, goal!, out var departed))
        {
            return new Choice(Kind.UseProbe, sameDestPrep: false);
        }

        if (!string.IsNullOrEmpty(departed)
            && !string.Equals(departed, goal, StringComparison.Ordinal)
            && string.Equals(probe, departed, StringComparison.Ordinal))
        {
            return new Choice(Kind.HoldDeparted, sameDestPrep: true);
        }

        return new Choice(Kind.UseProbe, sameDestPrep: true);
    }

    private static bool IsSameDestPrep(
        IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        string goal,
        out string? departed)
    {
        departed = null;
        if (steps == null || currentIndex <= 0 || currentIndex >= steps.Count)
        {
            return false;
        }

        var current = steps[currentIndex];
        var previous = steps[currentIndex - 1];
        if (current == null || previous == null)
        {
            return false;
        }

        if (current.Kind != SwitchListStepKind.Prep
            || previous.Kind != SwitchListStepKind.Transit)
        {
            return false;
        }

        var prevDest = previous.DestTrackId?.Trim();
        var currDest = current.DestTrackId?.Trim();
        if (string.IsNullOrEmpty(prevDest) || string.IsNullOrEmpty(currDest))
        {
            return false;
        }

        if (!string.Equals(prevDest, currDest, StringComparison.Ordinal)
            || !string.Equals(currDest, goal, StringComparison.Ordinal))
        {
            return false;
        }

        departed = RouteStepDestPolicy.WalkFromLabelTrack(steps, currentIndex, goal);
        return true;
    }
}
