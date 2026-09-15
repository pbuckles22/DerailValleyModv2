namespace YardMasterSuite.Core;

/// <summary>
/// Consecutive drive pins (Past switch / to TT) flip Set word.
/// Same-direction consecutive pins would be one longer pin.
/// Prep and TT spin are not pins. The next drive after a pin (including
/// Prep) takes the opposite facing — Forward in → Reverse at the pin.
/// </summary>
public static class SwitchListPinFacing
{
    /// <summary>
    /// Engineer golden rule: a frog pin exists only because you clear it and
    /// then drive the opposite way (forward past, then reverse — or reverse
    /// past, then forward). Through-frogs on a same-direction corridor are
    /// not pins. Prep, TT spin, and to-TT are not CLEARED frogs.
    /// </summary>
    public static bool IsClearedFrogPin(SwitchListStep? step)
    {
        if (step == null)
        {
            return false;
        }

        if (step.Kind == SwitchListStepKind.Prep
            || step.Kind == SwitchListStepKind.TurnAround
            || step.Kind == SwitchListStepKind.Delivery
            || SwitchListDriveFacing.IsDriveToTurntable(step.Label))
        {
            return false;
        }

        if (step.Kind == SwitchListStepKind.Pivot)
        {
            return true;
        }

        if (step.Kind != SwitchListStepKind.Transit)
        {
            return false;
        }

        var label = step.Label ?? "";
        return label.IndexOf("Past switch", System.StringComparison.Ordinal) >= 0
            || label.IndexOf("until CLEARED", System.StringComparison.Ordinal) >= 0;
    }

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
    /// Bind/label facing for a row. Used with
    /// <see cref="NextDriveNeedsReverseAfterCleared"/>.
    /// </summary>
    public static bool StepNeedsReverse(SwitchListStep? step)
    {
        if (step == null)
        {
            return false;
        }

        if (step.BindNeedsReverse == true)
        {
            return true;
        }

        return step.Label?.StartsWith(
            SwitchListDriveFacing.Reverse,
            System.StringComparison.Ordinal) == true;
    }

    /// <summary>
    /// Golden rule: a frog pin exists only because after CLEARED you drive
    /// the opposite way (forward past → reverse, or reverse past → forward).
    /// Through-frogs on a same-direction corridor are not pins. Null when
    /// <paramref name="frog"/> is not a CLEARED frog.
    /// </summary>
    public static bool? NextDriveNeedsReverseAfterCleared(SwitchListStep? frog)
    {
        if (!IsClearedFrogPin(frog))
        {
            return null;
        }

        return NeedsReverseAtPin(StepNeedsReverse(frog));
    }

    /// <summary>
    /// Next drive after a CLEARED frog (skips TT spin). Null when none.
    /// </summary>
    public static SwitchListStep? NextDriveAfterClearedFrog(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int frogIndex)
    {
        if (steps == null || frogIndex < 0 || frogIndex >= steps.Count)
        {
            return null;
        }

        if (!IsClearedFrogPin(steps[frogIndex]))
        {
            return null;
        }

        for (var j = frogIndex + 1; j < steps.Count; j++)
        {
            var next = steps[j];
            if (next == null)
            {
                continue;
            }

            if (next.Kind == SwitchListStepKind.TurnAround
                && !SwitchListDriveFacing.IsDriveToTurntable(next.Label))
            {
                continue;
            }

            return next;
        }

        return null;
    }

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
