using BattleTech;
using Harmony;
using System.Collections.Generic;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(Team), "DoneWithAllAvailableActors")]
    public static class Team_DoneWithAllAvailableActors {
        private static void Prefix(Team __instance, List<IStackSequence> __result) {
            Mod.Log.Info($"T:DWAAA invoked");
            if (!__instance.IsLocalPlayer) { return; }

            if (__instance.Combat.TurnDirector.IsInterleavePending) {
                if (__result == null) {
                    Mod.Log.Info("Result was null, adding a new list.");
                    __result = new List<IStackSequence>();
                }

                int numUnitsEndingActivation = 0;
                foreach (AbstractActor unit in __instance.units) {
                    Mod.Log.Info($"Processing unit: {unit.DisplayName}_{unit.GetPilot().Name}");
                    if (!unit.IsCompletingActivation && !unit.IsDead && !unit.IsFlaggedForDeath) {
                        Mod.Log.Info($"  Ending activation ");
                        IStackSequence item = unit.DoneWithActor();
                        numUnitsEndingActivation++;
                        __result.Add(item);
                    }
                }

                Traverse numUnitsEndingActivationT = Traverse.Create(__instance).Field("numUnitsEndingActivation");
                int currentValue = numUnitsEndingActivationT.GetValue<int>();
                numUnitsEndingActivationT.SetValue(currentValue + numUnitsEndingActivation);
            }

        }
    }
}
