using BattleTech;
using CustomComponents;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Helper
{
    public static class MeleeHelper
    {

		// This assumes you're calling from a place that has already determined that we can reach the target.
		public static MeleeState GetMeleeState(AbstractActor attacker, Vector3 attackPos, ICombatant target)
        {
			if (attacker == null || target == null)
            {
				Mod.Log.Warn("Null attacker or target - cannot melee!");
				return new MeleeState { CanCharge = false, CanKick = false, CanPunch = false };
			}

			if (attacker is Turret turret)
			{
				Mod.Log.Warn("I don't know how a turret does melee!");
				return new MeleeState { CanCharge = false, CanKick = false, CanPunch = false };

			}

			if (target is BattleTech.Building building)
            {
				Mod.Log.Warn("I don't know how to melee a building!");
				return new MeleeState { CanCharge = false, CanKick = false, CanPunch = false };
            }

			MeleeState meleeState = new MeleeState();
			Mod.Log.Info($"Building melee state for attacker: {CombatantUtils.Label(attacker)} against target: {CombatantUtils.Label(target)}");
			if (attacker is Mech attackerMech)
            {
				float distance = (attackPos - target.CurrentPosition).magnitude;
				Mod.Log.Info($"Attack distance: {distance}m = attackPos: {attackPos} - targetPos: {target.CurrentPosition}");

				// Evaluate our damage state and determine what our total damage should be
				meleeState.EvaluateDamage(attackerMech);

				// Check if our distance requires a sprint (and thus no punch, kick, etc)
				float maxWalkSpeed = MechHelper.FinalWalkSpeed(attackerMech);
				float maxSprintSpeed = MechHelper.FinalRunSpeed(attackerMech);
				Mod.Log.Info($"Attacker walkSpeed: {maxWalkSpeed}m  sprintSpeed: {maxSprintSpeed}m");
				if (distance > maxWalkSpeed)
                {
					meleeState.CanPunch = false;
					meleeState.CanKick = false;
                }

				// If any hips are gone, cannot kick 
				// If any shoulders are gone, cannot punch from that arm
				// Add any damage from two working punches directly
				// If attacker prone, no attacks are allowed
				// Check elevation levels here; kick only allowed at certain elevations, etc

				// How to handle absolute vs. multiplier modifiers? Let all modifiers apply... damage will remove the effects. Then apply the missing actuator modifiers
				
			}

			// TODO: Handle vehicles someday?

			return new MeleeState();
        }

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
