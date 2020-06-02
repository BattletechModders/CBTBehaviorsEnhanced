using BattleTech;
using BattleTech.UI;
using Harmony;
using System;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(CombatHUD), "Init")]
    [HarmonyPatch(new Type[] {  typeof(CombatGameState) })]
    static class CombatHUD_Init
    {
        static void Postfix(CombatHUD __instance)
        {
            Mod.Log.Debug("Capturing reference to CombatHUD");
            ModState.CombatHUD = __instance;
        }
    }
}
