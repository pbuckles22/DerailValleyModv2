using System;
using System.Collections.Generic;
using DV;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// F8: grant all obtainable licenses, next press restores the snapshot
    /// taken before the grant (career load / buy stays Real until toggled).
    /// F11 is the game stats overlay — do not bind license debug there.
    /// </summary>
    public sealed class LicenseDebugHotkey : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private LicenseDebugMode _mode = LicenseDebugMode.Real;
        private List<GeneralLicenseType_v2>? _snapGeneral;
        private List<JobLicenseType_v2>? _snapJob;

        private void OnDisable()
        {
            if (_mode == LicenseDebugMode.AllGranted)
            {
                try
                {
                    var lm = LicenseManager.Instance;
                    if (lm != null)
                    {
                        TryRestore(lm);
                    }
                }
                catch
                {
                    // fail closed
                }

                _mode = LicenseDebugMode.Real;
            }

            _snapGeneral = null;
            _snapJob = null;
        }

        private void Update()
        {
            if (!Enum.TryParse(LicenseDebugToggle.HotkeyName, ignoreCase: true, out KeyCode debugKey)
                || !Input.GetKeyDown(debugKey))
            {
                return;
            }

            if (!HudWorldSession.IsActive(PlayerManager.PlayerTransform != null)
                || !ScreenOverlayGate.WorldReady()
                || ScreenOverlayGate.IsBlocking())
            {
                return;
            }

            try
            {
                var lm = LicenseManager.Instance;
                if (lm == null)
                {
                    EmitLog?.Invoke(SmokeLicenseGrantGate.FormatFail("no LicenseManager"));
                    return;
                }

                var next = LicenseDebugToggle.Next(_mode);
                if (next == LicenseDebugMode.AllGranted)
                {
                    if (!TrySnapshot(lm) || !TryAcquireAll(lm))
                    {
                        _snapGeneral = null;
                        _snapJob = null;
                        return;
                    }

                    _mode = LicenseDebugMode.AllGranted;
                }
                else
                {
                    if (!TryRestore(lm))
                    {
                        return;
                    }

                    _mode = LicenseDebugMode.Real;
                    _snapGeneral = null;
                    _snapJob = null;
                }

                LocoRadarProbe.MarkWorldEnter();
                EmitLog?.Invoke(LicenseDebugToggle.FormatLog(_mode));
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatFail(ex.GetType().Name));
            }
        }

        private bool TrySnapshot(LicenseManager lm)
        {
            try
            {
                var general = lm.GetGeneralAcquiredLicenses();
                var jobs = lm.GetAcquiredJobLicenses();
                _snapGeneral = general != null
                    ? new List<GeneralLicenseType_v2>(general)
                    : new List<GeneralLicenseType_v2>();
                _snapJob = jobs != null
                    ? new List<JobLicenseType_v2>(jobs)
                    : new List<JobLicenseType_v2>();
                return true;
            }
            catch
            {
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatFail("snapshot"));
                return false;
            }
        }

        private bool TryAcquireAll(LicenseManager lm)
        {
            try
            {
                foreach (GeneralLicenseType t in Enum.GetValues(typeof(GeneralLicenseType)))
                {
                    if (t == GeneralLicenseType.NotSet)
                    {
                        continue;
                    }

                    var v2 = TransitionHelpers.ToV2(t);
                    if (v2 == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (!lm.IsGeneralLicenseObtainable(v2) && !lm.IsGeneralLicenseAcquired(v2))
                        {
                            continue;
                        }

                        if (!lm.IsGeneralLicenseAcquired(v2))
                        {
                            lm.AcquireGeneralLicense(v2);
                        }
                    }
                    catch
                    {
                        // skip blocked
                    }
                }

                foreach (JobLicenses t in Enum.GetValues(typeof(JobLicenses)))
                {
                    var v2 = TransitionHelpers.ToV2(t);
                    if (v2 == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (!lm.IsJobLicenseObtainable(v2) && !lm.IsJobLicenseAcquired(v2))
                        {
                            continue;
                        }

                        if (!lm.IsJobLicenseAcquired(v2))
                        {
                            lm.AcquireJobLicense(v2);
                        }
                    }
                    catch
                    {
                        // skip blocked
                    }
                }

                return true;
            }
            catch
            {
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatFail("acquire"));
                return false;
            }
        }

        private bool TryRestore(LicenseManager lm)
        {
            try
            {
                var keepGeneral = new HashSet<GeneralLicenseType_v2>(
                    _snapGeneral ?? (IEnumerable<GeneralLicenseType_v2>)Array.Empty<GeneralLicenseType_v2>());
                var keepJob = new HashSet<JobLicenseType_v2>(
                    _snapJob ?? (IEnumerable<JobLicenseType_v2>)Array.Empty<JobLicenseType_v2>());

                var currentGeneral = lm.GetGeneralAcquiredLicenses();
                if (currentGeneral != null)
                {
                    var extraGeneral = new List<GeneralLicenseType_v2>();
                    foreach (var lic in currentGeneral)
                    {
                        if (lic != null && !keepGeneral.Contains(lic))
                        {
                            extraGeneral.Add(lic);
                        }
                    }

                    for (var i = 0; i < extraGeneral.Count; i++)
                    {
                        lm.RemoveGeneralLicense(extraGeneral[i]);
                    }
                }

                var currentJob = lm.GetAcquiredJobLicenses();
                if (currentJob != null)
                {
                    var extra = new List<JobLicenseType_v2>();
                    foreach (var job in currentJob)
                    {
                        if (job != null && !keepJob.Contains(job))
                        {
                            extra.Add(job);
                        }
                    }

                    if (extra.Count > 0)
                    {
                        lm.RemoveJobLicense(extra);
                    }
                }

                foreach (var lic in keepGeneral)
                {
                    if (lic != null && !lm.IsGeneralLicenseAcquired(lic))
                    {
                        lm.AcquireGeneralLicense(lic);
                    }
                }

                foreach (var job in keepJob)
                {
                    if (job != null && !lm.IsJobLicenseAcquired(job))
                    {
                        lm.AcquireJobLicense(job);
                    }
                }

                return true;
            }
            catch
            {
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatFail("restore"));
                return false;
            }
        }
    }
}
