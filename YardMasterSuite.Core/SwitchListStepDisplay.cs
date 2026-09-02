namespace YardMasterSuite.Core;

/// <summary>Desk-friendly Switch List lines (**8.7** route multi-leg).</summary>
public static class SwitchListStepDisplay
{
    public static string FormatDeskLine(
        SwitchListStep step,
        int stepIndex,
        int stepCount,
        bool isActive) =>
        FormatDeskLine(step, stepIndex, stepCount, isActive, destNeedsReverse: null);

    public static bool UsesLiveDestFacing(SwitchListStepKind kind) =>
        UsesLiveDriveFacing(kind);

    public static bool UsesLiveDriveFacing(SwitchListStepKind kind) =>
        kind is SwitchListStepKind.TurnAround
            or SwitchListStepKind.Prep
            or SwitchListStepKind.Transit
            or SwitchListStepKind.Pivot
            or SwitchListStepKind.ReverseInto
            or SwitchListStepKind.Delivery;

    /// <summary>Live cab Set word for the active Switch List leg.</summary>
    public static bool? ResolveDriveNeedsReverse(
        SwitchListStep step,
        RouteClearancePhase clearancePhase,
        bool planPinArmed,
        bool sessionHasPin,
        bool pinLatched,
        bool pinTravelReverse,
        bool pinBehindLive,
        bool destBehindLive)
    {
        if (!UsesLiveDriveFacing(step.Kind))
        {
            return null;
        }

        if (step.BindNeedsReverse is bool bind)
        {
            return bind;
        }

        var pinLeg = SwitchListRunner.StepUsesApproachPinFacing(step.Kind)
            && (planPinArmed || sessionHasPin);

        return RouteFacingPhasePolicy.FacingNeedsReverse(
            clearancePhase,
            pinLeg,
            pinLatched,
            pinTravelReverse,
            pinBehindLive,
            destBehindLive);
    }

    /// <summary>
    /// Dest Set word follows <see cref="RouteDestFacingPolicy"/> (pin-reverse
    /// ⇒ dest ahead). Do not pass origin crow-flies <c>IsDestBehind</c>.
    /// </summary>
    public static string LiveLabel(SwitchListStep step, bool? driveNeedsReverse)
    {
        if (driveNeedsReverse is not bool needsReverse)
        {
            return step.Label ?? "";
        }

        switch (step.Kind)
        {
            case SwitchListStepKind.TurnAround:
                if (SwitchListDriveFacing.IsDriveToTurntable(step.Label))
                {
                    return SwitchListDriveFacing.FormatDriveLabel(
                        needsReverse,
                        SwitchListDriveFacing.ToTurntableAction,
                        step.DestTrackId);
                }

                return SwitchListDriveFacing.FormatTurnAroundLabel(needsReverse);
            case SwitchListStepKind.Prep:
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Prep", step.DestTrackId);
            case SwitchListStepKind.Transit:
                return FormatTransitLabel(step, needsReverse);
            case SwitchListStepKind.Pivot:
                return FormatPivotLabel(step, needsReverse);
            case SwitchListStepKind.ReverseInto:
                var action = needsReverse ? "Reverse into" : "into";
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, action, step.DestTrackId);
            case SwitchListStepKind.Delivery:
                return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Delivery", step.DestTrackId);
            default:
                return step.Label ?? "";
        }
    }

    private static string FormatTransitLabel(SwitchListStep step, bool needsReverse)
    {
        var label = step.Label ?? "";
        if (label.IndexOf("Past switch", System.StringComparison.Ordinal) >= 0)
        {
            return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Past switch", step.DestTrackId)
                + " until CLEARED";
        }

        return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Transit", step.DestTrackId);
    }

    private static string FormatPivotLabel(SwitchListStep step, bool needsReverse)
    {
        var label = step.Label ?? "";
        if (label.IndexOf("until CLEARED", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Pivot", step.DestTrackId)
                + " until CLEARED";
        }

        return SwitchListDriveFacing.FormatDriveLabel(needsReverse, "Pivot", step.DestTrackId);
    }

    public static string FormatDeskLine(
        SwitchListStep step,
        int stepIndex,
        int stepCount,
        bool isActive,
        bool? destNeedsReverse)
    {
        var mark = isActive ? "▶ " : "  ";
        var head = stepCount > 0
            ? mark + (stepIndex + 1) + "/" + stepCount + " · "
            : mark;
        return head + CompactLabel(LiveLabel(step, destNeedsReverse));
    }

    public static string CompactLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "—";
        }

        var t = label!.Trim();
        const string until = " until CLEARED";
        if (t.Length >= until.Length
            && t.EndsWith(until, System.StringComparison.OrdinalIgnoreCase))
        {
            return t.Substring(0, t.Length - until.Length).Trim();
        }

        return t;
    }

    public const int DeskLinePx = 20;

    /// <summary>Desk scroll viewport — 7-row lists must show the last row.</summary>
    public static int DeskListViewHeightPx(int stepCount, bool compact)
    {
        if (stepCount <= 0)
        {
            return 0;
        }

        var content = (stepCount * DeskLinePx) + 4;
        var cap = compact ? 56 : 164;
        return content < cap ? content : cap;
    }
}
