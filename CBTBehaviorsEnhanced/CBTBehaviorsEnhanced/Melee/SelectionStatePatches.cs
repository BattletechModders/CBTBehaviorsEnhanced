using BattleTech;
using BattleTech.UI;
using Harmony;
using System.Collections.Generic;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Melee
{

    [HarmonyPatch(typeof(SelectionStateJump), "ProjectedStabilityForState", MethodType.Getter)]
    static class SelectionStateJump_ProjectedStabilityForState_Getter
    {
        static void Postfix(SelectionStateJump __instance, ref float __result)
        {
            Mod.Log.Trace("SSJ:PSFS - entered.");

            if (__instance.SelectedActor is Mech selectedMech && ModState.MeleeStates?.SelectedState != null && __instance.PotentialMeleeTarget != null)
            {
                float newStability = selectedMech.CurrentStability + ModState.MeleeStates.SelectedState.AttackerInstability;
                float minStability = selectedMech.GetMinStability(StabilityChangeSource.DFA, newStability);
                Mod.Log.Debug($"Stability change for {CombatantUtils.Label(selectedMech)} => " +
                    $"current: {selectedMech.CurrentStability}  projectedNew: {ModState.MeleeStates.SelectedState.AttackerInstability}  " +
                    $"totalChange: {newStability}  afterDump: {minStability}");
                __result = minStability;
            }
        }
    }

    [HarmonyPatch(typeof(SelectionStateMove), "ProjectedStabilityForState", MethodType.Getter)]
    static class SelectionStateMove_ProjectedStabilityForState_Getter
    {
        static void Postfix(SelectionStateMove __instance, ref float __result)
        {
            Mod.Log.Trace("SSM:PSFS - entered.");

            if (__instance.SelectedActor is Mech selectedMech && ModState.MeleeStates?.SelectedState != null && __instance.PotentialMeleeTarget != null)
            {
                float newStability = selectedMech.CurrentStability + ModState.MeleeStates.SelectedState.AttackerInstability;
                
                List<WayPoint> waypoints = ActorMovementSequence.ExtractWaypointsFromPath(selectedMech, selectedMech.Pathing.CurrentPath, selectedMech.Pathing.ResultDestination, selectedMech.Pathing.CurrentMeleeTarget, selectedMech.Pathing.MoveType);
                StabilityChangeSource changeSource = StabilityChangeSource.Moving;
                if (WayPoint.GetDistFromWaypointList(selectedMech.CurrentPosition, waypoints) < 1f)
                {
                    changeSource = StabilityChangeSource.RemainingStationary;
                }
                float minStability = selectedMech.GetMinStability(changeSource, newStability);
                Mod.Log.Debug($"Stability change for {CombatantUtils.Label(selectedMech)} => " +
                    $"current: {selectedMech.CurrentStability}  projectedNew: {ModState.MeleeStates.SelectedState.AttackerInstability}  " +
                    $"totalChange: {newStability}  afterDump: {minStability}");
                __result = minStability;
            }
        }
    }
}
