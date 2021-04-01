using BattleTech;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Extension;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches.AI.InfluenceMap
{
    public class PreferAvoidStationaryWhenOutTonned : CustomInfluenceMapPositionFactor
    {

        public PreferAvoidStationaryWhenOutTonned() { }

        public override string Name => "Prefer avoiding stationary moves when unit is out-tonned by nearby enemies";

        public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position, float angle, MoveType moveType, PathNode pathNode)
        {
            Mod.AILog.Debug?.Write($"Evaluating PreferAvoidStationaryWhenOutTonned for unit: {unit.DistinctId()} at position: {position}");

            float factor = 0f;
            if (unit is Mech mech)
            {
                float opforTonnage = 0;
                foreach (AbstractActor enemyActor in unit.Combat.AllEnemies)
                {
                    if (enemyActor is Mech enemyMech)
                    {
                        if ((position - enemyActor.CurrentPosition).magnitude < enemyActor.MaxMeleeEngageRangeDistance)
                        {
                            opforTonnage += enemyMech.tonnage;
                        }
                    }
                }

                float ratio = opforTonnage / mech.tonnage;
                Mod.AILog.Debug?.Write($"  - ratio: {ratio} from opfor tonnage: {opforTonnage} / mech tonnage: {mech.tonnage}");
            }
            else
            {
                Mod.AILog.Debug?.Write($"  - could not find an enemy within maxMeleeRange, returning 0");
            }
            return factor;
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
