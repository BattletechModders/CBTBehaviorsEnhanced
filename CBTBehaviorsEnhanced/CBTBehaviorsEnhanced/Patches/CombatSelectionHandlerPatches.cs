using BattleTech.UI;

namespace CBTBehaviorsEnhanced.Patches
{

    [HarmonyPatch(typeof(CombatSelectionHandler), "RemoveCompletedItems")]
    public static class CombatSelectionHandler_RemoveCompletedItems
    {
        public static void Prefix(ref bool __runOriginal, CombatSelectionHandler __instance, ref bool __state)
        {
            if (!__runOriginal) return;

            __state = false;

            if (__instance == null || __instance.ActiveState == null) { return; }
            if (__instance.SelectedActor != null && __instance.SelectedActor.IsDead) { return; }

            Mod.Log.Trace?.Write($"CSH:RCI");

            AbstractActor selectedActor = __instance.SelectedActor;
            Traverse combatT = Traverse.Create(__instance).Property("Combat");
            CombatGameState combat = combatT.GetValue<CombatGameState>();

            if (__instance.ActiveState.IsComplete && !combat.TurnDirector.IsInterleaved)
            {
                __state = true;
            }
        }

        public static void Postfix(CombatSelectionHandler __instance, ref bool __state)
        {
            if (__state)
            {
                // Because we override movement states to ConsumesFiring=true during non-interleaved mode, we have to force an auto-select
                Mod.Log.Debug?.Write("Invoking auto select.");
                __instance.AutoSelectActor();
            }
        }
    }

}
