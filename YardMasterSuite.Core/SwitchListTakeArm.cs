namespace YardMasterSuite.Core;

/// <summary>
/// Take only when GO starts the <em>haul</em> Transit after Prep (job HUD
/// RED→progress). Yard Past-switch / to-TT GO must not activate the job.
/// </summary>
public static class SwitchListTakeArm
{
    /// <summary>
    /// True when this GO should call TakeJob: step supports GO, and either
    /// there is no Prep row, or the current index is past the last Prep.
    /// </summary>
    public static bool IsHaulTransitTake(
        System.Collections.Generic.IReadOnlyList<SwitchListStep>? steps,
        int currentIndex,
        SwitchListStep? step)
    {
        if (step == null || !SwitchListRunner.StepSupportsGo(step))
        {
            return false;
        }

        if (steps == null || steps.Count == 0)
        {
            return false;
        }

        if (currentIndex < 0 || currentIndex >= steps.Count)
        {
            return false;
        }

        var lastPrep = -1;
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i].Kind == SwitchListStepKind.Prep)
            {
                lastPrep = i;
            }
        }

        if (lastPrep < 0)
        {
            return true;
        }

        return currentIndex > lastPrep;
    }
}
