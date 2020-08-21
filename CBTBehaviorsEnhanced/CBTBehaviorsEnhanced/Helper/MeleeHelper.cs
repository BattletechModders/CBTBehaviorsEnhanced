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
				Mod.Log.Warn?.Write("Null attacker or target - cannot melee!");
				return new MeleeStates();
			}

			if (attacker is Turret turret)
			{
				Mod.Log.Warn?.Write("I don't know how a turret does melee!");
				return new MeleeStates();
			}

			// TODO: YET
			if (attacker is Vehicle vehicle)
			{
				Mod.Log.Warn?.Write("I don't know how a vehicle does melee!");
				return new MeleeStates();
			}

			if (target is BattleTech.Building building)
            {
				Mod.Log.Warn?.Write("I don't know how to melee a building!");
				return new MeleeStates();
			}

			Mod.Log.Info?.Write($"Building melee state for attacker: {CombatantUtils.Label(attacker)} against target: {CombatantUtils.Label(target)}");

			Mech attackerMech = attacker as Mech;
			AbstractActor targetActor = target as AbstractActor;

			HashSet<MeleeAttackType> validAnimations = AvailableAttacks(attackerMech, attackPos, target);

			MeleeStates states = new MeleeStates()
			{
				Charge = new ChargeMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				DFA = new DFAMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				Kick = new KickMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				PhysicalWeapon = new PhysicalWeaponMeleeState(attackerMech, attackPos, targetActor, validAnimations),
				Punch = new PunchMeleeState(attackerMech, attackPos, targetActor, validAnimations),
			};
			Mod.Log.Info?.Write($" - valid attacks => charge: {states.Charge.IsValid}  dfa: {states.DFA.IsValid}  kick: {states.Kick.IsValid}  " +
				$"weapon: {states.PhysicalWeapon.IsValid}  punch: {states.Punch.IsValid}");

			return states;
			
        }

        public static HashSet<MeleeAttackType> AvailableAttacks(Mech attacker, Vector3 attackPos, ICombatant target)
        {
			Mod.Log.Info?.Write($"Checking available animations for attacker: {CombatantUtils.Label(attacker)} " +
				$"at position: {attackPos} versus target: {CombatantUtils.Label(target)}");

			HashSet<MeleeAttackType> availableAttacks = new HashSet<MeleeAttackType>();
			if (target == null) return availableAttacks;

			MeleeAttackHeight validMeleeHeights = GetValidMeleeHeights(attacker, attackPos, target);
			Mod.Log.Info?.Write($"ValidMeleeHeights => {validMeleeHeights}");

			// If prone, or the attack height is ground, the only thing we can do is stomp the target. 
			// TODO: HBS has vehicles only available as stomp targets. Reinstate that?
			if (target.IsProne || validMeleeHeights == MeleeAttackHeight.Ground)
            {
				availableAttacks.Add(MeleeAttackType.Stomp);
				Mod.Log.Info?.Write($" - Target is prone or attack height is ground, returning STOMP.");
				return availableAttacks;
            }

			Turret turret = target as Turret;
			// HBS prevents you from punching a turret. Why? 
			bool atPunchHeight = (validMeleeHeights & MeleeAttackHeight.High) != MeleeAttackHeight.None;
			if (atPunchHeight && turret == null)
			{
				if (attacker.MechDef.Chassis.PunchesWithLeftArm && !attacker.IsLocationDestroyed(ChassisLocations.LeftArm))
                {
					Mod.Log.Info?.Write($" - Adding punch as left arm is not destroyed");
					availableAttacks.Add(MeleeAttackType.Punch); 
                }

				if (!attacker.MechDef.Chassis.PunchesWithLeftArm && !attacker.IsLocationDestroyed(ChassisLocations.RightArm))
				{
					Mod.Log.Info?.Write($" - Adding punch as right arm is not destroyed");
					availableAttacks.Add(MeleeAttackType.Punch);
				}
			}

			bool atKickHeight = (validMeleeHeights & MeleeAttackHeight.Low) != MeleeAttackHeight.None;
			if (atKickHeight 
				&& !attacker.IsLocationDestroyed(ChassisLocations.LeftLeg) 
				&& !attacker.IsLocationDestroyed(ChassisLocations.RightLeg)
				)
			{
				Mod.Log.Info?.Write($" - Adding kick");
				availableAttacks.Add(MeleeAttackType.Kick);
			}

			bool atTackleHeight = (validMeleeHeights & MeleeAttackHeight.Medium) != MeleeAttackHeight.None;
			if (atTackleHeight)
			{
				Mod.Log.Info?.Write($" - Adding tackle");
				availableAttacks.Add(MeleeAttackType.Tackle);
			}

			Mod.Log.Info?.Write($" - Returning {availableAttacks.Count} available attack animations");
			return availableAttacks;
		}

		// Duplication of HBS code for improved readability and logging
		private static MeleeAttackHeight GetValidMeleeHeights(Mech attacker, Vector3 attackPosition, ICombatant target)
		{
			Mod.Log.Info?.Write($"Evaluating melee attack height for {CombatantUtils.Label(attacker)} at position: {attackPosition} " +
				$"vs. target: {CombatantUtils.Label(target)}");
			
			MeleeAttackHeight meleeAttackHeight = MeleeAttackHeight.None;
			AbstractActor abstractActor = target as AbstractActor;
			
			if (abstractActor == null || abstractActor.UnitType == UnitType.Turret)
			{
				return (MeleeAttackHeight.Low | MeleeAttackHeight.Medium | MeleeAttackHeight.High);
			}

			float attackerBase_Y = attacker.CurrentPosition.y;
			float attackerLOS_Y = attacker.LOSSourcePositions[0].y;
			float attackerHeightBaseToLOS = attackerLOS_Y - attackerBase_Y;

			attackerBase_Y = attackPosition.y;
			attackerLOS_Y = attackerBase_Y + attackerHeightBaseToLOS;
			Mod.Log.Info?.Write($" - attackerBase_Y: {attackerBase_Y} attackerLOS_Y: {attackerLOS_Y} attackerHeightBaseToLOS: {attackerHeightBaseToLOS}");

			float targetBase_Y = target.CurrentPosition.y;
			float targetLOS_Y = ((AbstractActor)target).LOSSourcePositions[0].y;
			Mod.Log.Info?.Write($" - targetBase_Y: {targetBase_Y} targetLOS_Y: {targetLOS_Y }");

			// If attacker base > target base -> attacker base
			// else if target base > attacker LOS -> attacker LOS 
			// else else -> target base
			float lowestAttackPosOnTarget = (attackerBase_Y > targetBase_Y) ? 
				attackerBase_Y : 
				((targetBase_Y > attackerLOS_Y) ? attackerLOS_Y : targetBase_Y);
			
			// If attacker LOS < target LOS -> attacker LOS
			// else if target LOS < attacker base -> attacker base
			// else else -> target LOS
			float highestAttackPosOnTarget = (attackerLOS_Y < targetLOS_Y) ? 
				attackerLOS_Y : 
				((targetLOS_Y < attackerBase_Y) ? attackerBase_Y : targetLOS_Y);
			Mod.Log.Info?.Write($" - lowestAttackPosOnTarget: {lowestAttackPosOnTarget} highestAttackPosOnTarget: {highestAttackPosOnTarget}");
			
			float lowestAttack_Y = attackerBase_Y;
			
			float delta20   = attackerHeightBaseToLOS * 0.2f + attackerBase_Y;
			float delta20_2 = attackerHeightBaseToLOS * 0.2f + attackerBase_Y;
			
			float delta30 = attackerHeightBaseToLOS * 0.3f + attackerBase_Y;
			
			float delta75 = attackerHeightBaseToLOS * 0.75f + attackerBase_Y;
			
			float delta45   = attackerHeightBaseToLOS * 0.45f + attackerBase_Y;
			float delta45_2 = attackerHeightBaseToLOS * 0.45f + attackerBase_Y;
			
			float highestAttack_Y = attackerLOS_Y;

			Mod.Log.Info?.Write($" - lowestAttack_Y: {lowestAttack_Y}  delta20: {delta20}  delta30: {delta30}  delta45: {delta45}  " +
				$"delta75: {delta75}  highestAttack_Y: {highestAttack_Y}");

			float lowestAttackPosOnTarget_2 = lowestAttackPosOnTarget;

			if ((lowestAttackPosOnTarget_2 <= delta20 && lowestAttack_Y <= highestAttackPosOnTarget) || targetLOS_Y <= lowestAttack_Y)
			{
				Mod.Log.Info?.Write(" - adding Ground attack.");
				meleeAttackHeight |= MeleeAttackHeight.Ground;
			}

			if (lowestAttackPosOnTarget_2 <= delta45 && delta20_2 <= highestAttackPosOnTarget)
			{
				Mod.Log.Info?.Write(" - adding Low attack.");
				meleeAttackHeight |= MeleeAttackHeight.Low;
			}

			if (lowestAttackPosOnTarget_2 <= delta75 && delta30 <= highestAttackPosOnTarget)
			{
				Mod.Log.Info?.Write(" - adding Medium attack.");
				meleeAttackHeight |= MeleeAttackHeight.Medium;
			}

			if (lowestAttackPosOnTarget_2 <= highestAttack_Y && delta45_2 <= highestAttackPosOnTarget)
			{
				Mod.Log.Info?.Write(" - adding High attack.");
				meleeAttackHeight |= MeleeAttackHeight.High;
			}

			Mod.Log.Info?.Write($" - Melee attack height = {meleeAttackHeight}");
			return meleeAttackHeight;
		}
	}
}
