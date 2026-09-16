using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Cab auto warehouse: native <c>WarehouseMachineController.ActivateExternally</c>
    /// (same DelayedLoadUnload as the lever, no walk to the kiosk).
    /// </summary>
    internal static class WarehouseLoadGovernor
    {
        internal static System.Action<string>? EmitLog;

        private static string? _cachedDest;
        private static WarehouseMachineController? _cached;

        internal static void Reset()
        {
            _cachedDest = null;
            _cached = null;
        }

        internal static bool Tick(string? destTrackId, bool unload)
        {
            if (!TryFind(destTrackId, out var ctrl) || ctrl == null)
            {
                return WarehouseLoadSession.Locked;
            }

            var machine = ctrl.warehouseMachine;
            var ongoing = false;
            var stillHasWork = false;
            try
            {
                ongoing = ctrl.LoadOrUnloadOngoing;
            }
            catch
            {
                ongoing = false;
            }

            try
            {
                if (machine != null)
                {
                    stillHasWork = unload
                        ? machine.AnyTrainToUnloadPresentOnTrack()
                        : machine.AnyTrainToLoadPresentOnTrack();
                }
            }
            catch
            {
                stillHasWork = false;
            }

            WarehouseLoadSession.Observe(ongoing, stillHasWork);
            return WarehouseLoadSession.Locked;
        }

        internal static bool CarsReady(string? destTrackId, bool unload)
        {
            if (!TryFind(destTrackId, out var ctrl) || ctrl == null || ctrl.warehouseMachine == null)
            {
                return false;
            }

            try
            {
                return unload
                    ? ctrl.warehouseMachine.AnyTrainToUnloadPresentOnTrack()
                    : ctrl.warehouseMachine.AnyTrainToLoadPresentOnTrack();
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryStart(string? destTrackId)
        {
            if (!TryFind(destTrackId, out var ctrl) || ctrl == null)
            {
                return false;
            }

            try
            {
                WarehouseLoadSession.Begin();
                ctrl.ActivateExternally();
                EmitLog?.Invoke(SwitchListRunnerTelemetry.YardChainWarehouseLoad);
                return true;
            }
            catch
            {
                WarehouseLoadSession.Clear();
                return false;
            }
        }

        private static bool TryFind(string? destTrackId, out WarehouseMachineController? ctrl)
        {
            ctrl = null;
            var dest = destTrackId?.Trim();
            if (string.IsNullOrEmpty(dest))
            {
                return false;
            }

            if (_cached != null
                && string.Equals(_cachedDest, dest, System.StringComparison.OrdinalIgnoreCase)
                && _cached)
            {
                ctrl = _cached;
                return true;
            }

            WarehouseMachineController[]? all;
            try
            {
                all = Object.FindObjectsOfType<WarehouseMachineController>();
            }
            catch
            {
                return false;
            }

            if (all == null || all.Length == 0)
            {
                return false;
            }

            for (var i = 0; i < all.Length; i++)
            {
                var candidate = all[i];
                if (candidate == null)
                {
                    continue;
                }

                if (!MatchesDest(candidate, dest!))
                {
                    continue;
                }

                _cached = candidate;
                _cachedDest = dest;
                ctrl = candidate;
                return true;
            }

            return false;
        }

        private static bool MatchesDest(WarehouseMachineController candidate, string dest)
        {
            try
            {
                var name = candidate.warehouseTrackName?.Trim();
                if (!string.IsNullOrEmpty(name)
                    && string.Equals(name, dest, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                var railId = LogicTrackKey.FromRail(candidate.warehouseTrack);
                if (!string.IsNullOrEmpty(railId)
                    && string.Equals(railId, dest, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                // fall through
            }

            try
            {
                var logicId = LogicTrackKey.FromLogic(candidate.warehouseMachine?.WarehouseTrack);
                if (!string.IsNullOrEmpty(logicId)
                    && string.Equals(logicId, dest, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
