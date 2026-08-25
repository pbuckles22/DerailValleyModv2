using System.Collections.Generic;
using DV.Interaction.Inputs;
using DV.KeyboardInput;
using HarmonyLib;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// Cab Incremental: swallow GetAnyDirButtonDown chatter so levers
    /// cannot walk themselves after the player cuts them.
    /// </summary>
    [HarmonyPatch(typeof(NotchedPortIncrementalInput), nameof(NotchedPortIncrementalInput.Tick))]
    internal static class NotchedPortIncrementalInputPatch
    {
        private static readonly Dictionary<int, bool> Held = new Dictionary<int, bool>(8);

        private static bool Prefix(NotchedPortIncrementalInput __instance)
        {
            try
            {
                var player = InputManager.NewPlayer;
                if (player == null)
                {
                    return true;
                }

                var actionId = __instance.applyAction.id;
                if (actionId < 0)
                {
                    return true;
                }

                var held = player.GetAnyDirButton(actionId);
                var down = player.GetAnyDirButtonDown(actionId);
                var key = __instance.GetInstanceID();
                Held.TryGetValue(key, out var wasHeld);
                var apply = IncrementalChatterGate.ShouldApplyNotch(down, wasHeld);
                if (held)
                {
                    Held[key] = true;
                }
                else
                {
                    Held.Remove(key);
                }

                if (!down)
                {
                    return true;
                }

                return apply;
            }
            catch
            {
                return true;
            }
        }
    }
}
