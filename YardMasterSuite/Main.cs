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
                _ymsCoreObject.AddComponent<GcCadenceProbe>();
                
                modEntry.Logger.Log("[YMS v2] Activated. GC Probe running.");
            }
            else
            {
                // 1. Unhook Harmony
                HarmonyInstance.UnpatchAll(HarmonyInstance.Id);

                // 2. Stop hitch logs before destroying the probe
                GcCadenceProbe.EmitLog = null;
                
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