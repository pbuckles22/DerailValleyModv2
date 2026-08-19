using System;
using System.Collections.Generic;
using System.Reflection;
using DV.CabControls;
using DV.HUD;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.Simulation.Fuses;
using LocoSim.Definitions;
using LocoSim.Implementations;
using LocoSim.Resources;
using UnityEngine;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Read-only loco sim ports for **6.6** Load / Motors / Fluids.
    /// Adapted from v1 <c>TelemetryReader</c> (no debug overrides). Fuse / MU
    /// lookups are cached per loco so 10 Hz ticks do not alloc.
    /// </summary>
    internal static class LocoSimReader
    {
        private static readonly Dictionary<Type, LoadFieldMap> LoadFieldCache = new Dictionary<Type, LoadFieldMap>();
        private static MotorSetFieldMap? _motorSetFields;

        private static TrainCar? _cachedLoco;
        private static SimulationFlow? _cachedFlow;
        private static MultipleUnitStateObserver? _cachedMu;
        private static Fuse? _cachedTmFuse;
        private static ControlImplBase? _cachedTmFuseControl;
        private static OverridableBaseControl? _cachedTmFuseOverridable;
        private static bool _fuseResolved;

        internal static void ReadPower(
            TrainCar loco,
            out float? fuelPercent,
            out float? oilPercent,
            out float? loadPercent,
            out MotorStatus? motors)
        {
            fuelPercent = null;
            oilPercent = null;
            loadPercent = null;
            motors = null;
            if (loco == null)
            {
                return;
            }

            try
            {
                BindLoco(loco);
                var flow = _cachedFlow;
                if (flow?.OrderedSimComps == null)
                {
                    return;
                }

                fuelPercent = ReadFluidPercent(flow, ResourceContainerType.FUEL);
                oilPercent = ReadFluidPercent(flow, ResourceContainerType.OIL);
                loadPercent = ReadLoadPercent(flow);
                motors = ReadMotorStatus(flow, TryGetCabTempBand(), TryGetTmFuseOn());
            }
            catch
            {
                // Sim graph can be mid-rebuild while boarding.
            }
        }

        private static void BindLoco(TrainCar loco)
        {
            if (ReferenceEquals(_cachedLoco, loco) && _cachedFlow != null)
            {
                return;
            }

            _cachedLoco = loco;
            _cachedFlow = null;
            _cachedMu = null;
            _cachedTmFuse = null;
            _cachedTmFuseControl = null;
            _cachedTmFuseOverridable = null;
            _fuseResolved = false;

            var sim = loco.SimController;
            if (sim != null)
            {
                _cachedFlow = sim.simFlow ?? sim.SimulationFlow;
            }

            try
            {
                _cachedMu = loco.GetComponent<MultipleUnitStateObserver>();
            }
            catch
            {
                _cachedMu = null;
            }
        }

        private static float? ReadFluidPercent(SimulationFlow flow, ResourceContainerType resourceType)
        {
            foreach (var comp in flow.OrderedSimComps)
            {
                if (comp is not ResourceContainer container || container.resourceType != resourceType)
                {
                    continue;
                }

                var normalized = SafePortValue(container.normalizedReadOutPort);
                if (normalized != null)
                {
                    return FluidDisplay.PercentFromNormalized(normalized);
                }

                var fromAmount = FluidDisplay.PercentFromAmount(
                    SafePortValue(container.amountReadOut),
                    SafePortValue(container.capacityReadOutPort) ?? SafeFloat(container.capacity));
                if (fromAmount != null)
                {
                    return fromAmount;
                }
            }

            return null;
        }

        private static float? ReadLoadPercent(SimulationFlow flow)
        {
            foreach (var comp in flow.OrderedSimComps)
            {
                if (comp == null)
                {
                    continue;
                }

                var fromComp = ReadLoadPercentFromComponent(comp);
                if (fromComp != null)
                {
                    return fromComp;
                }
            }

            return null;
        }

        private static MotorStatus? ReadMotorStatus(
            SimulationFlow flow,
            MotorCabTempBand? cabTempBand,
            bool? tmFuseOn)
        {
            foreach (var comp in flow.OrderedSimComps)
            {
                if (comp == null)
                {
                    continue;
                }

                var fromComp = ReadMotorStatusFromComponent(comp, cabTempBand, tmFuseOn);
                if (fromComp != null)
                {
                    return fromComp;
                }
            }

            if (tmFuseOn == false)
            {
                return MotorStatus.Dead;
            }

            return null;
        }

        private static MotorStatus? ReadMotorStatusFromComponent(
            SimComponent comp,
            MotorCabTempBand? cabTempBand,
            bool? tmFuseOn)
        {
            if (comp is TractionMotor tm)
            {
                return MotorDisplay.StatusFromSignals(
                    SafePortValue(tm.tmsStateReadOut),
                    SafePortReferenceValue(tm.temperature),
                    SafeFloat(tm.overheatingTemperatureThreshold),
                    SafePortValue(tm.workingTractionMotorsReadOut),
                    tm.numberOfTractionMotors,
                    cabTempBand,
                    tmFuseOn);
            }

            if (comp is TractionMotorSet set)
            {
                return ReadMotorStatusFromMotorSet(set, cabTempBand, tmFuseOn);
            }

            return null;
        }

        private static MotorStatus? ReadMotorStatusFromMotorSet(
            TractionMotorSet set,
            MotorCabTempBand? cabTempBand,
            bool? tmFuseOn)
        {
            var map = GetMotorSetFieldMap();
            if (map is null)
            {
                return null;
            }

            return MotorDisplay.StatusFromSignals(
                ReadPortField(set, map.Value.TmsState),
                ReadPortReferenceField(set, map.Value.Temp),
                ReadFloatField(set, map.Value.OverheatThreshold),
                ReadPortField(set, map.Value.Working),
                ReadIntAsFloatField(set, map.Value.NumberOfMotors),
                cabTempBand,
                tmFuseOn);
        }

        private static MotorSetFieldMap? GetMotorSetFieldMap()
        {
            if (_motorSetFields is not null)
            {
                return _motorSetFields;
            }

            var type = typeof(TractionMotorSet);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var map = new MotorSetFieldMap(
                type.GetField("tmsStateReadOut", flags),
                type.GetField("tmTempReader", flags),
                type.GetField("overheatingTemperatureThreshold", flags),
                type.GetField("workingTractionMotorsReadOut", flags),
                type.GetField("numberOfTractionMotors", flags));
            if (!map.HasRequired)
            {
                return null;
            }

            _motorSetFields = map;
            return map;
        }

        private static float? ReadLoadPercentFromComponent(SimComponent comp)
        {
            if (comp is TractionMotor tm)
            {
                var normalized = SafePortValue(tm.ampsNormalizedReadOut);
                if (normalized != null)
                {
                    return LoadDisplay.PercentFromNormalized(normalized);
                }

                var fromAmps = LoadDisplay.PercentFromAmps(
                    SafePortValue(tm.ampsReadOut),
                    SafePortValue(tm.maxAmpsReadOut));
                if (fromAmps != null)
                {
                    return fromAmps;
                }

                return LoadDisplay.PercentFromNormalized(SafePortValue(tm.loadOnGeneratorReadOut));
            }

            var map = GetOrBuildLoadFieldMap(comp.GetType());
            if (!map.HasAny)
            {
                return null;
            }

            float? ampsNormalized = ReadPortField(comp, map.AmpsNormalized);
            float? amps = ReadPortField(comp, map.Amps);
            float? maxAmps = ReadPortField(comp, map.MaxAmps);
            float? ampsPerTm = ReadPortField(comp, map.AmpsPerTm);
            float? maxPerTm = ReadPortField(comp, map.MaxPerTm);
            float? totalAmps = ReadPortField(comp, map.TotalAmps) ?? ReadPortReferenceField(comp, map.TotalAmpsRef);
            float? working = ReadPortField(comp, map.Working);
            float? loadOnGenerator = ReadPortField(comp, map.LoadOnGenerator);
            float? maxAmpsConst = ReadFloatField(comp, map.MaxAmpsConst);

            if (ampsNormalized != null)
            {
                return LoadDisplay.PercentFromNormalized(ampsNormalized);
            }

            var perTm = LoadDisplay.PercentFromAmps(ampsPerTm, maxPerTm);
            if (perTm != null)
            {
                return perTm;
            }

            var direct = LoadDisplay.PercentFromAmps(amps ?? totalAmps, maxAmps ?? maxAmpsConst);
            if (direct != null)
            {
                return direct;
            }

            if (totalAmps != null && maxPerTm != null && working is > 0f)
            {
                return LoadDisplay.PercentFromAmps(totalAmps, maxPerTm.Value * working.Value);
            }

            return LoadDisplay.PercentFromNormalized(loadOnGenerator);
        }

        private static LoadFieldMap GetOrBuildLoadFieldMap(Type type)
        {
            if (LoadFieldCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            FieldInfo? ampsNormalized = null;
            FieldInfo? amps = null;
            FieldInfo? maxAmps = null;
            FieldInfo? ampsPerTm = null;
            FieldInfo? maxPerTm = null;
            FieldInfo? totalAmps = null;
            FieldInfo? totalAmpsRef = null;
            FieldInfo? working = null;
            FieldInfo? loadOnGenerator = null;
            FieldInfo? maxAmpsConst = null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var field in type.GetFields(flags))
            {
                var name = field.Name;
                if (field.FieldType == typeof(Port))
                {
                    if (NameHas(name, "ampsNormalized"))
                    {
                        ampsNormalized = field;
                    }
                    else if (NameHas(name, "loadOnGenerator"))
                    {
                        loadOnGenerator = field;
                    }
                    else if (NameHas(name, "maxAmpsPerTM") || NameHas(name, "maxAmpsPerTm"))
                    {
                        maxPerTm = field;
                    }
                    else if (NameHas(name, "ampsPerTM") || NameHas(name, "ampsPerTm"))
                    {
                        ampsPerTm = field;
                    }
                    else if (NameHas(name, "maxAmpsReadOut") || name.Equals("maxAmps", StringComparison.Ordinal))
                    {
                        maxAmps = field;
                    }
                    else if (name.Equals("ampsReadOut", StringComparison.Ordinal))
                    {
                        amps = field;
                    }
                    else if (NameHas(name, "totalAmps"))
                    {
                        totalAmps = field;
                    }
                    else if (NameHas(name, "workingTractionMotors"))
                    {
                        working = field;
                    }
                }
                else if (field.FieldType == typeof(PortReference) && NameHas(name, "totalAmps"))
                {
                    totalAmpsRef = field;
                }
                else if (field.FieldType == typeof(float) && name.Equals("maxAmps", StringComparison.Ordinal))
                {
                    maxAmpsConst = field;
                }
            }

            var map = new LoadFieldMap(
                ampsNormalized,
                amps,
                maxAmps,
                ampsPerTm,
                maxPerTm,
                totalAmps,
                totalAmpsRef,
                working,
                loadOnGenerator,
                maxAmpsConst);
            LoadFieldCache[type] = map;
            return map;
        }

        private static MotorCabTempBand? TryGetCabTempBand()
        {
            if (_cachedMu == null)
            {
                return null;
            }

            try
            {
                return (MotorCabTempBand)(int)_cachedMu.MUChainTemperatureState;
            }
            catch
            {
                return null;
            }
        }

        private static bool? TryGetTmFuseOn()
        {
            if (_fuseResolved)
            {
                return ReadCachedFuse();
            }

            _fuseResolved = true;
            var loco = _cachedLoco;
            var flow = _cachedFlow;
            if (loco == null)
            {
                return null;
            }

            try
            {
                var deadTm = loco.GetComponent<DeadTractionMotorsController>()
                    ?? loco.GetComponentInChildren<DeadTractionMotorsController>(true);
                if (deadTm != null
                    && !string.IsNullOrEmpty(deadTm.tmFuseId)
                    && flow != null
                    && flow.TryGetFuse(deadTm.tmFuseId, out var tmFuse, canBeNull: true)
                    && tmFuse != null)
                {
                    _cachedTmFuse = tmFuse;
                    return tmFuse.State;
                }

                LocoFuseBoxReference? box = null;
                try
                {
                    box = loco.GetComponent<LocoFuseBoxReference>()
                        ?? loco.GetComponentInChildren<LocoFuseBoxReference>(true)
                        ?? loco.loadedInterior?.GetComponentInChildren<LocoFuseBoxReference>(true)
                        ?? loco.loadedExternalInteractables?.GetComponent<LocoFuseBoxReference>()
                        ?? loco.loadedExternalInteractables?.GetComponentInChildren<LocoFuseBoxReference>(true);
                }
                catch
                {
                    box = null;
                }

                if (box?.tractionMotorFuse != null
                    && box.tractionMotorFuse.TryGetComponent<ControlImplBase>(out var boxCtrl))
                {
                    _cachedTmFuseControl = boxCtrl;
                    return boxCtrl.Value > 0.5f;
                }

                var icm = loco.GetComponent<InteriorControlsManager>()
                    ?? loco.GetComponentInChildren<InteriorControlsManager>(true);
                if (icm != null
                    && icm.TryGetControl(
                        InteriorControlsManager.ControlType.TractionMotorFuse,
                        out var reference))
                {
                    if (reference.controlImplBase != null)
                    {
                        _cachedTmFuseControl = reference.controlImplBase;
                        return _cachedTmFuseControl.Value > 0.5f;
                    }

                    if (reference.overridableBaseControl != null)
                    {
                        _cachedTmFuseOverridable = reference.overridableBaseControl;
                        return _cachedTmFuseOverridable.Value > 0.5f;
                    }
                }

                if (flow != null)
                {
                    var feeders = loco.GetComponentsInChildren<InteractableFuseFeeder>(true);
                    for (var i = 0; i < feeders.Length; i++)
                    {
                        var id = feeders[i]?.fuseId?.Trim();
                        if (string.IsNullOrEmpty(id))
                        {
                            continue;
                        }

                        var fuseId = id!;
                        if (fuseId.IndexOf("tm", StringComparison.OrdinalIgnoreCase) < 0
                            && fuseId.IndexOf("traction", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        if (flow.TryGetFuse(fuseId, out var fuse, canBeNull: true) && fuse != null)
                        {
                            _cachedTmFuse = fuse;
                            return fuse.State;
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        private static bool? ReadCachedFuse()
        {
            try
            {
                if (_cachedTmFuse != null)
                {
                    return _cachedTmFuse.State;
                }

                if (_cachedTmFuseControl != null)
                {
                    return _cachedTmFuseControl.Value > 0.5f;
                }

                if (_cachedTmFuseOverridable != null)
                {
                    return _cachedTmFuseOverridable.Value > 0.5f;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static float? ReadPortField(SimComponent comp, FieldInfo? field)
        {
            if (field == null)
            {
                return null;
            }

            try
            {
                return SafePortValue(field.GetValue(comp) as Port);
            }
            catch
            {
                return null;
            }
        }

        private static float? ReadPortReferenceField(SimComponent comp, FieldInfo? field)
        {
            if (field == null)
            {
                return null;
            }

            try
            {
                return SafePortReferenceValue(field.GetValue(comp) as PortReference);
            }
            catch
            {
                return null;
            }
        }

        private static float? ReadFloatField(SimComponent comp, FieldInfo? field)
        {
            if (field == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(comp) is float f ? SafeFloat(f) : null;
            }
            catch
            {
                return null;
            }
        }

        private static float? ReadIntAsFloatField(SimComponent comp, FieldInfo? field)
        {
            if (field == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(comp) is int n ? n : (float?)null;
            }
            catch
            {
                return null;
            }
        }

        private static float? SafePortValue(Port? port)
        {
            if (port == null)
            {
                return null;
            }

            try
            {
                return SafeFloat(port.Value);
            }
            catch
            {
                return null;
            }
        }

        private static float? SafePortReferenceValue(PortReference? pref)
        {
            if (pref == null || !pref.IsConnected)
            {
                return null;
            }

            try
            {
                return SafeFloat(pref.Value);
            }
            catch
            {
                return null;
            }
        }

        private static float? SafeFloat(float value) =>
            float.IsNaN(value) || float.IsInfinity(value) ? null : value;

        private static bool NameHas(string name, string token) =>
            name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;

        private readonly struct LoadFieldMap
        {
            public LoadFieldMap(
                FieldInfo? ampsNormalized,
                FieldInfo? amps,
                FieldInfo? maxAmps,
                FieldInfo? ampsPerTm,
                FieldInfo? maxPerTm,
                FieldInfo? totalAmps,
                FieldInfo? totalAmpsRef,
                FieldInfo? working,
                FieldInfo? loadOnGenerator,
                FieldInfo? maxAmpsConst)
            {
                AmpsNormalized = ampsNormalized;
                Amps = amps;
                MaxAmps = maxAmps;
                AmpsPerTm = ampsPerTm;
                MaxPerTm = maxPerTm;
                TotalAmps = totalAmps;
                TotalAmpsRef = totalAmpsRef;
                Working = working;
                LoadOnGenerator = loadOnGenerator;
                MaxAmpsConst = maxAmpsConst;
            }

            public FieldInfo? AmpsNormalized { get; }
            public FieldInfo? Amps { get; }
            public FieldInfo? MaxAmps { get; }
            public FieldInfo? AmpsPerTm { get; }
            public FieldInfo? MaxPerTm { get; }
            public FieldInfo? TotalAmps { get; }
            public FieldInfo? TotalAmpsRef { get; }
            public FieldInfo? Working { get; }
            public FieldInfo? LoadOnGenerator { get; }
            public FieldInfo? MaxAmpsConst { get; }

            public bool HasAny =>
                AmpsNormalized != null
                || Amps != null
                || MaxAmps != null
                || AmpsPerTm != null
                || MaxPerTm != null
                || TotalAmps != null
                || TotalAmpsRef != null
                || Working != null
                || LoadOnGenerator != null
                || MaxAmpsConst != null;
        }

        private readonly struct MotorSetFieldMap
        {
            public MotorSetFieldMap(
                FieldInfo? tmsState,
                FieldInfo? temp,
                FieldInfo? overheatThreshold,
                FieldInfo? working,
                FieldInfo? numberOfMotors)
            {
                TmsState = tmsState;
                Temp = temp;
                OverheatThreshold = overheatThreshold;
                Working = working;
                NumberOfMotors = numberOfMotors;
            }

            public FieldInfo? TmsState { get; }
            public FieldInfo? Temp { get; }
            public FieldInfo? OverheatThreshold { get; }
            public FieldInfo? Working { get; }
            public FieldInfo? NumberOfMotors { get; }

            public bool HasRequired =>
                TmsState != null && Temp != null && OverheatThreshold != null;
        }
    }
}
