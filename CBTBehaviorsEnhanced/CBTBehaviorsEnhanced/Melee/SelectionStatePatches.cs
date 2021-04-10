using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.MeleeStates;
using Harmony;
using IRBTModUtils;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee
{
    // Update instability preview
    [HarmonyPatch(typeof(SelectionStateJump), "ProjectedStabilityForState", MethodType.Getter)]
    static class SelectionStateJump_ProjectedStabilityForState_Getter
    {
        static void Postfix(SelectionStateJump __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("SSJ:PSFS - entered.");

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.SelectedActor);
            if (__instance.SelectedActor is Mech selectedMech && selectedAttack != null && __instance.PotentialMeleeTarget != null)
            {
                float newStability = selectedMech.CurrentStability + selectedAttack.AttackerInstability;
                float minStability = selectedMech.GetMinStability(StabilityChangeSource.DFA, newStability);
                Mod.Log.Debug?.Write($"Stability change for {CombatantUtils.Label(selectedMech)} => " +
                    $"current: {selectedMech.CurrentStability}  projectedNew: {selectedAttack.AttackerInstability}  " +
                    $"totalChange: {newStability}  afterDump: {minStability}");
                __result = minStability;
            }
        }
    }

    // Update instability preview
    [HarmonyPatch(typeof(SelectionStateMove), "ProjectedStabilityForState", MethodType.Getter)]
    static class SelectionStateMove_ProjectedStabilityForState_Getter
    {
        static void Postfix(SelectionStateMove __instance, ref float __result)
        {
            Mod.Log.Trace?.Write("SSM:PSFS - entered.");

            MeleeAttack selectedAttack = ModState.GetSelectedAttack(__instance.SelectedActor);
            if (__instance.SelectedActor is Mech selectedMech && selectedAttack != null && __instance.PotentialMeleeTarget != null)
            {
                float newStability = selectedMech.CurrentStability + selectedAttack.AttackerInstability;
                
                List<WayPoint> waypoints = ActorMovementSequence.ExtractWaypointsFromPath(selectedMech, selectedMech.Pathing.CurrentPath, selectedMech.Pathing.ResultDestination, selectedMech.Pathing.CurrentMeleeTarget, selectedMech.Pathing.MoveType);
                StabilityChangeSource changeSource = StabilityChangeSource.Moving;
                if (WayPoint.GetDistFromWaypointList(selectedMech.CurrentPosition, waypoints) < 1f)
                {
                    changeSource = StabilityChangeSource.RemainingStationary;
                }
                float minStability = selectedMech.GetMinStability(changeSource, newStability);
                Mod.Log.Debug?.Write($"Stability change for {CombatantUtils.Label(selectedMech)} => " +
                    $"current: {selectedMech.CurrentStability}  projectedNew: {selectedAttack.AttackerInstability}  " +
                    $"totalChange: {newStability}  afterDump: {minStability}");
                __result = minStability;
            }
        }
    }

}
