using System;
using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Job bar (**6.13** + **6.20**). Priority: live taken → Cancelled flash →
    /// license warn + Preview (held overview/booklet). Hidden when none apply.
    /// </summary>
    public sealed class JobBarListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private readonly List<Car> _expectedLogic = new List<Car>(16);
        private readonly List<int> _expectedIds = new List<int>(16);
        private readonly List<Job> _heldJobs = new List<Job>(8);
        private readonly HashSet<Job> _heldSeen = new HashSet<Job>();
        private readonly List<string> _rawLicenses = new List<string>(8);
        private readonly List<string> _missingLicenses = new List<string>(8);

        private ActiveJobCache _cache;
        private ActiveJobDebugSnapshot? _lastLog;
        private CancelledFlashState _flash;
        private Job? _lifecycleHookJob;
        private bool _backupCancelledNoted;
        private string _lastLine = string.Empty;
        private string _expectedJobId = string.Empty;
        private float _nextAt;

        private void OnEnable()
        {
            _cache = default;
            _lastLog = null;
            _lastLine = string.Empty;
            _expectedJobId = string.Empty;
            _expectedLogic.Clear();
            _expectedIds.Clear();
            CancelledFlash.Clear(ref _flash);
            _backupCancelledNoted = false;
            UnhookJobLifecycle();
            _nextAt = 0f;
            Publish(force: true);
        }

        private void OnDisable()
        {
            _expectedLogic.Clear();
            _expectedIds.Clear();
            _expectedJobId = string.Empty;
            _heldJobs.Clear();
            _heldSeen.Clear();
            _rawLicenses.Clear();
            _missingLicenses.Clear();
            _lastLog = null;
            CancelledFlash.Clear(ref _flash);
            UnhookJobLifecycle();
        }

        private void LateUpdate()
        {
            if (PlayerManager.PlayerTransform == null)
            {
                return;
            }

            if (Time.unscaledTime < _nextAt)
            {
                return;
            }

            _nextAt = Time.unscaledTime + 0.25f;
            Publish(force: false);
        }

        private void Publish(bool force)
        {
            Sample(
                out var line,
                out var visible,
                out var jobId,
                out var extra,
                out var status,
                out var remaining,
                out var kind,
                out var previewMeters,
                out var licenseCodes,
                out var originYard);

            if (force || line != _lastLine)
            {
                _lastLine = line;
                YmsEventBus.RaiseJobBarChanged(new HudBarSnapshot(line, visible));
            }

            var changed = kind == JobBarKind.Prep
                ? ActiveJobTelemetry.ObservePrep(previewMeters, licenseCodes, originYard, ref _cache)
                : kind == JobBarKind.Cancelled
                    ? ActiveJobTelemetry.ObserveCancelled(jobId, ref _cache)
                    : ActiveJobTelemetry.Observe(visible, jobId, extra, status, remaining, ref _cache);

            if (!changed)
            {
                return;
            }

            var snap = ActiveJobTelemetry.Snapshot(ref _cache);
            var msg = ActiveJobTelemetry.NextLog(_lastLog, snap);
            _lastLog = snap;
            if (msg != null)
            {
                EmitLog?.Invoke(msg);
            }
        }

        private void Sample(
            out string line,
            out bool visible,
            out string? jobId,
            out int extra,
            out JobConsistStatus status,
            out float? remaining,
            out JobBarKind kind,
            out float? previewMeters,
            out string? licenseCodes,
            out string? originYard)
        {
            line = string.Empty;
            visible = false;
            jobId = null;
            extra = 0;
            status = JobConsistStatus.Missing;
            remaining = null;
            kind = JobBarKind.Hidden;
            previewMeters = null;
            licenseCodes = null;
            originYard = null;

            var now = Time.unscaledTime;
            var liveTaken = TryGetPrimaryTakenJob(out var job, out extra) && job != null;
            EnsureLifecycleHooks(liveTaken ? job : null);
            if (liveTaken)
            {
                _backupCancelledNoted = false;
            }
            else
            {
                NoteCancelledIfPresent(now);
            }

            if (CancelledFlash.TryConsume(ref _flash, now, liveTaken, out var cancelledId))
            {
                jobId = cancelledId;
                line = ActiveJobHudLine.FormatCancelled(cancelledId, richText: true);
                visible = true;
                kind = JobBarKind.Cancelled;
                return;
            }

            if (liveTaken && job != null)
            {
                jobId = job.ID?.Trim();
                if (string.IsNullOrEmpty(jobId))
                {
                    _expectedJobId = string.Empty;
                    _expectedLogic.Clear();
                    return;
                }

                remaining = BonusTimeDisplay.RemainingSeconds(job.TimeLimit, SafeTimeOnJob(job));
                EnsureExpected(job, jobId);
                status = JobConsistProbe.Evaluate(job, SeedCar(), _expectedLogic, _expectedIds);
                line = ActiveJobHudLine.Format(
                    ActiveJobHudLine.FormatJobId(jobId, extra),
                    JobConsistStatusDisplay.FormatHud(status),
                    BonusTimeDisplay.Format(remaining, richText: true));
                visible = !string.IsNullOrWhiteSpace(line);
                kind = JobBarKind.Taken;
                return;
            }

            _expectedJobId = string.Empty;
            _expectedLogic.Clear();

            if (!JobPrepReader.TryFillHeldJobs(_heldJobs, _heldSeen, includingDropped: true))
            {
                return;
            }

            previewMeters = null;
            originYard = null;
            string? previewChip = null;
            if (JobPrepReader.TryPreview(_heldJobs, out var meters, out originYard))
            {
                previewMeters = meters;
                previewChip = PreviewEdgeDisplay.Format(previewMeters, richText: true);
            }

            string? licenseWarn = null;
            if (JobPrepReader.TryFillMissingLicenseCodes(_heldJobs, _rawLicenses, _missingLicenses))
            {
                licenseCodes = JobPrepReader.JoinCodes(_missingLicenses);
                licenseWarn = LicenseWarnDisplay.Format(_missingLicenses, richText: true);
            }

            var prep = ActiveJobHudLine.FormatPrep(licenseWarn, previewChip);
            if (string.IsNullOrEmpty(prep))
            {
                previewMeters = null;
                licenseCodes = null;
                originYard = null;
                return;
            }

            line = prep ?? string.Empty;
            visible = true;
            kind = JobBarKind.Prep;
        }

        private void NoteCancelledIfPresent(float now)
        {
            if (_backupCancelledNoted || _flash.Until >= now)
            {
                return;
            }

            try
            {
                var jobs = JobsManager.Instance?.currentJobs;
                if (jobs == null)
                {
                    return;
                }

                for (var i = 0; i < jobs.Count; i++)
                {
                    var candidate = jobs[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    string? state = null;
                    try
                    {
                        state = candidate.State.ToString();
                    }
                    catch
                    {
                        state = null;
                    }

                    if (!ActiveJobHudLine.IsCancelledState(state))
                    {
                        continue;
                    }

                    CancelledFlash.Note(ref _flash, candidate.ID, now);
                    _backupCancelledNoted = true;
                    return;
                }
            }
            catch
            {
                // fail closed
            }
        }

        private void EnsureLifecycleHooks(Job? target)
        {
            if (ReferenceEquals(_lifecycleHookJob, target))
            {
                return;
            }

            UnhookJobLifecycle();
            if (target == null)
            {
                return;
            }

            try
            {
                target.JobAbandoned += OnJobCancelled;
                target.JobExpired += OnJobCancelled;
                target.JobCompleted += OnJobCompleted;
                _lifecycleHookJob = target;
            }
            catch
            {
                _lifecycleHookJob = null;
            }
        }

        private void UnhookJobLifecycle()
        {
            if (_lifecycleHookJob == null)
            {
                return;
            }

            try
            {
                _lifecycleHookJob.JobAbandoned -= OnJobCancelled;
                _lifecycleHookJob.JobExpired -= OnJobCancelled;
                _lifecycleHookJob.JobCompleted -= OnJobCompleted;
            }
            catch
            {
                // ignore
            }

            _lifecycleHookJob = null;
        }

        private void OnJobCancelled(Job job) =>
            CancelledFlash.Note(ref _flash, job != null ? job.ID : null, Time.unscaledTime);

        private void OnJobCompleted(Job _) =>
            CancelledFlash.Clear(ref _flash);

        private void EnsureExpected(Job job, string? jobId)
        {
            var id = jobId ?? string.Empty;
            if (string.Equals(_expectedJobId, id, StringComparison.Ordinal) && _expectedLogic.Count > 0)
            {
                return;
            }

            _expectedJobId = id;
            _expectedLogic.Clear();
            _expectedIds.Clear();
            JobConsistProbe.Evaluate(job, null, _expectedLogic, _expectedIds);
        }

        private static TrainCar? SeedCar()
        {
            try
            {
                var boarded = PlayerManager.Car;
                if (boarded != null)
                {
                    return boarded;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                var last = PlayerManager.LastLoco;
                if (last != null)
                {
                    return last;
                }
            }
            catch
            {
                // fall through
            }

            return UsableTrainProbe.TryGetUsableLoco() ?? UsableTrainProbe.TryGetTargetCar();
        }

        private static bool TryGetPrimaryTakenJob(out Job? job, out int extraCount)
        {
            job = null;
            extraCount = 0;
            try
            {
                var jobs = JobsManager.Instance?.currentJobs;
                if (jobs == null || jobs.Count == 0)
                {
                    return false;
                }

                Job? best = null;
                float? bestRemaining = null;
                var live = 0;
                for (var i = 0; i < jobs.Count; i++)
                {
                    var candidate = jobs[i];
                    if (candidate == null)
                    {
                        continue;
                    }

                    string? state = null;
                    try
                    {
                        state = candidate.State.ToString();
                    }
                    catch
                    {
                        state = null;
                    }

                    if (ActiveJobHudLine.IsCancelledState(state))
                    {
                        continue;
                    }

                    live++;
                    var remaining = BonusTimeDisplay.RemainingSeconds(
                        candidate.TimeLimit,
                        SafeTimeOnJob(candidate));
                    if (best == null)
                    {
                        best = candidate;
                        bestRemaining = remaining;
                        continue;
                    }

                    if (remaining is null)
                    {
                        continue;
                    }

                    if (bestRemaining is null || remaining.Value < bestRemaining.Value)
                    {
                        best = candidate;
                        bestRemaining = remaining;
                    }
                }

                if (best == null)
                {
                    return false;
                }

                job = best;
                extraCount = Math.Max(0, live - 1);
                return true;
            }
            catch
            {
                job = null;
                extraCount = 0;
                return false;
            }
        }

        private static float? SafeTimeOnJob(Job job)
        {
            try
            {
                return job.GetTimeOnJob();
            }
            catch
            {
                return null;
            }
        }
    }
}
