using BattleTech;
using BattleTech.UI;
using Harmony;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(CombatSelectionHandler), "RemoveCompletedItems")]
    public static class CombatSelectionHandler_RemoveCompletedItems {
        public static void Prefix(CombatSelectionHandler __instance, ref bool __state) {
            __state = false;

            if (__instance == null || __instance.ActiveState == null) { return; }
            if (__instance.SelectedActor != null && __instance.SelectedActor.IsDead) { return; }

            Mod.Log.Trace($"CSH:RCI");

            AbstractActor selectedActor = __instance.SelectedActor;
            Traverse combatT = Traverse.Create(__instance).Property("Combat");
            CombatGameState combat = combatT.GetValue<CombatGameState>();

            //Mod.Log.Debug($" == RCI - selectedActor: {CombatantUtils.Label(selectedActor)}" +
            //    $"  isInterleaved: {combat.TurnDirector.IsInterleaved}  isComplete: {__instance.ActiveState.IsComplete}");

            if (__instance.ActiveState.IsComplete && !combat.TurnDirector.IsInterleaved) {
                __state = true;
            }
        }

        public static void Postfix(CombatSelectionHandler __instance, ref bool __state) {
            if (__state) {
                // Because we override movement states to ConsumesFiring=true during non-interleaved mode, we have to force an auto-select
                Mod.Log.Debug("Invoking auto select.");
                __instance.AutoSelectActor();
            }
        }
    }

}
