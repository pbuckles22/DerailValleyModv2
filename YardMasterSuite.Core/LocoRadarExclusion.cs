using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Radar exclusion seeds (6.16). LastLoco walks the trainset so an MU mate
/// is not an amber "other" after hop-off (6.16.14).
/// </summary>
public static class LocoRadarExclusion
{
    public static void AddLastLocoTrainset(
        ISet<int> exclude,
        int lastLocoId,
        int[]? trainsetLocoIds)
    {
        if (exclude == null)
        {
            return;
        }

        if (lastLocoId != 0)
        {
            exclude.Add(lastLocoId);
        }

        if (trainsetLocoIds == null)
        {
            return;
        }

        for (var i = 0; i < trainsetLocoIds.Length; i++)
        {
            var id = trainsetLocoIds[i];
            if (id != 0)
            {
                exclude.Add(id);
            }
        }
    }
}
