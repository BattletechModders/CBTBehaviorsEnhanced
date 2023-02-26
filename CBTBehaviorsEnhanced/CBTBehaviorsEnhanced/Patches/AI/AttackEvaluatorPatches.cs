using BattleTech;
using CBTBehaviorsEnhanced.MeleeStates;
using HarmonyLib;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBTBehaviorsEnhanced.Patches.AI
{
    [HarmonyPatch(typeof(AttackEvaluator), "MakeAttackOrder")]
    [HarmonyAfter("io.mission.modrepuation", "us.frostraptor.CleverGirl")]
    static class AttackEvaluator_MakeAttackOrder
    {

        // This patch needs to run to correctly fix the melee attack to the target selected by CleverGirl/AI.
        //   During AI eval multiple targets are evaluated, and this ensures we use the attack for the one that was picked.
        static void Postfix(AbstractActor unit, ref BehaviorTreeResults __result)
        {
            if (__result != null && __result.nodeState == BehaviorNodeState.Success && __result.orderInfo is AttackOrderInfo attackOrderInfo)
            {
                if (attackOrderInfo.IsMelee)
                {
                    Mod.Log.Debug?.Write($"Setting melee weapon for attack from attacker: {unit?.DistinctId()} versus target: {attackOrderInfo.TargetUnit?.DistinctId()}");
                    // Create melee options
                    MeleeState meleeState = ModState.AddorUpdateMeleeState(unit, attackOrderInfo.AttackFromLocation, attackOrderInfo.TargetUnit);
                    if (meleeState != null)
                    {
                        MeleeAttack meleeAttack = meleeState.GetHighestDamageAttackForUI();
                        ModState.AddOrUpdateSelectedAttack(unit, meleeAttack);
                    }
                }
                else if (attackOrderInfo.IsDeathFromAbove)
                {
                    // Create melee options
                    MeleeState meleeState = ModState.AddorUpdateMeleeState(unit, attackOrderInfo.AttackFromLocation, attackOrderInfo.TargetUnit);
                    if (meleeState != null)
                    {
                        MeleeAttack meleeAttack = meleeState.DFA;
                        ModState.AddOrUpdateSelectedAttack(unit, meleeAttack);
                    }
                }
            }
            else
            {
                Mod.Log.Trace?.Write($"BehaviorTree result is not failed: {__result?.nodeState} or is not an attackOrderInfo, skipping.");
            }
        }
    }
}
