using BattleTech;
using CBTBehaviorsEnhanced.Objects;
using System.Collections.Generic;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Helper
{

	public static class DamageHelper
    {
		public static void ClusterDamage(float totalDamage, float divisor, out float[] clusteredDamage)
        {
			List<float> clusters = new List<float>();
			while (totalDamage > 0)
			{
				if (totalDamage > divisor)
				{
					clusters.Add(divisor);
					totalDamage -= divisor;
				}
				else
				{
					clusters.Add(totalDamage);
					totalDamage = 0;
				}
			}
			clusteredDamage = clusters.ToArray();
		}
    }

    public static class MeleeHelper
    {

		// This assumes you're calling from a place that has already determined that we can reach the target.
		public static MeleeStates GetMeleeStates(AbstractActor attacker, Vector3 attackPos, ICombatant target)
        {
			if (attacker == null || target == null)
            {
				Mod.Log.Warn("Null attacker or target - cannot melee!");
				return new MeleeStates();
			}

			if (attacker is Turret turret)
			{
				Mod.Log.Warn("I don't know how a turret does melee!");
				return new MeleeStates();
			}

			// TODO: YET
			if (attacker is Vehicle vehicle)
			{
				Mod.Log.Warn("I don't know how a vehicle does melee!");
				return new MeleeStates();
			}

			if (target is BattleTech.Building building)
            {
				Mod.Log.Warn("I don't know how to melee a building!");
				return new MeleeStates();
			}

			Mod.Log.Info($"Building melee state for attacker: {CombatantUtils.Label(attacker)} against target: {CombatantUtils.Label(target)}");

			Mech attackerMech = attacker as Mech;
			AbstractActor targetActor = target as AbstractActor;

			HashSet<MeleeAttackType> validAnimations = AvailableAttacks(attackerMech, attackPos, target);

			return new MeleeStates()
			{
				Charge = new ChargeMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				DFA = new DFAMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				Kick = new KickMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				PhysicalWeapon = new PhysicalWeaponMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				Punch = new PunchMeleeState(attackerMech, attackPos, targetActor, validAnimations),
			};
        }

        public static HashSet<MeleeAttackType> AvailableAttacks(Mech attacker, Vector3 attackPos, ICombatant target)
        {
			HashSet<MeleeAttackType> availableAttacks = new HashSet<MeleeAttackType>();

			if (target == null) return availableAttacks;

			MeleeAttackHeight validMeleeHeights = GetValidMeleeHeights(attacker, attackPos, target);

			// If prone, or the attack height is ground, the only thing we can do is stomp the target. 
			// TODO: HBS has vehicles only available as stomp targets. Reinstate that?
			if (target.IsProne || validMeleeHeights == MeleeAttackHeight.Ground)
            {
				availableAttacks.Add(MeleeAttackType.Stomp);
				return availableAttacks;
            }

			Turret turret = target as Turret;
			// HBS prevents you from punching a turret. Why? 
			bool atPunchHeight = (validMeleeHeights & MeleeAttackHeight.High) != MeleeAttackHeight.None;
			if (atPunchHeight && turret == null)
			{
				if (attacker.MechDef.Chassis.PunchesWithLeftArm && !attacker.IsLocationDestroyed(ChassisLocations.LeftArm))
                {
					availableAttacks.Add(MeleeAttackType.Punch); 
                }

				if (!attacker.MechDef.Chassis.PunchesWithLeftArm && attacker.IsLocationDestroyed(ChassisLocations.RightArm))
				{
					availableAttacks.Add(MeleeAttackType.Punch);
				}
			}

			bool atKickHeight = (validMeleeHeights & MeleeAttackHeight.Low) != MeleeAttackHeight.None;
			if (atKickHeight && !attacker.IsLocationDestroyed(ChassisLocations.LeftLeg) && !attacker.IsLocationDestroyed(ChassisLocations.RightLeg))
			{
				availableAttacks.Add(MeleeAttackType.Kick);
			}

			bool atTackleHeight = (validMeleeHeights & MeleeAttackHeight.Medium) != MeleeAttackHeight.None;
			if (atTackleHeight)
			{
				availableAttacks.Add(MeleeAttackType.Tackle);
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
