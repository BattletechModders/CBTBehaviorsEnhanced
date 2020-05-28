using BattleTech;
using CBTBehaviorsEnhanced.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class MeleeHelper
    {
        public static HashSet<MeleeAttackDef> AvailableAttacks(Mech attacker, Vector3 attackPos, ICombatant target)
        {
			HashSet<MeleeAttackDef> availableAttacks = new HashSet<MeleeAttackDef>();

			if (target == null) return availableAttacks;

			MeleeAttackHeight validMeleeHeights = GetValidMeleeHeights(attacker, attackPos, target);

			// If prone, or the attack height is ground, the only thing we can do is stomp the target. 
			// TODO: Validate that our legs are not destroyed. Can't stomp if no legs!
			// TODO: HBS has vehicles only available as stomp targets. Reinstate that?
			if (target.IsProne || validMeleeHeights == MeleeAttackHeight.Ground)
            {
				availableAttacks.Add(new MeleeAttackDef() { Type = MeleeAttackType.Stomp });
				return availableAttacks;
            }

			Turret turret = target as Turret;
			// HBS prevents you from punching a turret. Why? 
			bool atPunchHeight = (validMeleeHeights & MeleeAttackHeight.High) != MeleeAttackHeight.None;
			if (atPunchHeight && turret == null)
			{
				// TODO: Each arm should check actuators for damage
				// TODO: Each arm should check for melee weapon
				// TODO: If melee weapon is enabled, use it
				// TODO: If melee weapon is disabled, but actuators are present, allow punch
				if (!attacker.IsLocationDestroyed(ChassisLocations.LeftArm))
                {
					availableAttacks.Add(new MeleeAttackDef() { Type = MeleeAttackType.Punch, Limb = ChassisLocations.LeftArm }); 
                }

				if (attacker.IsLocationDestroyed(ChassisLocations.RightArm))
				{
					availableAttacks.Add(new MeleeAttackDef() { Type = MeleeAttackType.Punch, Limb = ChassisLocations.RightArm });
				}
			}

			bool atKickHeight = (validMeleeHeights & MeleeAttackHeight.Low) != MeleeAttackHeight.None;
			if (atKickHeight && !attacker.IsLocationDestroyed(ChassisLocations.LeftLeg) && !attacker.IsLocationDestroyed(ChassisLocations.RightLeg))
			{
				availableAttacks.Add(new MeleeAttackDef() { Type = MeleeAttackType.Kick, Limb = ChassisLocations.RightLeg });
			}

			bool atTackleHeight = (validMeleeHeights & MeleeAttackHeight.Medium) != MeleeAttackHeight.None;
			if (atTackleHeight)
			{
				availableAttacks.Add(new MeleeAttackDef() { Type = MeleeAttackType.Tackle });
			}

			return availableAttacks;
		}

		// Duplication of HBS code for improved readability
		private static MeleeAttackHeight GetValidMeleeHeights(Mech attacker, Vector3 attackPosition, ICombatant target)
		{
			MeleeAttackHeight meleeAttackHeight = MeleeAttackHeight.None;
			AbstractActor targetActor = target as AbstractActor;
			if (targetActor == null || targetActor.UnitType == UnitType.Turret)
			{
				return MeleeAttackHeight.Low | MeleeAttackHeight.Medium | MeleeAttackHeight.High;
			}

			float attackerLowestHeight = attacker.CurrentPosition.y;
			float attackerLOSHeight = attacker.LOSSourcePositions[0].y;
			float attackerHeightDelta = attackerLOSHeight - attackerLowestHeight;
			attackerLowestHeight = attackPosition.y;
			attackerLOSHeight = attackerLowestHeight + attackerHeightDelta;

			float targetLowestHeight = target.CurrentPosition.y;
			float targetLOSHeight = targetActor.LOSSourcePositions[0].y;

			float attackPosWhenTargetAboveAttacker = targetLowestHeight > attackerLOSHeight ? attackerLOSHeight : targetLowestHeight;
			float attackPosWhenAttackerAboveTarget = attackerLowestHeight > targetLowestHeight ? attackerLowestHeight : attackPosWhenTargetAboveAttacker;

			float attackPosWhenTargetLOSBelowAttacker = targetLOSHeight < attackerLowestHeight ? attackerLowestHeight : targetLOSHeight;
			float attackerLOSBelowTargetLOSPos = attackerLOSHeight < targetLOSHeight ? attackerLOSHeight : attackPosWhenTargetLOSBelowAttacker;
			
			// Define attack heights from base of the attacker, to their their LOS height
			float lowestAttackHeight = attackerLowestHeight;
			float delta20Pos =  attackerHeightDelta * 0.2f + attackerLowestHeight;
			float delta30Pos =  attackerHeightDelta * 0.3f + attackerLowestHeight;
			float delta45Pos =  attackerHeightDelta * 0.45f + attackerLowestHeight;
			float delta75Pos =  attackerHeightDelta * 0.75f + attackerLowestHeight;
			float losAttackPos = attackerLOSHeight;

			float highestAttackPos = attackPosWhenAttackerAboveTarget;
			if ((highestAttackPos == delta20Pos && lowestAttackHeight <= attackerLOSBelowTargetLOSPos) || targetLOSHeight <= lowestAttackHeight)
			{
				meleeAttackHeight |= MeleeAttackHeight.Ground;
			}
			if (highestAttackPos == delta45Pos && delta20Pos <= attackerLOSBelowTargetLOSPos)
			{
				meleeAttackHeight |= MeleeAttackHeight.Low;
			}
			if (highestAttackPos == delta75Pos && delta30Pos <= attackerLOSBelowTargetLOSPos)
			{
				meleeAttackHeight |= MeleeAttackHeight.Medium;
			}
			if (highestAttackPos == losAttackPos && delta45Pos <= attackerLOSBelowTargetLOSPos)
			{
				meleeAttackHeight |= MeleeAttackHeight.High;
			}

			return meleeAttackHeight;
		}
	}
}
