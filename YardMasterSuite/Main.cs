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
                GcCadenceProbe.IsWorldSession = () =>
                    HudWorldSession.IsActive(PlayerManager.PlayerTransform != null);
                LocoStateListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ControlTelemetryListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ConsistTopologyListener.EmitLog = msg => modEntry.Logger.Log(msg);
                HeadingListener.EmitLog = msg => modEntry.Logger.Log(msg);
                ArOverlayManager.EmitLog = msg => modEntry.Logger.Log(msg);
                // HUD first so it is subscribed before publishers fire OnEnable.
                _ymsCoreObject.AddComponent<HudManager>();
                _ymsCoreObject.AddComponent<ArOverlayManager>();
                _ymsCoreObject.AddComponent<GcCadenceProbe>();
                _ymsCoreObject.AddComponent<ControlTelemetryListener>();
                // Consist before Loco: first-board T2 consist is raised from Loco OnEnable.
                _ymsCoreObject.AddComponent<ConsistTopologyListener>();
                _ymsCoreObject.AddComponent<LocoStateListener>();
                _ymsCoreObject.AddComponent<HeadingListener>();
                
                modEntry.Logger.Log("[YMS v2] Activated. GC Probe running.");
                modEntry.Logger.Log("[YMS v2] HUD running.");
                modEntry.Logger.Log("[YMS v2] Loco listener running.");
                modEntry.Logger.Log("[YMS v2] Control telemetry running.");
                modEntry.Logger.Log("[YMS v2] Consist listener running.");
                modEntry.Logger.Log("[YMS v2] Heading listener running.");
                modEntry.Logger.Log("[YMS v2] AR overlay running.");
            }
            else
            {
                // 1. Unhook Harmony
                HarmonyInstance.UnpatchAll(HarmonyInstance.Id);

                // 2. Stop hitch / loco logs before destroying components
                GcCadenceProbe.FlushPending();
                ArOverlayManager.FlushPending();
                GcCadenceProbe.EmitLog = null;
                GcCadenceProbe.IsWorldSession = null;
                LocoStateListener.EmitLog = null;
                ControlTelemetryListener.EmitLog = null;
                ConsistTopologyListener.EmitLog = null;
                HeadingListener.EmitLog = null;
                ArOverlayManager.EmitLog = null;
                
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