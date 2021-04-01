using BattleTech;
using Harmony;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CBTBehaviorsEnhanced.Patches.AI
{
    [HarmonyPatch]
    static class CanMeleeHostileTargetsNode_Tick_Patch
    {

        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("CanMeleeHostileTargetsNode");
            return AccessTools.Method(type, "Tick");
        }

        static bool Prefix(ref BehaviorTreeResults __result, 
            string ___name, BehaviorTree ___tree, AbstractActor ___unit)
        {
            Mod.AILog.Info?.Write("CanMeleeHostileTargetsNode:Tick() invoked.");

			if (!( ___unit is Mech))
            {
				// Not a mech, so don't allow them to melee
				__result = new BehaviorTreeResults(BehaviorNodeState.Failure);
				return false;
			}

			//bool flag = false;
			//for (int i = 0; i < unit.Weapons.Count; i++)
			//{
			//	if (unit.Weapons[i].CanFire)
			//	{
			//		flag = true;
			//		break;
			//	}
			//}
			
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
					return false;
				}
				else
                {
					Mod.AILog.Info?.Write($"AI attacker: {___unit.DistinctId()} can NOT engage target: {targetCombatant.DistinctId()}");
				}
			}

			Mod.AILog.Info?.Write($"AI source: {___unit.DistinctId()} could find no targets, skipping.");
			// Fall through - couldn't find a single enemy unit we could attack, so skip
			__result = new BehaviorTreeResults(BehaviorNodeState.Failure);
			return false;
        }
    }

}
