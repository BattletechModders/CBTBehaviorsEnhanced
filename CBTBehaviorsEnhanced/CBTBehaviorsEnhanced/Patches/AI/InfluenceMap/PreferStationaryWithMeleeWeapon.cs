using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches.AI.InfluenceMap
{
    public class PreferStationaryWithMeleeWeapon : CustomInfluenceMapPositionFactor
    {
        public override string Name => "Prefer stationary when unit has melee weapon";

        public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position, float angle, MoveType moveType, PathNode pathNode)
        {
            Mod.AILog.Info?.Write($"Evaluating preferStationaryWhenPunchBot for unit: {unit.DistinctId()} at position: {position}");

            Mech mech = unit as Mech;
            if (mech == null)
            {
                Mod.AILog.Info?.Write($"  - unit is not a mech, skipping.");
            }

            if (!FactorUtil.IsStationaryForActor(position, angle, unit))
            {
                Mod.AILog.Info?.Write($"  - position is not a stationary node, skipping.");
                return 0f;
            }

            MechMeleeCondition meleeCondition = new MechMeleeCondition(mech);
            if (!mech.CanMakePhysicalWeaponAttack(meleeCondition))
            {
                Mod.AILog.Info?.Write($"  - mech does not have a physical attack.");
                return 0f;
            }

            float maxMeleeEngageRangeDistance = unit.MaxMeleeEngageRangeDistance;
            for (int i = 0; i < unit.BehaviorTree.enemyUnits.Count; i++)
            {
                AbstractActor abstractActor = unit.BehaviorTree.enemyUnits[i] as AbstractActor;
                if (abstractActor != null && !abstractActor.IsDead && (unit.CurrentPosition - abstractActor.CurrentPosition).magnitude <= maxMeleeEngageRangeDistance)
                {
                    // Return 50 because why not?
                    Mod.AILog.Info?.Write($"  - {abstractActor.DistinctId()} can be attacked, returning 1.0f");
                    return 1f;
                }
            }

            Mod.AILog.Info?.Write($"  - could not find an enemy within maxMeleeRange, returning 0");
            return 0f;
        }

        public override float GetRegularMoveWeight(AbstractActor actor)
        {
            throw new NotImplementedException();
        }

        public override float GetSprintMoveWeight(AbstractActor actor)
        {
            throw new NotImplementedException();
        }

    }
}
