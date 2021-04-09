using BattleTech;
using CBTBehaviorsEnhanced.Extensions;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Extension;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches.AI.InfluenceMap
{
    public class PreferStationaryWithMeleeWeapon : CustomInfluenceMapPositionFactor
    {
        public PreferStationaryWithMeleeWeapon() { }

        public override string Name => "Prefer stationary when unit has melee weapon";

        public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position, float angle, MoveType moveType, PathNode pathNode)
        {
            Mod.AILog.Debug?.Write($"Evaluating PreferStationaryWithMeleeWeapon for unit: {unit.DistinctId()} at position: {position}");

            Mech mech = unit as Mech;
            if (mech == null)
            {
                Mod.AILog.Debug?.Write($"  - unit is not a mech, skipping.");
                return 0f;
            }

            if (!FactorUtil.IsStationaryForActor(position, angle, unit))
            {
                Mod.AILog.Debug?.Write($"  - position is not a stationary node, skipping.");
                return 0f;
            }

            if (!mech.CanMakePhysicalWeaponAttack())
            {
                Mod.AILog.Debug?.Write($"  - mech does not have a physical attack.");
                return 0f;
            }

            float maxMeleeEngageRangeDistance = unit.MaxMeleeEngageRangeDistance;
            for (int i = 0; i < unit.BehaviorTree.enemyUnits.Count; i++)
            {
                AbstractActor abstractActor = unit.BehaviorTree.enemyUnits[i] as AbstractActor;
                if (abstractActor != null && !abstractActor.IsDead && (unit.CurrentPosition - abstractActor.CurrentPosition).magnitude <= maxMeleeEngageRangeDistance)
                {
                    Mod.AILog.Debug?.Write($"  - {abstractActor.DistinctId()} can be attacked, returning 1.0f");
                    return 1f;
                }
            }

            Mod.AILog.Debug?.Write($"  - could not find an enemy within maxMeleeRange, returning 0");
            return 0f;
        }

        public override float GetRegularMoveWeight(AbstractActor actor)
        {
            // TODO: Implement config linking tags to a value?
            return 1f;
        }

        public override float GetSprintMoveWeight(AbstractActor actor)
        {
            // TODO: Implement config linking tags to a value?
            return 1f;
        }

    }
}
