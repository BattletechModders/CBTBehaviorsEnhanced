using IRBTModUtils.Extension;
using System.Reflection;

namespace CBTBehaviorsEnhanced.Patches.AI
{
    [HarmonyPatch(typeof(CanMeleeHostileTargetsNode), "Tick")]
    static class CanMeleeHostileTargetsNode_Tick_Patch
    {

        static void Prefix(ref bool __runOriginal, ref BehaviorTreeResults __result,
            string ___name, BehaviorTree ___tree, AbstractActor ___unit)
        {
            if (!__runOriginal) return;

            Mod.AILog.Info?.Write("CanMeleeHostileTargetsNode:Tick() invoked.");

            if (!(___unit is Mech))
            {
                // Not a mech, so don't allow them to melee
                __result = new BehaviorTreeResults(BehaviorNodeState.Failure);
                __runOriginal = false;
                return;
            }


            Mod.AILog.Info?.Write($"AI attacker: {___unit.DistinctId()} has {___unit.BehaviorTree.enemyUnits.Count} enemy units.");
            for (int j = 0; j < ___unit.BehaviorTree.enemyUnits.Count; j++)
            {
                ICombatant targetCombatant = ___unit.BehaviorTree.enemyUnits[j];

                // Skip the retaliation check; melee w/ kick is always viable, as is charge
                //Mech targetMech = targetCombatant as Mech;
                //if (targetMech != null)
                //{
                //	float num = AIUtil.ExpectedDamageForMeleeAttackUsingUnitsBVs(targetMech, unit, targetMech.CurrentPosition, mech.CurrentPosition, useRevengeBonus: false, unit);
                //	float num2 = AIUtil.ExpectedDamageForMeleeAttackUsingUnitsBVs(mech, targetMech, mech.CurrentPosition, targetMech.CurrentPosition, useRevengeBonus: false, unit);
                //	if (num2 <= 0f)
                //	{
                //		continue;
                //	}
                //	float num3 = num / num2;
                //	if (flag2 && num3 > unit.BehaviorTree.GetBehaviorVariableValue(BehaviorVariableName.Float_MeleeDamageRatioCap).FloatVal)
                //	{
                //		continue;
                //	}
                //}

                if (___unit.CanEngageTarget(targetCombatant))
                {
                    Mod.AILog.Info?.Write($"AI attacker: {___unit.DistinctId()} can engage target: {targetCombatant.DistinctId()}, returning true nodeState.");
                    __result = new BehaviorTreeResults(BehaviorNodeState.Success);
                    __runOriginal = false;
                    return;                    
                }
                else
                {
                    Mod.AILog.Info?.Write($"AI attacker: {___unit.DistinctId()} can NOT engage target: {targetCombatant.DistinctId()}");
                }
            }

            Mod.AILog.Info?.Write($"AI source: {___unit.DistinctId()} could find no targets, skipping.");
            // Fall through - couldn't find a single enemy unit we could attack, so skip
            __result = new BehaviorTreeResults(BehaviorNodeState.Failure);

            __runOriginal = false;
        }
    }

}
