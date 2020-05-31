using BattleTech;
using Harmony;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(CombatGameState), "_Init")]
    public static class CombatGameState__Init {
        public static void Postfix(CombatGameState __instance) {
            Mod.Log.Trace("CGS:_I - entered.");

            ModState.Combat = __instance;
        }
    }

    [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
    public static class CombatGameState_OnCombatGameDestroyed {
        public static bool Prepare() { return Mod.Config.Features.BiomeBreaches; }

        public static void Postfix(CombatGameState __instance) {
            Mod.Log.Trace("CGS:OCGD - entered.");

            // Reset any combat state
            ModState.Reset();
        }
    }
}
