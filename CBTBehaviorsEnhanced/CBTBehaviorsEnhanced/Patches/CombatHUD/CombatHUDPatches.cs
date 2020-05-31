using BattleTech.UI;
using Harmony;

namespace CBTBehaviorsEnhanced.Patches
{
    [HarmonyPatch(typeof(CombatHUD), "Init")]
    static class CombatHUD_Init
    {
        static void Postfix(CombatHUD __instance)
        {
            Mod.Log.Debug("Capturing reference to CombatHUD");
            ModState.CombatHUD = __instance;
        }
    }
}
