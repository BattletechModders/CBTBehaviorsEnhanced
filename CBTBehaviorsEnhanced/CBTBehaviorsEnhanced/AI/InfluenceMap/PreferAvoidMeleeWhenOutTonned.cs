using BattleTech;
using CustomUnits;
using IRBTModUtils.CustomInfluenceMap;
using IRBTModUtils.Extension;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Patches.AI.InfluenceMap
{
    public class PreferAvoidMeleeWhenOutTonned : CustomInfluenceMapPositionFactor
    {

        public PreferAvoidMeleeWhenOutTonned() { }

        public override string Name => "Prefer avoiding melee positions for enemies when out-tonned.";

        public override float EvaluateInfluenceMapFactorAtPosition(AbstractActor unit, Vector3 position, float angle, MoveType moveType, PathNode pathNode)
        {
            Mod.AILog.Debug?.Write($"Evaluating PreferAvoidStationaryWhenOutTonned for unit: {unit.DistinctId()} at position: {position}");

            float factor = 0f;
            if (unit is TrooperSquad)
            {
                // Troopers don't care about being out-tonned, they are geared for that
                return -500;
            }
            else if (unit is Mech mech)
            {
                // TODO: SHould be vehicle OR mech, since both can move
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

                // TODO: Should incorporate check for ranged shots. This may be priortizing nodes outside of effective weapon range?
                //  Maybe should be a position with lots of shots instead? Seperate InfluenceMapPositionFactor

                float ratio = -1 * opforTonnage / mech.tonnage;
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
