﻿using BattleTech;
using CBTBehaviorsEnhanced.Helper;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using us.frostraptor.modUtils;

namespace CBTBehaviorsEnhanced.Objects
{
    public class DFAMeleeState : MeleeState
    {
		// Per BT Manual pg.36,
		//   * target takes 1 pt. each 10 tons of attacker, which is then multiplied by 3 and rounded up
		//   * attacker takes tonnage / 5, rounded up
		//   * Damage clustered in 5 (25) point groupings for both attacker & defender
		//   * Target resolves on punch table
		//   *   Prone targets resolve on rear table
		//   * Attacker resolves on kick table
		//   * Comparative attack modifier; difference in attacker and defender is applied to attack
		//   *  +3 modifier to hit for jumping
		//   *  +2 to hit if upper or lower leg actuators are hit
		//   *  -2 modifier if target is prone
		//   * Attacker makes PSR with +4, target with +2 and fall

		public DFAMeleeState(Mech attacker, Vector3 attackPos, AbstractActor target, 
			HashSet<MeleeAttackType> validAnimations) : base(attacker)
        {
			Mod.Log.Info($"Buliding DFA state for attacker: {CombatantUtils.Label(attacker)} @ attackPos: {attackPos} vs. target: {CombatantUtils.Label(target)}");

			this.Label = Mod.LocalizedText.Labels[ModText.LT_Label_Melee_Type_DeathFromAbove];
			this.IsValid = ValidateAttack(attacker, target);
			if (IsValid)
			{
				this.AttackerTable = DamageTable.KICK;
				this.TargetTable = target.IsProne ? DamageTable.REAR : DamageTable.PUNCH;

				CalculateDamages(attacker, target);
				CalculateInstability(attacker, target);
				CalculateModifiers(attacker, target);
				CreateDescriptions(attacker, target);

				// Set the animation type
				this.AttackAnimation = MeleeAttackType.DFA;
			}
		}

        private bool ValidateAttack(Mech attacker, AbstractActor target)
        {
			// Animations will never include DFA, as that's only for selecting a random attack. Assume the UI has done the checking
			//  to allow or prevent a DFA attack
			if (!attacker.CanDFA)
			{
				Mod.Log.Info($"Attacker unable to DFA due to damage or inability.");
				return false;
			}

			if (!attacker.CanDFATargetFromPosition(target, attacker.CurrentPosition))
			{
				Mod.Log.Info($"Attacker unable to DFA target from their position.");
				return false;
			}

			// No damage check - by rules, you can DFA?
			Mod.Log.Info("DFA ATTACK validated");
			return true;
		}

		private void CreateDescriptions(Mech attacker, AbstractActor target)
        {
			int sumAttackerDamage = this.AttackerDamageClusters.Count() > 0 ?
				(int)Math.Ceiling(this.AttackerDamageClusters.Sum()) : 0;
			string localText = new Text(
				Mod.LocalizedText.AttackDescriptions[ModText.LT_AtkDesc_DFA_Desc], 
				new object[] { 
					sumAttackerDamage, this.AttackerInstability
				})
				.ToString();

			this.DescriptionNotes.Add(localText);
		}

		private void CalculateModifiers(Mech attacker, AbstractActor target)
		{
			// Always add the jump modifier
			this.AttackModifiers.Add(ModText.LT_Label_Attacker_Jumped, Mod.Config.ToHitSelfJumped);

			// Build the comparative skill level
			int comparativeSkill = (attacker.SkillPiloting - target.SkillPiloting) * -1;
			Mod.Log.Info($"Comparative skill = {comparativeSkill} => attacker {CombatantUtils.Label(attacker)} @ piloting: {attacker.SkillPiloting} " +
				$"vs. target: {CombatantUtils.Label(target)} @ piloting: {target.SkillPiloting} ");
			this.AttackModifiers.Add(ModText.LT_Label_ComparativeSkill_Piloting, comparativeSkill);

			// Check for prone targets
			if (target.IsProne) this.AttackModifiers.Add(ModText.LT_Label_Target_Prone, Mod.Config.Melee.ProneTargetAttackModifier);

		}

		private void CalculateDamages(Mech attacker, AbstractActor target)
        {
			Mod.Log.Info($"Calculating DFA damage for attacker: {CombatantUtils.Label(attacker)} " +
				$"vs. target: {CombatantUtils.Label(target)}");
			
			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Tonnage => Attacker: {attacker.tonnage}  Target: {targetTonnage}");

			// Calculate attacker damage
			float attackerRaw = (float)Math.Ceiling(Mod.Config.Melee.DFA.AttackerDamagePerTargetTon * targetTonnage);

			// Modifiers
			float attackerMod = attacker.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerDamageMod) ?
				attacker.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackerDamageMod) : 0f;
			float attackerMulti = attacker.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerDamageMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.DeathFromAboveAttackerDamageMulti) : 1f;
			float attackerFinal = (float)Math.Ceiling((attackerRaw + attackerMod) * attackerMulti);
			Mod.Log.Info($" - Attacker damage => final: {attackerFinal} = (raw: {attackerRaw} + mod: {attackerMod}) x multi: {attackerMulti}");

			// split attacker damage into clusters
			DamageHelper.ClusterDamage(attackerRaw, Mod.Config.Melee.DFA.DamageClusterDivisor, out this.AttackerDamageClusters);

			// Resolve target damage
			float targetRaw = (float)Math.Ceiling(Mod.Config.Melee.DFA.TargetDamagePerAttackerTon * attacker.tonnage);

			// Modifiers
			float targetMod = attacker.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetDamageMod) ?
				attacker.StatCollection.GetValue<int>(ModStats.DeathFromAboveTargetDamageMod) : 0f;
			float targetMulti = attacker.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetDamageMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.DeathFromAboveTargetDamageMulti) : 1f;

			float targetFinal = (float)Math.Ceiling((targetRaw + targetMod) * targetMulti);
			Mod.Log.Info($" - Target damage => final: {targetFinal} = (raw: {targetRaw} + mod: {targetMod}) x multi: {targetMulti}");

			// split target damage into clusters
			DamageHelper.ClusterDamage(targetFinal, Mod.Config.Melee.DFA.DamageClusterDivisor, out this.TargetDamageClusters);
		}

		private void CalculateInstability(Mech attacker, AbstractActor target)
		{
			Mod.Log.Info($"Calculating DFA instability for attacker: {CombatantUtils.Label(attacker)} " +
				$"vs. target: {CombatantUtils.Label(target)}");

			float targetTonnage = 0;
			if (target is Vehicle vehicle) targetTonnage = vehicle.tonnage;
			else if (target is Mech mech) targetTonnage = mech.tonnage;
			Mod.Log.Info($" - Target tonnage is: {targetTonnage}");

			// Resolve attacker instability
			float attackerRaw = (float)Math.Ceiling(Mod.Config.Melee.DFA.AttackerInstabilityPerTargetTon * targetTonnage);

			// Modifiers
			float attackerMod = attacker.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerInstabilityMod) ?
				attacker.StatCollection.GetValue<int>(ModStats.DeathFromAboveAttackerInstabilityMod) : 0f;
			float attackerMulti = attacker.StatCollection.ContainsStatistic(ModStats.DeathFromAboveAttackerInstabilityMulti) ?
				attacker.StatCollection.GetValue<float>(ModStats.DeathFromAboveAttackerInstabilityMulti) : 1f;

			float attackerFinal = (float)Math.Ceiling((attackerRaw + attackerMod) * attackerMulti);
			Mod.Log.Info($" - Attacker instability => final: {attackerFinal} = (raw: {attackerRaw} + mod: {attackerMod}) x multi: {attackerMulti}");
			this.AttackerInstability = attackerFinal;

			// Resolve target instability
			float targetRaw = (float)Math.Ceiling(Mod.Config.Melee.DFA.TargetInstabilityPerAttackerTon * targetTonnage);

			// Modifiers
			float targetMod = target.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetInstabilityMod) ?
				target.StatCollection.GetValue<int>(ModStats.DeathFromAboveTargetInstabilityMod) : 0f;
			float targetMulti = target.StatCollection.ContainsStatistic(ModStats.DeathFromAboveTargetInstabilityMulti) ?
				target.StatCollection.GetValue<float>(ModStats.DeathFromAboveTargetInstabilityMulti) : 1f;

			float targetFinal = (float)Math.Ceiling((targetRaw + targetMod) * targetMulti);
			Mod.Log.Info($" - target instability => final: {targetFinal} = (raw: {targetRaw} + mod: {targetMod}) x multi: {targetMulti}");
			this.TargetInstability = targetFinal;

		}
	}
}
