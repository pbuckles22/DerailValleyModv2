namespace YardMasterSuite.Core;

/// <summary>
/// Consecutive drive pins (Past switch / to TT) flip Set word.
/// Same-direction consecutive pins would be one longer pin.
/// Prep and TT spin are not pins. The next drive after a pin (including
/// Prep) takes the opposite facing — Forward in → Reverse at the pin.
/// </summary>
public static class SwitchListPinFacing
{
    public static bool IsDrivePin(SwitchListStep? step)
    {
        if (step == null)
        {
            return false;
        }

        if (step.Kind == SwitchListStepKind.Pivot)
        {
            return true;
        }

        if (step.Kind == SwitchListStepKind.TurnAround
            && SwitchListDriveFacing.IsDriveToTurntable(step.Label))
        {
            return true;
        }

        if (step.Kind != SwitchListStepKind.Transit)
        {
            return false;
        }

        var label = step.Label ?? "";
        return label.IndexOf("Past switch", System.StringComparison.Ordinal) >= 0
            || SwitchListDriveFacing.IsDriveToTurntable(label);
    }

    public static bool AlternateNeedsReverse(bool previousPinNeedsReverse) =>
        !previousPinNeedsReverse;

    /// <summary>
    /// Hard cab rule: at a pin, drive the opposite of the approach that got
    /// you there. Forward in → Reverse at pin. Reverse in → Forward at pin.
    /// </summary>
    public static bool NeedsReverseAtPin(bool approachedNeedsReverse) =>
        AlternateNeedsReverse(approachedNeedsReverse);

    /// <summary>
    /// Facing for the next pin when <paramref name="previous"/> is itself a pin.
    /// Null when the previous row is Prep / TT spin / not a pin.
    /// </summary>
    public static bool? AlternateAfter(SwitchListStep? previous)
    {
        if (!IsDrivePin(previous))
        {
            return null;
        }

        var prevReverse = previous!.BindNeedsReverse
            ?? previous.Label?.StartsWith(
                SwitchListDriveFacing.Reverse,
                System.StringComparison.Ordinal) == true;
        return AlternateNeedsReverse(prevReverse);
    }
}
