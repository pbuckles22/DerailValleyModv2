using System.Collections.Generic;
using DV;
using DV.InventorySystem;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Held JobOverview / JobBooklet scan for Preview + license warn (**6.20**).
    /// Reuses caller buffers. Fail-closed.
    /// </summary>
    internal static class JobPrepReader
    {
        internal static bool TryFillHeldJobs(
            List<Job> jobs,
            HashSet<Job> seen,
            bool includingDropped)
        {
            jobs.Clear();
            seen.Clear();
            try
            {
                var inv = Inventory.Instance;
                if (inv == null)
                {
                    return false;
                }

                var items = inv.GetItemsArray(includingDropped: includingDropped);
                if (items != null)
                {
                    for (var i = 0; i < items.Length; i++)
                    {
                        Consider(items[i], jobs, seen);
                    }
                }

                var handCap = inv.HandCapacity;
                for (var h = 0; h < handCap; h++)
                {
                    Consider(inv.GetEquippedItemAtSlot(h), jobs, seen);
                }

                return jobs.Count > 0;
            }
            catch
            {
                jobs.Clear();
                return false;
            }
        }

        internal static float? PreviewMetersRemaining(List<Job> heldJobs) =>
            TryPreview(heldJobs, out var meters, out _) ? meters : (float?)null;

        internal static bool TryPreview(List<Job> heldJobs, out float meters, out string? yardId)
        {
            meters = 0f;
            yardId = null;
            if (heldJobs == null || heldJobs.Count == 0)
            {
                return false;
            }

            try
            {
                float? best = null;
                string? bestYard = null;
                for (var i = 0; i < heldJobs.Count; i++)
                {
                    var job = heldJobs[i];
                    if (!TryResolveStation(job, out var station, out var resolvedYard) || station == null)
                    {
                        continue;
                    }

                    var range = station.GetComponent<StationJobGenerationRange>();
                    if (range == null)
                    {
                        continue;
                    }

                    var radius = PreviewEdgeDisplay.RadiusFromSqr(range.destroyGeneratedJobsSqrDistanceRegular);
                    var playerDist = PreviewEdgeDisplay.DistanceFromSqr(range.PlayerSqrDistanceFromStationCenter);
                    var remaining = PreviewEdgeDisplay.MetersRemaining(playerDist, radius);
                    if (remaining is not float candidate)
                    {
                        continue;
                    }

                    if (best is null || candidate < best.Value)
                    {
                        best = candidate;
                        bestYard = resolvedYard;
                    }
                }

                if (best is not float win)
                {
                    return false;
                }

                meters = win;
                yardId = bestYard;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryFillMissingLicenseCodes(
            List<Job> heldJobs,
            List<string> raw,
            List<string> codes)
        {
            raw.Clear();
            codes.Clear();
            if (heldJobs == null || heldJobs.Count == 0)
            {
                return false;
            }

            try
            {
                var lm = LicenseManager.Instance;
                if (lm == null)
                {
                    return false;
                }

                for (var i = 0; i < heldJobs.Count; i++)
                {
                    var job = heldJobs[i];
                    if (job == null)
                    {
                        continue;
                    }

                    var required = JobLicenseType_v2.ToV2List(job.requiredLicenses);
                    if (required == null || required.Count == 0 || lm.IsLicensedForJob(required))
                    {
                        continue;
                    }

                    var missing = lm.GetMissingLicensesForJob(required);
                    if (missing == null || missing.Count == 0)
                    {
                        continue;
                    }

                    foreach (var lic in missing)
                    {
                        if (lic == null)
                        {
                            continue;
                        }

                        raw.Add(lic.v1.ToString());
                    }
                }

                var normalized = LicenseWarnDisplay.NormalizeCodes(raw);
                for (var i = 0; i < normalized.Count; i++)
                {
                    codes.Add(normalized[i]);
                }

                return codes.Count > 0;
            }
            catch
            {
                raw.Clear();
                codes.Clear();
                return false;
            }
        }

        internal static string JoinCodes(List<string> codes)
        {
            if (codes == null || codes.Count == 0)
            {
                return string.Empty;
            }

            if (codes.Count == 1)
            {
                return codes[0];
            }

            return string.Join(",", codes);
        }

        private static void Consider(GameObject? go, List<Job> jobs, HashSet<Job> seen)
        {
            if (go == null)
            {
                return;
            }

            Job? job = null;
            var overview = go.GetComponent<JobOverview>();
            if (overview != null)
            {
                job = overview.job;
            }
            else
            {
                var booklet = go.GetComponent<JobBooklet>();
                if (booklet != null)
                {
                    job = booklet.job;
                }
            }

            if (job == null || !seen.Add(job))
            {
                return;
            }

            jobs.Add(job);
        }

        private static bool TryResolveStation(Job job, out StationController? station, out string? yardId)
        {
            station = null;
            yardId = null;
            try
            {
                var originYard = JobOriginYard.Resolve(job.ID, job.chainData?.chainOriginYardId);
                if (!string.IsNullOrWhiteSpace(originYard))
                {
                    station = StationController.GetStationByYardID(originYard);
                    if (station != null && station.StationInfoValid
                        && station.GetComponent<StationJobGenerationRange>() != null)
                    {
                        yardId = originYard;
                        return true;
                    }
                }

                if (!TryNearestStationWithJobRange(out station))
                {
                    return false;
                }

                try
                {
                    yardId = station?.stationInfo?.YardID;
                }
                catch
                {
                    yardId = originYard;
                }

                return station != null;
            }
            catch
            {
                station = null;
                yardId = null;
                return false;
            }
        }

        private static bool TryNearestStationWithJobRange(out StationController? station)
        {
            station = null;
            var stations = StationController.allStations;
            if (stations == null || stations.Count == 0)
            {
                return false;
            }

            StationController? best = null;
            var bestSqr = float.MaxValue;
            for (var i = 0; i < stations.Count; i++)
            {
                var candidate = stations[i];
                if (candidate == null || !candidate.StationInfoValid)
                {
                    continue;
                }

                var range = candidate.GetComponent<StationJobGenerationRange>();
                if (range == null)
                {
                    continue;
                }

                var sqr = range.PlayerSqrDistanceFromStationCenter;
                if (sqr >= bestSqr)
                {
                    continue;
                }

                bestSqr = sqr;
                best = candidate;
            }

            station = best;
            return best != null;
        }
    }
}
