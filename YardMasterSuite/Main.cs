using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using HarmonyLib;
using YardMasterSuite.Core; // Ensure this matches your namespace for the EventBus and GC Probe

namespace YardMasterSuite
{
    public static class Main
    {
        public static UnityModManager.ModEntry Instance { get; private set; }
        public static Harmony HarmonyInstance { get; private set; }
        
        // The invisible GameObject that keeps our non-UI monitors alive
        private static GameObject _ymsCoreObject;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            Instance = modEntry;
            modEntry.OnToggle = OnToggle;
            
            HarmonyInstance = new Harmony(modEntry.Info.Id);
            
            modEntry.Logger.Log("[YMS v2] Mod Loaded. Awaiting toggle.");
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool isActivating)
        {
            if (isActivating)
            {
                // 1. Hook Harmony Patches
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                
                // 2. Initialize Foundation (Stutter Alarm)
                _ymsCoreObject = new GameObject("YMS_Core_Lifecycle");
                Object.DontDestroyOnLoad(_ymsCoreObject);
                GcCadenceProbe.EmitLog = msg => modEntry.Logger.Log(msg);
                GcCadenceProbe.IsWorldSession = WorldSessionGate.IsActive;
                LocoStateListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ControlTelemetryListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ConsistTopologyListener.EmitLog = msg => modEntry.Logger.Log(msg);
                HeadingListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ArOverlayManager.EmitLog = msg => modEntry.Logger.Log(msg);
                YmsMailboxDrain.EmitLog = msg => modEntry.Logger.Log(msg);
                PathGraphMapper.EmitLog = msg => modEntry.Logger.Log(msg);
                PathGraphMapper.IsWorldSession = WorldSessionGate.IsActive;
                SpeedTelemetryListener.EmitLog = msg => modEntry.Logger.Log(msg);
                SpeedLimitListener.EmitLog = msg => modEntry.Logger.Log(msg);
                PostedBoardListener.EmitLog = msg => modEntry.Logger.Log(msg);
                PostedBoardListener.IsWorldSession = WorldSessionGate.IsActive;
                UsableTrainListener.EmitLog = msg => modEntry.Logger.Log(msg);
                LocalCarTelemetryListener.EmitLog = msg => modEntry.Logger.Log(msg);
                AlwaysOnHudListener.EmitLog = msg => modEntry.Logger.Log(msg);
                JobBarListener.EmitLog = msg => modEntry.Logger.Log(msg);
                OnConsistControlListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ThermalGovernorListener.EmitLog = msg => modEntry.Logger.Log(msg);
                AutoBrakeGovernorListener.EmitLog = msg => modEntry.Logger.Log(msg);
                AutoCouplerListener.EmitLog = msg => modEntry.Logger.Log(msg);
                LicenseDebugHotkey.EmitLog = msg => modEntry.Logger.Log(msg);
                TrainGadgetListener.EmitLog = msg => modEntry.Logger.Log(msg);
                BackupProximityListener.EmitLog = msg => modEntry.Logger.Log(msg);
                // HUD first so it is subscribed before publishers fire OnEnable.
                _ymsCoreObject.AddComponent<HudManager>();
                _ymsCoreObject.AddComponent<ArOverlayManager>();
                _ymsCoreObject.AddComponent<GcCadenceProbe>();
                _ymsCoreObject.AddComponent<ControlTelemetryListener>();
                // Consist before Loco: first-board T2 consist is raised from Loco OnEnable.
                _ymsCoreObject.AddComponent<ConsistTopologyListener>();
                _ymsCoreObject.AddComponent<LocoStateListener>();
                _ymsCoreObject.AddComponent<HeadingListener>();
                _ymsCoreObject.AddComponent<YmsMailboxDrain>();
                _ymsCoreObject.AddComponent<PathGraphMapper>();
                _ymsCoreObject.AddComponent<SpeedTelemetryListener>();
                _ymsCoreObject.AddComponent<PostedBoardListener>();
                _ymsCoreObject.AddComponent<SpeedLimitListener>();
                _ymsCoreObject.AddComponent<UsableTrainListener>();
                _ymsCoreObject.AddComponent<LocalCarTelemetryListener>();
                _ymsCoreObject.AddComponent<AlwaysOnHudListener>();
                _ymsCoreObject.AddComponent<JobBarListener>();
                _ymsCoreObject.AddComponent<OnConsistControlListener>();
                _ymsCoreObject.AddComponent<ThermalGovernorListener>();
                _ymsCoreObject.AddComponent<AutoBrakeGovernorListener>();
                _ymsCoreObject.AddComponent<AutoCouplerListener>();
                _ymsCoreObject.AddComponent<LicenseDebugHotkey>();
                _ymsCoreObject.AddComponent<TrainGadgetListener>();
                _ymsCoreObject.AddComponent<BackupProximityListener>();
                if (SmokeLicenseGrantGate.Enabled)
                {
                    LicenseSmokeGrant.EmitLog = msg => modEntry.Logger.Log(msg);
                    _ymsCoreObject.AddComponent<LicenseSmokeGrant>();
                }
                
                modEntry.Logger.Log("[YMS v2] Activated. GC Probe running.");
                modEntry.Logger.Log("[YMS v2] HUD running.");
                modEntry.Logger.Log("[YMS v2] Loco listener running.");
                modEntry.Logger.Log("[YMS v2] Control telemetry running.");
                modEntry.Logger.Log("[YMS v2] Consist listener running.");
                modEntry.Logger.Log("[YMS v2] Heading listener running.");
                modEntry.Logger.Log("[YMS v2] AR overlay running.");
                modEntry.Logger.Log("[YMS v2] Mailbox drain running.");
                modEntry.Logger.Log("[YMS v2] Track graph running.");
                modEntry.Logger.Log("[YMS v2] Speed telemetry running.");
                modEntry.Logger.Log("[YMS v2] Posted board index running.");
                modEntry.Logger.Log("[YMS v2] Limit display running.");
                modEntry.Logger.Log("[YMS v2] Clock running.");
                modEntry.Logger.Log("[YMS v2] Marked running.");
                modEntry.Logger.Log("[YMS v2] Station running.");
                modEntry.Logger.Log("[YMS v2] Job bar running.");
                modEntry.Logger.Log("[YMS v2] On-consist control running.");
                modEntry.Logger.Log("[YMS v2] Three-Gate write path running.");
                modEntry.Logger.Log("[YMS v2] Thermal governor running.");
                modEntry.Logger.Log("[YMS v2] Auto-brake governor running.");
                modEntry.Logger.Log("[YMS v2] Auto-coupler running.");
                modEntry.Logger.Log("[YMS v2] Train gadgets running.");
                modEntry.Logger.Log("[YMS v2] Rear/Front proximity running.");
                if (SmokeLicenseGrantGate.Enabled)
                {
                    modEntry.Logger.Log("[YMS v2] Smoke license grant armed (set SmokeLicenseGrantGate.Enabled = false to disable).");
                }
            }
            else
            {
                // 1. Unhook Harmony
                HarmonyInstance.UnpatchAll(HarmonyInstance.Id);

                // 2. Stop hitch / loco logs before destroying components
                GcCadenceProbe.FlushPending();
                ArOverlayManager.FlushPending();
                ParkMarkSession.Clear();
                PathCheckSession.Clear();
                GcCadenceProbe.EmitLog = null;
                GcCadenceProbe.IsWorldSession = null;
                LocoStateListener.EmitLog = null;
                ControlTelemetryListener.EmitLog = null;
                ConsistTopologyListener.EmitLog = null;
                HeadingListener.EmitLog = null;
                ArOverlayManager.EmitLog = null;
                YmsMailboxDrain.EmitLog = null;
                PathGraphMapper.EmitLog = null;
                PathGraphMapper.IsWorldSession = null;
                SpeedTelemetryListener.EmitLog = null;
                SpeedLimitListener.EmitLog = null;
                PostedBoardListener.EmitLog = null;
                PostedBoardListener.IsWorldSession = null;
                UsableTrainListener.EmitLog = null;
                LocalCarTelemetryListener.EmitLog = null;
                AlwaysOnHudListener.EmitLog = null;
                JobBarListener.EmitLog = null;
                OnConsistControlListener.EmitLog = null;
                ThermalGovernorListener.EmitLog = null;
                AutoBrakeGovernorListener.EmitLog = null;
                AutoCouplerListener.EmitLog = null;
                LicenseDebugHotkey.EmitLog = null;
                TrainGadgetListener.EmitLog = null;
                BackupProximityListener.EmitLog = null;
                LicenseSmokeGrant.EmitLog = null;
                
                // 3. Destroy Foundation
                if (_ymsCoreObject != null)
                {
                    Object.Destroy(_ymsCoreObject);
                    _ymsCoreObject = null;
                }
                
                // 4. STOP MEMORY LEAKS (The Unsubscribe Mandate)
                YmsEventBus.ClearAllSubscriptions();
                
                modEntry.Logger.Log("[YMS v2] Deactivated cleanly.");
            }

            return true;
        }
    }
}