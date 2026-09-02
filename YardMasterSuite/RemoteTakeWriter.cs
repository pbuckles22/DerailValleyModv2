using DV;
using DV.Booklets;
using DV.Logic.Job;
using DV.ThingTypes;
using UnityEngine;

namespace YardMasterSuite
{
    /// <summary>
    /// Calls vanilla <see cref="JobsManager.TakeJob"/> from the desk (**13.6.1**).
    /// Fail-closed. Office validator is not required — the manager API exists.
    /// </summary>
    internal static class RemoteTakeWriter
    {
        internal static bool ApiAllowsTake()
        {
            try
            {
                return JobsManager.Instance != null;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryReadPaper(
            Job? job,
            out bool previewHeld,
            out bool alreadyTaken)
        {
            previewHeld = false;
            alreadyTaken = false;
            if (job == null)
            {
                return false;
            }

            try
            {
                alreadyTaken = job.State == JobState.InProgress;
                previewHeld = job.State == JobState.Available;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryTake(Job job)
        {
            try
            {
                var mgr = JobsManager.Instance;
                if (mgr == null)
                {
                    return false;
                }

                mgr.TakeJob(job, takenViaLoadGame: false);
                if (job.State != JobState.InProgress)
                {
                    return false;
                }

                TrySpawnBooklet(job);
                JobPrepReader.TryDestroyHeldOverview(job);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TrySpawnBooklet(Job job)
        {
            try
            {
                var player = PlayerManager.PlayerTransform;
                if (player == null)
                {
                    return;
                }

                BookletCreator.CreateJobBooklet(
                    job,
                    player.position,
                    player.rotation,
                    player,
                    addToWorldStorage: true);
            }
            catch
            {
                // fail closed — take already registered
            }
        }
    }
}
