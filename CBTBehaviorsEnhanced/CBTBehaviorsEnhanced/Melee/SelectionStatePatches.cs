using BattleTech;
using BattleTech.UI;
using CBTBehaviorsEnhanced.MeleeStates;
using HarmonyLib;
using IRBTModUtils;
using IRBTModUtils.Extension;
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
                Mod.Log.Trace?.Write($"Stability change for {CombatantUtils.Label(selectedMech)} => " +
                    $"current: {selectedMech.CurrentStability}  projectedNew: {selectedAttack.AttackerInstability}  " +
                    $"totalChange: {newStability}  afterDump: {minStability}");
                __result = minStability;
            }
        }
    }

    [HarmonyPatch(typeof(SelectionStateMove), "GetAllMeleeTargets")]
    static class SelectionStateMove_GetAllMeleeTargets
    {
        static void Postfix(SelectionStateMove __instance, ref List<ICombatant> __result)
        {
            if (__instance == null || __result == null) return;
            if (__instance.SelectedActor == null || !(__instance.SelectedActor is Mech)) return;

            // If there are valid results, check for any melee state that would prevent attacks
            if (__result != null && __result.Count > 0)
            {
                Mech selectedMech = __instance.SelectedActor as Mech;
                Mod.MeleeLog.Debug?.Write($" filtering targets of mech: {selectedMech.DistinctId()} for melee state");
                List<ICombatant> filteredList = new List<ICombatant>();
                foreach (ICombatant target in __result)
                {
                    if (target is AbstractActor targetActor)
                    {
                        Mod.MeleeLog.Debug?.Write($"Finding selection melee state for actor: {__instance.SelectedActor.DistinctId()} " +
                            $"at pos: {__instance.PreviewPos} vs. target: {targetActor.DistinctId()}");
                        MeleeState meleeState = new MeleeState(selectedMech, __instance.PreviewPos, targetActor);

                        bool hasValidAttack = meleeState.Charge.IsValid || meleeState.Kick.IsValid ||
                            meleeState.PhysicalWeapon.IsValid || meleeState.Punch.IsValid;
                        if (!hasValidAttack)
                        {
                            Mod.MeleeLog.Debug?.Write($" -- No valid attack detected, removing target as melee candidate");
                        }
                        else
                        {
                            Mod.MeleeLog.Debug?.Write($"  -- Valid attacks, keeping target as candidate for attacks =>  " +
                                $"charge: {meleeState.Charge.IsValid}  kick: {meleeState.Kick.IsValid}  punch: {meleeState.Punch.IsValid}  weapon: {meleeState.PhysicalWeapon.IsValid}");
                            filteredList.Add(targetActor);
                        }
                    }
                    else
                    {
                        Mod.MeleeLog.Debug?.Write($" -- skipping non-actor target: {target.DistinctId()}");
                    }
                }

                __result.Clear();
                __result.AddRange(filteredList);
            }
        }
    }

}
