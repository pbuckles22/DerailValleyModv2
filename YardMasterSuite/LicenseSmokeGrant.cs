using System;
using DV;
using DV.Logic.Job;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// 6.7 older-save helper: grant all obtainable licenses once per world load
    /// when <see cref="SmokeLicenseGrantGate.Enabled"/> is true.
    /// Disable by setting that field to false and redeploying.
    /// </summary>
    public sealed class LicenseSmokeGrant : MonoBehaviour
    {
        internal static Action<string>? EmitLog;

        private bool _done;

        private void LateUpdate()
        {
            if (_done || PlayerManager.PlayerTransform == null)
            {
                return;
            }

            _done = true;
            if (!SmokeLicenseGrantGate.Enabled)
            {
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatDisabled());
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

                var acquired = AcquireAll(lm);
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatGranted(acquired));
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke(SmokeLicenseGrantGate.FormatFail(ex.GetType().Name));
            }
        }

        private static int AcquireAll(LicenseManager lm)
        {
            var acquired = 0;
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
                        acquired++;
                    }
                }
                catch
                {
                    // skip unobtainable / blocked
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
                        acquired++;
                    }
                }
                catch
                {
                    // skip unobtainable / blocked
                }
            }

            return acquired;
        }
    }
}
