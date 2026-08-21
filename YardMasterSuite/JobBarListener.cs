using System;
using System.Collections.Generic;
using DV.Logic.Job;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Active job bar (**6.13**). Taken <c>currentJobs</c> → Job · GO/HOLD/RED · Bonus.
    /// Hidden when no taken job. Preview / license / Cancelled flash are not this ship.
    /// </summary>
    public sealed class JobBarListener : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private readonly List<Car> _expectedLogic = new List<Car>(16);
        private readonly List<int> _expectedIds = new List<int>(16);

        private ActiveJobCache _cache;
        private ActiveJobDebugSnapshot? _lastLog;
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
            _nextAt = 0f;
            Publish(force: true);
        }

        private void OnDisable()
        {
            _expectedLogic.Clear();
            _expectedIds.Clear();
            _expectedJobId = string.Empty;
            _lastLog = null;
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
                out var remaining);

            if (force || line != _lastLine)
            {
                _lastLine = line;
                YmsEventBus.RaiseJobBarChanged(new HudBarSnapshot(line, visible));
            }

            if (!ActiveJobTelemetry.Observe(visible, jobId, extra, status, remaining, ref _cache))
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
            out float? remaining)
        {
            line = string.Empty;
            visible = false;
            jobId = null;
            extra = 0;
            status = JobConsistStatus.Missing;
            remaining = null;

            if (!TryGetPrimaryTakenJob(out var job, out extra) || job == null)
            {
                _expectedJobId = string.Empty;
                _expectedLogic.Clear();
                return;
            }

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
        }

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
