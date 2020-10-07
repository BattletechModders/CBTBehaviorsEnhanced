using BattleTech;
using BattleTech.UI;
using IRBTModUtils;
using IRBTModUtils.Extension;
using System.Collections.Generic;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class CACDelegates
    {
        public static float HeatToHitModifer(ToHit toHit, AbstractActor attacker, Weapon weapon, ICombatant target, 
            Vector3 attackPos, Vector3 targetPos,  LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot)
        {

            float modifier = 0;
            if (attacker is Mech mech && mech.IsOverheated)
            {

                float penalty = 0f;
                foreach (KeyValuePair<int, int> kvp in Mod.Config.Heat.Firing)
                {
                    if (mech.CurrentHeat >= kvp.Key)
                    {
                        penalty = kvp.Value;
                    }
                }

                Mod.Log.Trace?.Write($"  AttackPenalty: {penalty:+0;-#} from heat: {mech.CurrentHeat} for actor: {attacker.DistinctId()}");
                modifier = penalty;
            }

            return modifier;
        }

        public static float JumpedToHitModifier(ToHit toHit, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPos, Vector3 targetPos, LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot)
        {

            float modifier = 0;

            if (attacker == null || weapon == null) return 0;

            if (
                (attacker.HasMovedThisRound && attacker.JumpedLastRound) ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump)
                )
            {
                Mod.Log.Debug?.Write($"Attacker jumped, adding attack modifier: {Mod.Config.ToHitSelfJumped}");
                modifier = (float)Mod.Config.ToHitSelfJumped;
            }

            return modifier;
        }

        public static float MeleeToHitModifiers(ToHit toHit, AbstractActor attacker, Weapon weapon, ICombatant target,
            Vector3 attackPos, Vector3 targetPos, LineOfFireLevel lofLevel, MeleeAttackType meleeAttackType, bool isCalledShot)
        {

            float modifier = 0;

            if (attacker == null || weapon == null) return 0;

            if (
                (attacker.HasMovedThisRound && attacker.JumpedLastRound) ||
                (SharedState.CombatHUD?.SelectionHandler?.ActiveState != null &&
                SharedState.CombatHUD?.SelectionHandler?.ActiveState is SelectionStateJump)
                )
            {
                modifier = (float)Mod.Config.ToHitSelfJumped;
            }

            return modifier;
        }
    }


}
