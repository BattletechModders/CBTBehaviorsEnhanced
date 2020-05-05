using BattleTech;
using Harmony;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Patches {

    [HarmonyPatch(typeof(Team), "DoneWithAllAvailableActors")]
    public static class Team_DoneWithAllAvailableActors {
        private static void Prefix(Team __instance, List<IStackSequence> __result) {
            Mod.Log.Debug($"T:DWAAA invoked");
            if (!__instance.IsLocalPlayer) { return; }

            if (__instance.Combat.TurnDirector.IsInterleavePending) {
                if (__result == null) {
                    Mod.Log.Debug("Result was null, adding a new list.");
                    __result = new List<IStackSequence>();
                }

                int numUnitsEndingActivation = 0;
                foreach (AbstractActor unit in __instance.units) {
                    Mod.Log.Debug($"Processing unit: {unit.DisplayName}_{unit.GetPilot().Name}");
                    if (!unit.IsCompletingActivation && !unit.IsDead && !unit.IsFlaggedForDeath) {
                        Mod.Log.Info($"  Ending activation for unit {CombatantUtils.Label(unit)}");
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
