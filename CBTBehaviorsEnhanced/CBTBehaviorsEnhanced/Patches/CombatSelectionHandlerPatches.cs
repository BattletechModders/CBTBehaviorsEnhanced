using BattleTech.UI;
using Harmony;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(SelectionState), "CanDeselect", MethodType.Getter)]
    public static class SelectionState_CanDeselect_Getter {
        //public static bool Prefix(SelectionState __instance, ref bool __result) {

        //    if (ModState.Combat.TurnDirector.IsInterleaved) {
        //        __result = !__instance.SelectedActor.HasBegunActivation && !__instance.SelectedActor.StoodUpThisRound;
        //    } else {
        //        // If we already have an order going, don't allow the player to select another unit
        //        __result = ModState.Combat.StackManager.IsAnyOrderActiveIncludeNonInterleaved && 
        //            (!__instance.SelectedActor.HasBegunActivation || (__instance.Orders != null && __instance.ConsumesFiring));
        //    }

        //    return false;
        //}
    }
}
